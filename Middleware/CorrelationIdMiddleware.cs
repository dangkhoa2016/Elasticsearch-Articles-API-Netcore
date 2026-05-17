using Microsoft.AspNetCore.Http;
using Serilog.Context;
using System;
using System.Threading.Tasks;

namespace elasticsearch_netcore.Middleware
{
    /// <summary>
    /// Middleware that generates a correlation ID for each request and adds it to the response headers and log context.
    /// </summary>
    public class CorrelationIdMiddleware
    {
        private const string CorrelationIdHeader = "X-Correlation-Id";
        private readonly RequestDelegate _next;

        public CorrelationIdMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var correlationId = GetCorrelationId(context);

            using (LogContext.PushProperty("CorrelationId", correlationId))
            {
                context.Response.OnStarting(() =>
                {
                    if (!context.Response.Headers.ContainsKey(CorrelationIdHeader))
                    {
                        context.Response.Headers.Append(CorrelationIdHeader, correlationId);
                    }

                    return Task.CompletedTask;
                });

                await _next(context);
            }
        }

        private static string GetCorrelationId(HttpContext context)
        {
            if (context.Request.Headers.TryGetValue(CorrelationIdHeader, out var existingCorrelationId))
            {
                return existingCorrelationId.ToString();
            }

            return Guid.NewGuid().ToString("N");
        }
    }
}
