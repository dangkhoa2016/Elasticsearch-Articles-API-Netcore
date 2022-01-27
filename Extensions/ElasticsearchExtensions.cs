using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nest;

namespace elasticsearch_netcore.Extensions
{
    public static class ElasticsearchExtensions
    {
        public static void AddElasticsearch(this IServiceCollection services, IConfiguration configuration)
        {
            string url = configuration["ElasticsearchSettings:url"];
            var settings = new ConnectionSettings(new Uri(string.IsNullOrWhiteSpace(url) ? "http://localhost:9200" : url))
                                .DisableDirectStreaming();
                                //.EnableTcpStats();
            var defaultIndex = configuration["ElasticsearchSettings:defaultIndex"];

            if (!string.IsNullOrEmpty(defaultIndex))
                settings = settings.DefaultIndex(defaultIndex);

            services.AddSingleton<IElasticClient>(new ElasticClient(settings));
        }
    }
}
