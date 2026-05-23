using System.Net;
using FluentAssertions;
using elasticsearch_netcore.ViewModels;

namespace ElasticsearchArticlesApiNetcore.Tests.Integration;

/// <summary>
/// Integration tests for the Categories API endpoints.
/// </summary>
[Collection("Integration")]
public class CategoriesApiTests : ApiTestBase
{
    public CategoriesApiTests(TestWebApplicationFactory factory) : base(factory)
    {
    }

    #region Authentication Tests

    [Fact]
    public async Task GetCategories_WithoutAuth_Returns401()
    {
        var response = await _client.GetAsync("/api/categories");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateCategory_WithoutAuth_Returns401()
    {
        var content = CreateJsonContent(new CategoryViewModel { Title = "Test" });
        var response = await _client.PostAsync("/api/categories", content);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region CRUD Tests

    [Fact]
    public async Task CreateCategory_WithAuth_Returns200()
    {
        var token = await GetAuthTokenAsync();
        SetAuthToken(token);

        var category = new CategoryViewModel
        {
            Title = "Technology"
        };

        var response = await _client.PostAsync("/api/categories", CreateJsonContent(category));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await ReadResponseAsync<CategoryViewModel>(response);
        result.Should().NotBeNull();
        result!.Title.Should().Be("Technology");
        result.Id.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetCategories_WithAuth_ReturnsOk()
    {
        var token = await GetAuthTokenAsync();
        SetAuthToken(token);

        // Create a category first
        var category = new CategoryViewModel { Title = "Science" };
        var createResponse = await _client.PostAsync("/api/categories", CreateJsonContent(category));
        createResponse.EnsureSuccessStatusCode();

        var response = await _client.GetAsync("/api/categories");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetCategoryById_WhenExists_Returns200()
    {
        var token = await GetAuthTokenAsync();
        SetAuthToken(token);

        // Create first
        var category = new CategoryViewModel { Title = "Mathematics" };
        var createResponse = await _client.PostAsync("/api/categories", CreateJsonContent(category));
        createResponse.EnsureSuccessStatusCode();
        var created = await ReadResponseAsync<CategoryViewModel>(createResponse);

        // Get by ID
        var response = await _client.GetAsync($"/api/categories/{created!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await ReadResponseAsync<CategoryViewModel>(response);
        result.Should().NotBeNull();
        result!.Title.Should().Be("Mathematics");
    }

    [Fact]
    public async Task UpdateCategory_WithAuth_Returns200()
    {
        var token = await GetAuthTokenAsync();
        SetAuthToken(token);

        // Create
        var category = new CategoryViewModel { Title = "Old Title" };
        var createResponse = await _client.PostAsync("/api/categories", CreateJsonContent(category));
        createResponse.EnsureSuccessStatusCode();
        var created = await ReadResponseAsync<CategoryViewModel>(createResponse);

        // Update
        created!.Title = "New Title";
        var updateResponse = await _client.PutAsync($"/api/categories/{created.Id}", CreateJsonContent(created));

        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await ReadResponseAsync<CategoryViewModel>(updateResponse);
        updated.Should().NotBeNull();
        updated!.Title.Should().Be("New Title");
    }

    [Fact]
    public async Task DeleteCategory_WithAuth_Returns200()
    {
        var token = await GetAuthTokenAsync();
        SetAuthToken(token);

        // Create
        var category = new CategoryViewModel { Title = "To Delete" };
        var createResponse = await _client.PostAsync("/api/categories", CreateJsonContent(category));
        createResponse.EnsureSuccessStatusCode();
        var created = await ReadResponseAsync<CategoryViewModel>(createResponse);

        // Delete
        var response = await _client.DeleteAsync($"/api/categories/{created!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetArticlesForCategory_WithAuth_ReturnsOk()
    {
        var token = await GetAuthTokenAsync();
        SetAuthToken(token);

        var category = new CategoryViewModel { Title = "Empty Category" };
        var createResponse = await _client.PostAsync("/api/categories", CreateJsonContent(category));
        createResponse.EnsureSuccessStatusCode();
        var created = await ReadResponseAsync<CategoryViewModel>(createResponse);

        var response = await _client.GetAsync($"/api/categories/{created!.Id}/articles");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion

    #region Validation Tests

    [Fact]
    public async Task CreateCategory_WithMissingTitle_Returns400()
    {
        var token = await GetAuthTokenAsync();
        SetAuthToken(token);

        var category = new CategoryViewModel();

        var response = await _client.PostAsync("/api/categories", CreateJsonContent(category));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion
}
