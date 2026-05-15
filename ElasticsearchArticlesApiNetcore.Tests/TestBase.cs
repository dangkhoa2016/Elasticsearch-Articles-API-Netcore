global using Xunit;
using elasticsearch_netcore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
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
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddJsonFile("appsettings.Testing.json", optional: false, reloadOnChange: false);
            });
        }
    }
}
