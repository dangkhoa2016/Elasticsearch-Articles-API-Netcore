using Elasticsearch.Net;
using Microsoft.Extensions.Logging;
using Nest;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace elasticsearch_netcore.Helpers
{
    public class Helper : IDisposable
    {
        readonly IElasticClient client = null;
        readonly ILogger<Helper> _logger;
        static string filePath = Path.Combine(Directory.GetCurrentDirectory(), "DB/index.v7.json");

        private const int MaxRetries = 3;
        private const int CircuitMaxFailures = 5;
        private const int CircuitTripSeconds = 30;
        private int _failureCount;
        private DateTime _lastFailureTime = DateTime.MinValue;
        private int _circuitState; // 0=closed, 1=half-open, 2=open

        private readonly ConcurrentQueue<IndexQueueItem> _indexQueue = new ConcurrentQueue<IndexQueueItem>();
        private readonly Timer _flushTimer;
        private const int MaxQueueSize = 100;
        private const int FlushIntervalMs = 5000;

        private class IndexQueueItem
        {
            public string Id { get; set; }
            public string Json { get; set; }
            public string IndexName { get; set; }
        }

        public Helper(ILogger<Helper> logger, IElasticClient elasticClient)
        {
            client = elasticClient;
            _logger = logger;
            _flushTimer = new Timer(async _ => await FlushIndexQueue(), null, FlushIntervalMs, FlushIntervalMs);
        }

        public void Dispose()
        {
            _flushTimer?.Dispose();
        }

        public dynamic GetIndice(string indexName = "")
        {
            if (client == null)
                return null;
            return client.Indices.Get(Indices.Index(indexName));
        }

        public dynamic GetDocCount(string indexName = "")
        {
            if (client == null)
                return null;
            if (string.IsNullOrWhiteSpace(indexName))
                indexName = IndexName;
            return client.Count(new CountRequest(Indices.Index(indexName)));
        }

        public dynamic GetDoc(string id, string indexName = "")
        {
            if (client == null)
                return null;
            if (string.IsNullOrWhiteSpace(indexName))
                indexName = IndexName;
            return client.Get(new DocumentPath<dynamic>(id).Index(Indices.Index(indexName)));
        }

        string IndexName
        {
            get
            {
                if (client == null)
                    return null;
                return client.ConnectionSettings.DefaultIndex;
            }
        }

        public async Task<bool> IndexExists(string indexName = "")
        {
            if (client == null)
                return false;
            if (string.IsNullOrWhiteSpace(indexName))
                indexName = IndexName;
            var result = await client.Indices.ExistsAsync(Indices.Index(indexName));
            return result.Exists;
        }

        public async Task<bool> DeleteIndex(string indexName = "")
        {
            if (client == null)
                return false;
            if (string.IsNullOrWhiteSpace(indexName))
                indexName = IndexName;
            var result = await client.Indices.DeleteAsync(Indices.Index(indexName));
            return result.Acknowledged;
        }

        public async Task<bool> InitIndex(string indexName = "")
        {
            if (client == null)
                return false;
            if (string.IsNullOrWhiteSpace(indexName))
                indexName = IndexName;

            _logger.LogInformation("Start init index: " + indexName);

            if (await IndexExists(indexName))
                return true;

            string json = File.ReadAllText(filePath);
            var result = await client.LowLevel.Indices.CreateAsync<CreateIndexResponse>(indexName, PostData.String(json));
            return result.Acknowledged;
        }

        public async Task IndexDocument(string Id, string json, string indexName = "")
        {
            _logger.LogInformation("Index document: " + Id);

            if (IsCircuitOpen())
            {
                _logger.LogWarning("Circuit breaker open, queueing document {Id}", Id);
                QueueIndexDocument(Id, json, indexName);
                return;
            }

            if (string.IsNullOrWhiteSpace(indexName))
                indexName = IndexName;

            await ExecuteWithRetry(async () =>
            {
                var response = await client.LowLevel.IndexAsync<BytesResponse>(indexName, Id, PostData.String(json));

                if (response.RequestBodyInBytes != null && response.RequestBodyInBytes.Length > 0)
                    _logger.LogInformation(System.Text.Encoding.UTF8.GetString(response.RequestBodyInBytes));
                else
                    _logger.LogInformation("Something error on endpoint: " + JsonConvert.SerializeObject(client.ConnectionSettings.ConnectionPool.Nodes));

                if (response.Body != null && response.Body.Length > 0)
                {
                    _logger.LogInformation(System.Text.Encoding.UTF8.GetString(response.Body));
                    return true;
                }
                return false;

            }, "Index document", Id);
        }

        public async Task RemoveIndexDocument(string Id, string indexName = "")
        {
            _logger.LogInformation("Remove document: " + Id);

            if (IsCircuitOpen())
            {
                _logger.LogWarning("Circuit breaker open, skipping remove for {Id}", Id);
                return;
            }

            if (string.IsNullOrWhiteSpace(indexName))
                indexName = IndexName;

            await ExecuteWithRetry(async () =>
            {
                var response = await client.LowLevel.DeleteAsync<BytesResponse>(indexName, Id);

                if (response.RequestBodyInBytes != null && response.RequestBodyInBytes.Length > 0)
                    _logger.LogInformation(System.Text.Encoding.UTF8.GetString(response.RequestBodyInBytes));

                if (response.Body != null && response.Body.Length > 0)
                {
                    _logger.LogInformation(System.Text.Encoding.UTF8.GetString(response.Body));
                    return true;
                }
                return false;

            }, "Remove document", Id);
        }

        public async Task<bool> BulkIndexDocument(Dictionary<string, string> jsonList, string indexName = "")
        {
            _logger.LogInformation("Bulk index document: " + jsonList.Count);

            if (IsCircuitOpen())
            {
                _logger.LogWarning("Circuit breaker open, skipping bulk index for {Count} documents", jsonList.Count);
                return false;
            }

            if (string.IsNullOrWhiteSpace(indexName))
                indexName = IndexName;

            return await ExecuteWithRetry(async () =>
            {
                List<string> json = new List<string>();
                foreach (var item in jsonList)
                {
                    json.Add(JsonConvert.SerializeObject(new { index = new { _index = indexName, _id = item.Key } }));
                    json.Add(item.Value);
                }
                var response = await client.LowLevel.BulkAsync<BytesResponse>(PostData.MultiJson(json));

                if (response.RequestBodyInBytes != null && response.RequestBodyInBytes.Length > 0)
                    _logger.LogInformation(System.Text.Encoding.UTF8.GetString(response.RequestBodyInBytes));

                if (response.Body != null && response.Body.Length > 0)
                {
                    _logger.LogInformation(System.Text.Encoding.UTF8.GetString(response.Body));
                    return true;
                }
                return false;

            }, "Bulk index", jsonList.Count.ToString());
        }

        private async Task<bool> ExecuteWithRetry(Func<Task<bool>> action, string operation, string identifier)
        {
            var delay = TimeSpan.FromSeconds(1);

            for (int attempt = 0; attempt <= MaxRetries; attempt++)
            {
                try
                {
                    var success = await action();
                    if (success)
                    {
                        ResetCircuit();
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "{Operation} attempt {Attempt} failed for {Id}", operation, attempt + 1, identifier);
                }

                if (attempt < MaxRetries)
                {
                    _logger.LogWarning("{Operation} attempt {Attempt} failed for {Id}. Retrying in {Delay}...", operation, attempt + 1, identifier, delay);
                    await Task.Delay(delay);
                    delay = TimeSpan.FromTicks(delay.Ticks * 2);
                }
                else
                {
                    _logger.LogError("{Operation} failed after {MaxRetries} attempts for {Id}", operation, MaxRetries + 1, identifier);
                    RecordFailure();
                    return false;
                }
            }

            RecordFailure();
            return false;
        }

        private bool IsCircuitOpen()
        {
            if (_circuitState == 0)
                return false;

            if (_circuitState == 2)
            {
                if ((DateTime.Now - _lastFailureTime).TotalSeconds > CircuitTripSeconds)
                {
                    _circuitState = 1;
                    _logger.LogInformation("Circuit breaker half-open");
                    return false;
                }
                return true;
            }

            return false;
        }

        private void RecordFailure()
        {
            var count = Interlocked.Increment(ref _failureCount);
            _lastFailureTime = DateTime.Now;

            if (count >= CircuitMaxFailures)
            {
                _circuitState = 2;
                _logger.LogWarning("Circuit breaker opened after {FailureCount} failures", count);
            }
        }

        private void ResetCircuit()
        {
            Interlocked.Exchange(ref _failureCount, 0);
            _circuitState = 0;
        }

        private void QueueIndexDocument(string id, string json, string indexName)
        {
            _indexQueue.Enqueue(new IndexQueueItem { Id = id, Json = json, IndexName = indexName });
            var count = _indexQueue.Count;
            _logger.LogInformation("Queued document {Id}, queue size: {Count}", id, count);

            if (count >= MaxQueueSize)
            {
                _logger.LogInformation("Queue reached max size, flushing");
                Task.Run(async () => await FlushIndexQueue());
            }
        }

        private async Task FlushIndexQueue()
        {
            var batch = new Dictionary<string, string>();
            while (_indexQueue.TryDequeue(out var item))
            {
                batch[item.Id] = item.Json;
            }

            if (batch.Count > 0)
            {
                _logger.LogInformation("Flushing index queue with {Count} items", batch.Count);
                var success = await BulkIndexDocument(batch);
                if (!success)
                {
                    _logger.LogWarning("Failed to flush index queue, {Count} items lost", batch.Count);
                }
            }
        }
    }
}
