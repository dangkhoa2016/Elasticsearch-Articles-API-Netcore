using elasticsearch_netcore.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

namespace elasticsearch_netcore.Middleware
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception for {Method} {Path}", context.Request.Method, context.Request.Path);

                if (context.Response.HasStarted)
                {
                    _logger.LogWarning("Response has already started, cannot write error response.");
                    throw;
                }

                var statusCode = (int)HttpStatusCode.InternalServerError;
                var errorCode = "InternalServerError";
                var message = "An unexpected error occurred. Please try again later.";

                if (ex is SqliteException sqliteEx && sqliteEx.SqliteErrorCode == 8)
                {
                    _logger.LogWarning(ex, "Database is read-only");
                    statusCode = (int)HttpStatusCode.ServiceUnavailable;
                    errorCode = "DatabaseReadOnly";
                    message = "Service temporarily unavailable: database is read-only. Please try again later.";
                }

                var errorResponse = new ErrorResponse(errorCode, message, statusCode);

                context.Response.StatusCode = statusCode;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(JsonSerializer.Serialize(errorResponse));
            }
        }
    }
}
