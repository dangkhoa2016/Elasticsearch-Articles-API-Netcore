using Elasticsearch.Net;
using Microsoft.Extensions.Logging;
using Nest;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace elasticsearch_netcore.Helpers
{
    public class Helper
    {
        readonly IElasticClient client = null;
        readonly ILogger<Helper> _logger;
        static string filePath = Path.Combine(Directory.GetCurrentDirectory(), "DB/index.v7.json");
        public Helper(ILogger<Helper> logger, IElasticClient elasticClient)
        {
            client = elasticClient;
            _logger = logger;
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

            //return client.LowLevel.Count<BytesResponse>(PostData.String("{}"));
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

            if (string.IsNullOrWhiteSpace(indexName))
                indexName = IndexName;
            try
            {
                var response = await client.LowLevel.IndexAsync<BytesResponse>(indexName, Id, PostData.String(json));

                if (response.RequestBodyInBytes != null && response.RequestBodyInBytes.Length > 0)
                    _logger.LogInformation(System.Text.Encoding.UTF8.GetString(response.RequestBodyInBytes));
                else
                    _logger.LogInformation("Something error on endpoint: " + JsonConvert.SerializeObject(client.ConnectionSettings.ConnectionPool.Nodes));
                if (response.Body != null && response.Body.Length > 0)
                    _logger.LogInformation(System.Text.Encoding.UTF8.GetString(response.Body));

            }
            catch (Exception ex)
            {
                _logger.LogInformation("Error index document: " + ex.Message);
            }
        }

        public async Task RemoveIndexDocument(string Id, string indexName = "")
        {
            _logger.LogInformation("Remove document: " + Id);

            if (string.IsNullOrWhiteSpace(indexName))
                indexName = IndexName;
            try
            {
                var response = await client.LowLevel.DeleteAsync<BytesResponse>(indexName, Id);

                if (response.RequestBodyInBytes != null && response.RequestBodyInBytes.Length > 0)
                    _logger.LogInformation(System.Text.Encoding.UTF8.GetString(response.RequestBodyInBytes));

                if (response.Body != null && response.Body.Length > 0)
                    _logger.LogInformation(System.Text.Encoding.UTF8.GetString(response.Body));

            }
            catch (Exception ex)
            {
                _logger.LogInformation("Error remove document: " + ex.Message);
            }
        }

        public async Task<bool> BulkIndexDocument(Dictionary<string, string> jsonList, string indexName = "")
        {
            _logger.LogInformation("Bulk index document: " + jsonList.Count);

            if (string.IsNullOrWhiteSpace(indexName))
                indexName = IndexName;
            try
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
                else
                    _logger.LogInformation("Something error on endpoint: " + JsonConvert.SerializeObject(client.ConnectionSettings.ConnectionPool.Nodes));
                if (response.Body != null && response.Body.Length > 0)
                {
                    _logger.LogInformation(System.Text.Encoding.UTF8.GetString(response.Body));
                    return true;
                }
                else
                    return false;
            }
            catch (Exception ex)
            {
                _logger.LogInformation("Error bulk index document: " + ex.Message);
            }

            return false;
        }

    }
}
