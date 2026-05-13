using System;

namespace elasticsearch_netcore.Models
{
    public class ErrorResponse
    {
        public string Error { get; set; }
        public string Message { get; set; }
        public int StatusCode { get; set; }
        public string Timestamp { get; set; }

        public ErrorResponse(string error, string message, int statusCode)
        {
            Error = error;
            Message = message;
            StatusCode = statusCode;
            Timestamp = DateTime.UtcNow.ToString("o");
        }
    }
}
