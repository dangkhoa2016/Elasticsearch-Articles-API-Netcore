global using Xunit;
using elasticsearch_netcore;
using elasticsearch_netcore.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ElasticsearchArticlesApiNetcore.Tests;

/// <summary>
/// Base class for integration tests with WebApplicationFactory.
/// Provides a configured test host for the Elasticsearch Articles API.
/// </summary>
public abstract class TestBase : IClassFixture<TestBase.TestWebApplicationFactory>
{
    protected readonly HttpClient _client;
    protected readonly TestWebApplicationFactory _factory;

    protected TestBase(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    protected IServiceScope CreateScope()
        => _factory.Services.CreateScope();

    public class TestWebApplicationFactory : WebApplicationFactory<Program>
    {
        private SqliteConnection? _sharedConnection;

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            // Use the test project's output directory as content root
            // so appsettings.Testing.json is found correctly
            var testProjectPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..");
            builder.UseContentRoot(testProjectPath);

            builder.ConfigureTestServices(services =>
            {
                // Remove the existing DbContext registration
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<ElasticsearchDBContext>));
                if (descriptor != null)
                    services.Remove(descriptor);

                // Create a shared in-memory SQLite connection
                _sharedConnection = new SqliteConnection("Data Source=:memory:");
                _sharedConnection.Open();

                services.AddDbContext<ElasticsearchDBContext>(options =>
                    options.UseSqlite(_sharedConnection));
            });

            builder.ConfigureTestServices(services =>
            {
                // Create the database schema after all services are configured
                var sp = services.BuildServiceProvider();
                using var scope = sp.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ElasticsearchDBContext>();
                context.Database.EnsureCreated();
            });

            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddJsonFile("appsettings.Testing.json", optional: false, reloadOnChange: false);
            });
        }

        public override async ValueTask DisposeAsync()
        {
            if (_sharedConnection != null)
            {
                _sharedConnection.Close();
                _sharedConnection.Dispose();
            }
            await base.DisposeAsync();
        }
    }
}
