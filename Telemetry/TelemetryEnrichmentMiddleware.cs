using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace elasticsearch_netcore.Telemetry
{
    /// <summary>
    /// Middleware that enriches OpenTelemetry activity spans with custom tags.
    /// Adds endpoint, user agent, and correlation ID information to traces.
    /// </summary>
    public class TelemetryEnrichmentMiddleware
    {
        private readonly RequestDelegate _next;

        public TelemetryEnrichmentMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var activity = Activity.Current;

            if (activity != null)
            {
                activity.SetTag("http.endpoint", context.Request.Path);
                activity.SetTag("http.method", context.Request.Method);
                activity.SetTag("http.user_agent", context.Request.Headers["User-Agent"].ToString());

                if (context.Request.Headers.TryGetValue("X-Correlation-Id", out var correlationId))
                {
                    activity.SetTag("correlation.id", correlationId.ToString());
                }

                if (context.User.Identity?.IsAuthenticated == true)
                {
                    activity.SetTag("user.authenticated", true);
                    activity.SetTag("user.name", context.User.Identity.Name);
                }
            }

            await _next(context);

            // Add response tags after the request completes
            if (activity != null && activity.IsAllDataRequested)
            {
                activity.SetTag("http.status_code", context.Response.StatusCode);
                activity.SetTag("http.response_content_length", context.Response.ContentLength ?? 0);
            }
        }
    }
}
