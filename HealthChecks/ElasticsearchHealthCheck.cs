using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace elasticsearch_netcore.HealthChecks
{
    /// <summary>
    /// Health check for Elasticsearch connectivity and cluster status.
    /// </summary>
    public class ElasticsearchHealthCheck : IHealthCheck
    {
        private readonly IElasticsearchHealthClient _elasticClient;

        public ElasticsearchHealthCheck(IElasticsearchHealthClient elasticClient)
        {
            _elasticClient = elasticClient;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                var isAlive = await _elasticClient.PingAsync(cancellationToken);

                if (!isAlive)
                {
                    return HealthCheckResult.Unhealthy("Elasticsearch ping failed");
                }

                var clusterHealth = await _elasticClient.GetClusterHealthAsync(cancellationToken);

                if (string.IsNullOrEmpty(clusterHealth.ClusterName))
                {
                    return HealthCheckResult.Unhealthy("Elasticsearch cluster health check failed");
                }

                var data = new System.Collections.Generic.Dictionary<string, object>
                {
                    { "cluster", clusterHealth.ClusterName },
                    { "status", clusterHealth.Status }
                };

                return string.Equals(clusterHealth.Status, "Red", StringComparison.OrdinalIgnoreCase)
                    ? HealthCheckResult.Unhealthy("Elasticsearch cluster status is Red", data: data)
                    : HealthCheckResult.Healthy($"Elasticsearch cluster status is {clusterHealth.Status}", data: data);
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy("Elasticsearch connection failed", ex);
            }
        }
    }
}
