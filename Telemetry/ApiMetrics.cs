#nullable enable
using System.Diagnostics.Metrics;

namespace elasticsearch_netcore.Telemetry
{
    /// <summary>
    /// Custom metrics for the Elasticsearch Articles API.
    /// Provides counters and gauges for business-level metrics.
    /// </summary>
    public static class ApiMetrics
    {
        private static readonly Meter _meter = new("ElasticsearchArticlesApi", "1.0.0");

        /// <summary>
        /// Counter for total articles processed (created, updated, deleted).
        /// </summary>
        public static Counter<long> ArticlesProcessed => _articlesProcessed ??= _meter.CreateCounter<long>(
            "api.articles.processed",
            description: "Total number of articles processed");

        /// <summary>
        /// Counter for total search queries executed.
        /// </summary>
        public static Counter<long> SearchQueries => _searchQueries ??= _meter.CreateCounter<long>(
            "api.search.queries",
            description: "Total number of search queries executed");

        /// <summary>
        /// Counter for total authentication attempts.
        /// </summary>
        public static Counter<long> AuthAttempts => _authAttempts ??= _meter.CreateCounter<long>(
            "api.auth.attempts",
            description: "Total number of authentication attempts");

        /// <summary>
        /// Counter for Elasticsearch operations (index, delete, bulk).
        /// </summary>
        public static Counter<long> ElasticsearchOperations => _elasticsearchOperations ??= _meter.CreateCounter<long>(
            "api.elasticsearch.operations",
            description: "Total number of Elasticsearch operations");

        /// <summary>
        /// Histogram for request processing duration within the application.
        /// </summary>
        public static Histogram<double> RequestDuration => _requestDuration ??= _meter.CreateHistogram<double>(
            "api.request.duration",
            unit: "ms",
            description: "Request processing duration");

        private static Counter<long>? _articlesProcessed;
        private static Counter<long>? _searchQueries;
        private static Counter<long>? _authAttempts;
        private static Counter<long>? _elasticsearchOperations;
        private static Histogram<double>? _requestDuration;
    }
}
