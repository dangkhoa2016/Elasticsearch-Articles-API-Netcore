using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;
using elasticsearch_netcore.Factories;

namespace elasticsearch_netcore.Models
{
    /// <summary>
    /// Design-time factory for EF Core migrations.
    /// Used by dotnet-ef CLI tools to create DbContext at design time.
    /// </summary>
    public class ElasticsearchDBContextFactory : IDesignTimeDbContextFactory<ElasticsearchDBContext>
    {
        public ElasticsearchDBContext CreateDbContext(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false)
                .AddJsonFile($"appsettings.{System.Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development"}.json", optional: true)
                .Build();

            var optionsBuilder = new DbContextOptionsBuilder<ElasticsearchDBContext>();
            DatabaseProviderFactory.ConfigureProvider(optionsBuilder, configuration);

            return new ElasticsearchDBContext(optionsBuilder.Options);
        }
    }
}
