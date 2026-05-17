using System.Net;
using FluentAssertions;
using elasticsearch_netcore.ViewModels;

namespace ElasticsearchArticlesApiNetcore.Tests.Integration;

/// <summary>
/// Integration tests for the Authors API endpoints.
/// </summary>
public class AuthorsApiTests : ApiTestBase
{
    public AuthorsApiTests(TestWebApplicationFactory factory) : base(factory)
    {
    }

    #region Authentication Tests

    [Fact]
    public async Task GetAuthors_WithoutAuth_Returns401()
    {
        var response = await _client.GetAsync("/api/authors");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateAuthor_WithoutAuth_Returns401()
    {
        var content = CreateJsonContent(new AuthorViewModel { FirstName = "Test" });
        var response = await _client.PostAsync("/api/authors", content);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region CRUD Tests

    [Fact]
    public async Task CreateAuthor_WithAuth_Returns200()
    {
        var token = await GetAuthTokenAsync();
        SetAuthToken(token);

        var author = new AuthorViewModel
        {
            FirstName = "John",
            LastName = "Doe"
        };

        var response = await _client.PostAsync("/api/authors", CreateJsonContent(author));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await ReadResponseAsync<AuthorViewModel>(response);
        result.Should().NotBeNull();
        result!.FirstName.Should().Be("John");
        result.LastName.Should().Be("Doe");
        result.Id.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetAuthors_WithAuth_ReturnsOk()
    {
        var token = await GetAuthTokenAsync();
        SetAuthToken(token);

        // Create an author first
        var author = new AuthorViewModel { FirstName = "Jane", LastName = "Smith" };
        var createResponse = await _client.PostAsync("/api/authors", CreateJsonContent(author));
        createResponse.EnsureSuccessStatusCode();

        var response = await _client.GetAsync("/api/authors");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetAuthorById_WhenExists_Returns200()
    {
        var token = await GetAuthTokenAsync();
        SetAuthToken(token);

        // Create first
        var author = new AuthorViewModel { FirstName = "Bob", LastName = "Wilson" };
        var createResponse = await _client.PostAsync("/api/authors", CreateJsonContent(author));
        createResponse.EnsureSuccessStatusCode();
        var created = await ReadResponseAsync<AuthorViewModel>(createResponse);

        // Get by ID
        var response = await _client.GetAsync($"/api/authors/{created!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await ReadResponseAsync<AuthorViewModel>(response);
        result.Should().NotBeNull();
        result!.FirstName.Should().Be("Bob");
    }

    [Fact]
    public async Task UpdateAuthor_WithAuth_Returns200()
    {
        var token = await GetAuthTokenAsync();
        SetAuthToken(token);

        // Create
        var author = new AuthorViewModel { FirstName = "Old", LastName = "Name" };
        var createResponse = await _client.PostAsync("/api/authors", CreateJsonContent(author));
        createResponse.EnsureSuccessStatusCode();
        var created = await ReadResponseAsync<AuthorViewModel>(createResponse);

        // Update
        created!.FirstName = "New";
        created.LastName = "Name";
        var updateResponse = await _client.PutAsync($"/api/authors/{created.Id}", CreateJsonContent(created));

        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await ReadResponseAsync<AuthorViewModel>(updateResponse);
        updated.Should().NotBeNull();
        updated!.FirstName.Should().Be("New");
    }

    [Fact]
    public async Task DeleteAuthor_WithAuth_Returns200()
    {
        var token = await GetAuthTokenAsync();
        SetAuthToken(token);

        // Create
        var author = new AuthorViewModel { FirstName = "Delete", LastName = "Me" };
        var createResponse = await _client.PostAsync("/api/authors", CreateJsonContent(author));
        createResponse.EnsureSuccessStatusCode();
        var created = await ReadResponseAsync<AuthorViewModel>(createResponse);

        // Delete
        var response = await _client.DeleteAsync($"/api/authors/{created!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetArticlesForAuthor_WithAuth_ReturnsOk()
    {
        var token = await GetAuthTokenAsync();
        SetAuthToken(token);

        var author = new AuthorViewModel { FirstName = "Author", LastName = "WithArticles" };
        var createResponse = await _client.PostAsync("/api/authors", CreateJsonContent(author));
        createResponse.EnsureSuccessStatusCode();
        var created = await ReadResponseAsync<AuthorViewModel>(createResponse);

        var response = await _client.GetAsync($"/api/authors/{created!.Id}/articles");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion

    #region Validation Tests

    [Fact]
    public async Task CreateAuthor_WithMissingFirstName_Returns400()
    {
        var token = await GetAuthTokenAsync();
        SetAuthToken(token);

        var author = new AuthorViewModel { LastName = "Doe" };

        var response = await _client.PostAsync("/api/authors", CreateJsonContent(author));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion
}
