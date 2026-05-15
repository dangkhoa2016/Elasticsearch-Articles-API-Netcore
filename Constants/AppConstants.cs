namespace elasticsearch_netcore.Constants
{
    /// <summary>
    /// Application-wide constants for pagination, caching, HTTP status codes, and queue settings.
    /// </summary>
    public static class AppConstants
    {
        /// <summary>
        /// Default number of items to return per page.
        /// </summary>
        public const int DefaultPageSize = 10;

        /// <summary>
        /// Maximum number of items allowed per page.
        /// </summary>
        public const int MaxPageSize = 50;

        /// <summary>
        /// Default cache expiration time in minutes for article data.
        /// </summary>
        public const int DefaultCacheExpirationMinutes = 15;

        /// <summary>
        /// Default batch size for bulk indexing operations.
        /// </summary>
        public const int BulkIndexPageSize = 100;

        /// <summary>
        /// Default queue capacity for background worker queue.
        /// </summary>
        public const int DefaultQueueCapacity = 100;

        /// <summary>
        /// Default JWT token expiration time in minutes.
        /// </summary>
        public const int DefaultJwtExpirationMinutes = 60;

        /// <summary>
        /// HTTP 500 Internal Server Error status code.
        /// </summary>
        public const int HttpStatusCodeInternalServerError = 500;
    }
}
