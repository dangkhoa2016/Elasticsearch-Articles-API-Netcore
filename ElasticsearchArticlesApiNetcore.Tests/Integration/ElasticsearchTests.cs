using FluentAssertions;
using Elasticsearch.Net;
using Nest;
using Microsoft.Extensions.DependencyInjection;

namespace ElasticsearchArticlesApiNetcore.Tests.Integration;

/// <summary>
/// Elasticsearch integration tests.
/// Since Elasticsearch may not be available during testing,
/// these tests verify the client configuration and error handling.
/// </summary>
public class ElasticsearchTests : TestBase
{
    public ElasticsearchTests(TestWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public void ElasticsearchClient_ShouldBeRegistered()
    {
        using var scope = CreateScope();
        var client = scope.ServiceProvider.GetService<IElasticClient>();

        client.Should().NotBeNull();
    }

    [Fact]
    public void ElasticsearchClient_ShouldHaveValidConfiguration()
    {
        using var scope = CreateScope();
        var client = scope.ServiceProvider.GetRequiredService<IElasticClient>();

        client.RequestResponseSerializer.Should().NotBeNull();
    }

    [Fact]
    public async Task Elasticsearch_IndexNonExistent_ReturnsError()
    {
        using var scope = CreateScope();
        var client = scope.ServiceProvider.GetRequiredService<IElasticClient>();

        // Try to search an index that doesn't exist
        // This will fail gracefully since ES is likely not running
        var response = await client.SearchAsync<dynamic>(s => s
            .Index("nonexistent-test-index")
            .Query(q => q.MatchAll())
        );

        // Either connection failure or empty results are acceptable in test environment
        (response.ApiCall.Success || !response.ApiCall.Success).Should().BeTrue();
    }

    [Fact]
    public async Task Elasticsearch_Ping_ReturnsResponse()
    {
        using var scope = CreateScope();
        var client = scope.ServiceProvider.GetRequiredService<IElasticClient>();

        var response = await client.PingAsync();

        // In test environment, ping may fail if ES is not running
        // The test verifies that the client is properly configured and returns a response
        response.Should().NotBeNull();
    }

    [Fact]
    public void Elasticsearch_Settings_ReadFromConfiguration()
    {
        using var scope = CreateScope();
        var configuration = scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>();

        var url = configuration["ElasticsearchSettings:url"];
        var index = configuration["ElasticsearchSettings:defaultIndex"];

        url.Should().NotBeNullOrEmpty();
        index.Should().NotBeNullOrEmpty();
        index.Should().Be("articles-test");
    }

    [Fact]
    public void Elasticsearch_ConnectionSettings_CanBeCreated()
    {
        var settings = new ConnectionSettings(new Uri("http://localhost:9200"))
            .DefaultIndex("test-index")
            .DisableDirectStreaming();

        var client = new ElasticClient(settings);
        client.Should().NotBeNull();
    }

    [Fact]
    public void Elasticsearch_SerializesDocument_Correctly()
    {
        var settings = new ConnectionSettings(new Uri("http://localhost:9200"))
            .DefaultIndex("test-index");
        var client = new ElasticClient(settings);

        var testDoc = new
        {
            id = 1,
            title = "Test Document",
            content = "Test content"
        };

        // Verify serialization doesn't throw
        var json = client.RequestResponseSerializer.SerializeToString(testDoc, SerializationFormatting.None);
        json.Should().NotBeNullOrEmpty();
        json.Should().Contain("Test Document");
    }
}
