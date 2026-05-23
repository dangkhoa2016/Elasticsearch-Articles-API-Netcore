namespace ElasticsearchArticlesApiNetcore.Tests.Integration;

/// <summary>
/// Collection fixture to ensure integration tests run serially to avoid
/// race conditions with shared in-memory SQLite database and HttpClient.
/// </summary>
[CollectionDefinition("Integration")]
public class IntegrationCollection : ICollectionFixture<TestBase.TestWebApplicationFactory>
{
}
