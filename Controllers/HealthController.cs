using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace elasticsearch_netcore.Controllers
{
    /// <summary>
    /// Health check endpoints for monitoring application, database, and Elasticsearch status.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class HealthController : ControllerBase
    {
        private readonly HealthCheckService _healthCheckService;

        public HealthController(HealthCheckService healthCheckService)
        {
            _healthCheckService = healthCheckService;
        }

        /// <summary>
        /// Basic health check - returns 200 OK if application is running.
        /// </summary>
        [HttpGet]
        public IActionResult Get()
        {
            return Ok(new { status = "healthy", timestamp = System.DateTimeOffset.UtcNow });
        }

        /// <summary>
        /// Detailed health check for all registered health checks (database + Elasticsearch).
        /// </summary>
        [HttpGet("detailed")]
        public async Task<ActionResult<HealthReport>> GetDetailed()
        {
            var report = await _healthCheckService.CheckHealthAsync();

            var result = new HealthReport
            {
                OverallStatus = report.Status.ToString(),
                Results = new Dictionary<string, string>(),
                Duration = report.TotalDuration.TotalMilliseconds
            };

            foreach (var entry in report.Entries)
            {
                result.Results[entry.Key] = entry.Value.Status.ToString();
            }

            var statusCode = report.Status switch
            {
                HealthStatus.Healthy => StatusCodes.Status200OK,
                HealthStatus.Degraded => StatusCodes.Status200OK,
                HealthStatus.Unhealthy => StatusCodes.Status503ServiceUnavailable,
                _ => StatusCodes.Status503ServiceUnavailable
            };

            return StatusCode(statusCode, result);
        }

        /// <summary>
        /// Database-only health check.
        /// </summary>
        [HttpGet("database")]
        public async Task<ActionResult<HealthReportEntry>> GetDatabase()
        {
            var report = await _healthCheckService.CheckHealthAsync();

            if (!report.Entries.TryGetValue("Database", out var entry))
            {
                return NotFound(new { error = "Database health check not found" });
            }

            var statusCode = entry.Status switch
            {
                HealthStatus.Healthy => StatusCodes.Status200OK,
                HealthStatus.Degraded => StatusCodes.Status200OK,
                HealthStatus.Unhealthy => StatusCodes.Status503ServiceUnavailable,
                _ => StatusCodes.Status503ServiceUnavailable
            };

            return StatusCode(statusCode, new
            {
                status = entry.Status.ToString(),
                description = entry.Description,
                data = entry.Data
            });
        }

        /// <summary>
        /// Elasticsearch-only health check.
        /// </summary>
        [HttpGet("elasticsearch")]
        public async Task<ActionResult<HealthReportEntry>> GetElasticsearch()
        {
            var report = await _healthCheckService.CheckHealthAsync();

            if (!report.Entries.TryGetValue("Elasticsearch", out var entry))
            {
                return NotFound(new { error = "Elasticsearch health check not found" });
            }

            var statusCode = entry.Status switch
            {
                HealthStatus.Healthy => StatusCodes.Status200OK,
                HealthStatus.Degraded => StatusCodes.Status200OK,
                HealthStatus.Unhealthy => StatusCodes.Status503ServiceUnavailable,
                _ => StatusCodes.Status503ServiceUnavailable
            };

            return StatusCode(statusCode, new
            {
                status = entry.Status.ToString(),
                description = entry.Description,
                data = entry.Data
            });
        }
    }

    /// <summary>
    /// Simplified health report response model.
    /// </summary>
    public class HealthReport
    {
        public string OverallStatus { get; set; }
        public Dictionary<string, string> Results { get; set; }
        public double Duration { get; set; }
    }
}
