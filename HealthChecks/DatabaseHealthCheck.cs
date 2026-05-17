using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace elasticsearch_netcore.HealthChecks
{
    /// <summary>
    /// Health check for SQLite database connectivity.
    /// </summary>
    public class DatabaseHealthCheck : IHealthCheck
    {
        private readonly Models.ElasticsearchDBContext _dbContext;

        public DatabaseHealthCheck(Models.ElasticsearchDBContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                await _dbContext.Database.CanConnectAsync(cancellationToken);
                return HealthCheckResult.Healthy("Database connection is healthy");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy("Database connection failed", ex);
            }
        }
    }
}
