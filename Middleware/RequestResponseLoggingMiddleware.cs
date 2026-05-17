using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Serilog.Context;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace elasticsearch_netcore.Middleware
{
    /// <summary>
    /// Middleware that logs request and response details including method, path, status code, and duration.
    /// </summary>
    public class RequestResponseLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestResponseLoggingMiddleware> _logger;

        public RequestResponseLoggingMiddleware(RequestDelegate next, ILogger<RequestResponseLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var request = context.Request;
            var stopwatch = Stopwatch.StartNew();

            var correlationId = context.Response.Headers["X-Correlation-Id"].ToString();

            LogRequest(request, correlationId);

            var originalBodyStream = context.Response.Body;
            using var responseBody = new MemoryStream();
            context.Response.Body = responseBody;

            try
            {
                await _next(context);

                stopwatch.Stop();
                await LogResponse(context, stopwatch.Elapsed, correlationId);
            }
            finally
            {
                responseBody.Seek(0, SeekOrigin.Begin);
                await responseBody.CopyToAsync(originalBodyStream);
                context.Response.Body = originalBodyStream;
            }
        }

        private void LogRequest(HttpRequest request, string correlationId)
        {
            _logger.LogInformation(
                "HTTP {Method} {Path} {QueryString} - CorrelationId: {CorrelationId}",
                request.Method,
                request.Path,
                request.QueryString,
                correlationId);
        }

        private Task LogResponse(HttpContext context, TimeSpan elapsed, string correlationId)
        {
            context.Response.Body.Seek(0, SeekOrigin.Begin);

            var statusCode = context.Response.StatusCode;
            var logLevel = statusCode >= 500
                ? LogLevel.Error
                : statusCode >= 400
                    ? LogLevel.Warning
                    : LogLevel.Information;

            var message = $"HTTP {context.Request.Method} {context.Request.Path} responded {statusCode} in {{ElapsedMilliseconds}}ms - CorrelationId: {{CorrelationId}}";

            _logger.Log(
                logLevel,
                message,
                elapsed.TotalMilliseconds,
                correlationId);

            context.Response.Body.Seek(0, SeekOrigin.Begin);
            return Task.CompletedTask;
        }
    }
}
