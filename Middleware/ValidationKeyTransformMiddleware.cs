using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace elasticsearch_netcore.Middleware
{
    public class ValidationKeyTransformMiddleware
    {
        private readonly RequestDelegate _next;

        public ValidationKeyTransformMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            var originalBodyStream = context.Response.Body;
            using var responseBody = new MemoryStream();
            context.Response.Body = responseBody;

            await _next(context);

            if (context.Response.StatusCode == 400)
            {
                responseBody.Seek(0, SeekOrigin.Begin);
                var bodyText = new StreamReader(responseBody).ReadToEnd();

                if (!string.IsNullOrEmpty(bodyText) && bodyText.Contains("\"errors\""))
                {
                    try
                    {
                        using var doc = JsonDocument.Parse(bodyText);
                        if (doc.RootElement.TryGetProperty("errors", out var errorsElement) && errorsElement.ValueKind == JsonValueKind.Object)
                        {
                            var transformedErrors = new Dictionary<string, string[]>();
                            foreach (var property in errorsElement.EnumerateObject())
                            {
                                var snakeKey = JsonNamingPolicy.SnakeCaseLower.ConvertName(property.Name);
                                var values = property.Value.EnumerateArray().Select(e => e.GetString()).ToArray();
                                transformedErrors[snakeKey] = values;
                            }

                            var transformed = new Dictionary<string, object>();
                            foreach (var prop in doc.RootElement.EnumerateObject())
                            {
                                if (prop.Name == "errors")
                                {
                                    transformed[prop.Name] = transformedErrors;
                                }
                                else
                                {
                                    transformed[prop.Name] = prop.Value.Clone();
                                }
                            }

                            var newBody = JsonSerializer.Serialize(transformed);
                            var newBytes = System.Text.Encoding.UTF8.GetBytes(newBody);
                            context.Response.ContentLength = newBytes.Length;
                            context.Response.ContentType = "application/problem+json";
                            await originalBodyStream.WriteAsync(newBytes);
                            return;
                        }
                    }
                    catch
                    {
                        // If transformation fails, return original body
                    }
                }

                responseBody.Seek(0, SeekOrigin.Begin);
                await responseBody.CopyToAsync(originalBodyStream);
            }
            else
            {
                responseBody.Seek(0, SeekOrigin.Begin);
                await responseBody.CopyToAsync(originalBodyStream);
            }
        }
    }
}
