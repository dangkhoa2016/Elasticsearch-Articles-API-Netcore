using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace elasticsearch_netcore.Middleware
{
    public class SecurityHeadersMiddleware
    {
        private readonly RequestDelegate _next;

        public SecurityHeadersMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // X-Content-Type-Options: Prevent MIME type sniffing
            context.Response.Headers.Append("X-Content-Type-Options", "nosniff");

            // X-Frame-Options: Prevent clickjacking
            context.Response.Headers.Append("X-Frame-Options", "DENY");

            // X-XSS-Protection: Enable XSS filter
            context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");

            // Referrer-Policy: Control referrer information
            context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");

            // Content-Security-Policy: Prevent XSS and injection attacks
            context.Response.Headers.Append("Content-Security-Policy", 
                "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data: https:; font-src 'self'; connect-src 'self'; frame-ancestors 'none'");

            // Permissions-Policy: Control browser features
            context.Response.Headers.Append("Permissions-Policy", 
                "geolocation=(), microphone=(), camera=()");

            // Remove server header
            context.Response.Headers.Remove("Server");
            context.Response.Headers.Remove("X-Powered-By");

            await _next(context);
        }
    }
}
