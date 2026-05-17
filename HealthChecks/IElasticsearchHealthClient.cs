using System;
using System.Threading;
using System.Threading.Tasks;

namespace elasticsearch_netcore.HealthChecks
{
    /// <summary>
    /// Abstraction over NEST IElasticClient operations used by ElasticsearchHealthCheck.
    /// Enables unit testing by allowing mock implementations.
    /// </summary>
    public interface IElasticsearchHealthClient
    {
        Task<bool> PingAsync(CancellationToken cancellationToken = default);
        Task<ClusterHealthInfo> GetClusterHealthAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Cluster health information returned by IElasticsearchHealthClient.
    /// </summary>
    public class ClusterHealthInfo
    {
        public string ClusterName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }
}
