using System;
using System.Threading;
using System.Threading.Tasks;
using Nest;

namespace elasticsearch_netcore.HealthChecks
{
    /// <summary>
    /// NEST-based implementation of IElasticsearchHealthClient.
    /// Wraps IElasticClient calls for use in health checks.
    /// </summary>
    public class NESTElasticsearchHealthClient : IElasticsearchHealthClient
    {
        private readonly IElasticClient _elasticClient;

        public NESTElasticsearchHealthClient(IElasticClient elasticClient)
        {
            _elasticClient = elasticClient;
        }

        public async Task<bool> PingAsync(CancellationToken cancellationToken = default)
        {
            var response = await _elasticClient.PingAsync(p => p, cancellationToken);
            return response.IsValid;
        }

        public async Task<ClusterHealthInfo> GetClusterHealthAsync(CancellationToken cancellationToken = default)
        {
            var response = await _elasticClient.Cluster.HealthAsync(Nest.Indices.All, _ => _);
            if (!response.IsValid)
                return new ClusterHealthInfo();

            return new ClusterHealthInfo
            {
                ClusterName = response.ClusterName ?? string.Empty,
                Status = response.Status.ToString()
            };
        }
    }
}
