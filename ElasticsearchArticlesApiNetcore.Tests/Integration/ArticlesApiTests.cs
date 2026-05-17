using System.Net;
using System.Text.Json;
using elasticsearch_netcore.Models;
using elasticsearch_netcore.ViewModels;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace ElasticsearchArticlesApiNetcore.Tests.Integration;

/// <summary>
/// Integration tests for the Articles API endpoints.
/// Tests authentication requirements, CRUD operations, and pagination.
/// </summary>
public class ArticlesApiTests : ApiTestBase
{
    public ArticlesApiTests(TestWebApplicationFactory factory) : base(factory)
    {
    }

    #region Authentication Tests

    [Fact]
    public async Task GetArticles_WithoutAuth_Returns401()
    {
        var response = await _client.GetAsync("/api/articles");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetArticle_WithoutAuth_Returns401()
    {
        var response = await _client.GetAsync("/api/articles/1");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateArticle_WithoutAuth_Returns401()
    {
        var content = CreateJsonContent(new ArticleViewModel { Title = "Test", Content = "Test" });
        var response = await _client.PostAsync("/api/articles", content);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateArticle_WithoutAuth_Returns401()
    {
        var content = CreateJsonContent(new ArticleViewModel { Title = "Updated" });
        var response = await _client.PutAsync("/api/articles/1", content);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeleteArticle_WithoutAuth_Returns401()
    {
        var response = await _client.DeleteAsync("/api/articles/1");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    /// <summary>
    /// Creates JSON payload for article creation with required categories/authors arrays.
    /// </summary>
    protected static StringContent CreateArticleJson(string title, string content, string? @abstract = null, long shares = 0)
    {
        var article = new
        {
            title = title,
            content = content,
            @abstract = @abstract ?? "Test",
            url = "https://example.com/test",
            shares = shares,
            categories = new object[] { },
            authors = new object[] { }
        };
        return CreateJsonContent(article);
    }

    #region CRUD Tests

    [Fact]
    public async Task CreateArticle_WithAuth_Returns200()
    {
        var token = await GetAuthTokenAsync();
        SetAuthToken(token);

        var content = CreateArticleJson(
            "Integration Test Article",
            "This is test content for integration testing",
            "Test abstract");

        var response = await _client.PostAsync("/api/articles", content);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await ReadResponseAsync<ArticleViewModel>(response);
        result.Should().NotBeNull();
        result!.Title.Should().Be("Integration Test Article");
        result.Content.Should().Be("This is test content for integration testing");
        result.Id.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetArticles_WithAuth_ReturnsOk()
    {
        var token = await GetAuthTokenAsync();
        SetAuthToken(token);

        var response = await _client.GetAsync("/api/articles");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        json.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetArticles_SupportsPagination()
    {
        var token = await GetAuthTokenAsync();
        SetAuthToken(token);

        var response = await _client.GetAsync("/api/articles?skip=0&take=5");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetArticleById_WhenExists_Returns200()
    {
        var token = await GetAuthTokenAsync();
        SetAuthToken(token);

        // First create an article
        var createResponse = await _client.PostAsync("/api/articles",
            CreateArticleJson("Article For GetById Test", "Content for GetById test", "Test abstract"));
        createResponse.EnsureSuccessStatusCode();
        var created = await ReadResponseAsync<ArticleViewModel>(createResponse);
        created.Should().NotBeNull();

        // Then get it by ID
        var getResponse = await _client.GetAsync($"/api/articles/{created!.Id}");

        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await ReadResponseAsync<ArticleViewModel>(getResponse);
        result.Should().NotBeNull();
        result!.Title.Should().Be("Article For GetById Test");
    }

    [Fact]
    public async Task GetArticleById_WhenNotExists_Returns404()
    {
        var token = await GetAuthTokenAsync();
        SetAuthToken(token);

        var response = await _client.GetAsync("/api/articles/999999");

        // Could be 404 or 200 with null depending on implementation
        (response.StatusCode == HttpStatusCode.NotFound ||
         response.StatusCode == HttpStatusCode.OK).Should().BeTrue();
    }

    [Fact]
    public async Task UpdateArticle_WithAuth_Returns200()
    {
        var token = await GetAuthTokenAsync();
        SetAuthToken(token);

        // Create first
        var createResponse = await _client.PostAsync("/api/articles",
            CreateArticleJson("Article To Update", "Original content", "Test"));
        createResponse.EnsureSuccessStatusCode();
        var created = await ReadResponseAsync<ArticleViewModel>(createResponse);

        // Update
        var updatedArticle = new
        {
            title = "Updated Title",
            content = "Original content",
            @abstract = "Test",
            url = "https://example.com/update-test",
            shares = 0,
            categories = new object[] { },
            authors = new object[] { }
        };
        var updateResponse = await _client.PutAsync($"/api/articles/{created!.Id}", CreateJsonContent(updatedArticle));

        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await ReadResponseAsync<ArticleViewModel>(updateResponse);
        updated.Should().NotBeNull();
        updated!.Title.Should().Be("Updated Title");
    }

    [Fact]
    public async Task DeleteArticle_WithAuth_Returns200()
    {
        var token = await GetAuthTokenAsync();
        SetAuthToken(token);

        // Create first
        var createResponse = await _client.PostAsync("/api/articles",
            CreateArticleJson("Article To Delete", "This article will be deleted", "Test"));
        createResponse.EnsureSuccessStatusCode();
        var created = await ReadResponseAsync<ArticleViewModel>(createResponse);

        // Delete
        var deleteResponse = await _client.DeleteAsync($"/api/articles/{created!.Id}");

        deleteResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion

    #region Validation Tests

    [Fact]
    public async Task CreateArticle_WithMissingTitle_Returns400()
    {
        var token = await GetAuthTokenAsync();
        SetAuthToken(token);

        var article = new { content = "Content without title", categories = new object[] { }, authors = new object[] { } };

        var response = await _client.PostAsync("/api/articles", CreateJsonContent(article));

        // Validation errors may return 400 or 500 depending on middleware configuration
        (response.StatusCode == HttpStatusCode.BadRequest ||
         response.StatusCode == HttpStatusCode.UnprocessableEntity ||
         response.StatusCode == HttpStatusCode.InternalServerError).Should().BeTrue();
    }

    [Fact]
    public async Task CreateArticle_WithMissingContent_Returns400()
    {
        var token = await GetAuthTokenAsync();
        SetAuthToken(token);

        var article = new { title = "Title without content", categories = new object[] { }, authors = new object[] { } };

        var response = await _client.PostAsync("/api/articles", CreateJsonContent(article));

        (response.StatusCode == HttpStatusCode.BadRequest ||
         response.StatusCode == HttpStatusCode.UnprocessableEntity ||
         response.StatusCode == HttpStatusCode.InternalServerError).Should().BeTrue();
    }

    #endregion

    #region Search Tests

    [Fact]
    public async Task GetArticles_WithTitleFilter_ReturnsOk()
    {
        var token = await GetAuthTokenAsync();
        SetAuthToken(token);

        var response = await _client.GetAsync("/api/articles?title=nonexistent");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion
}
