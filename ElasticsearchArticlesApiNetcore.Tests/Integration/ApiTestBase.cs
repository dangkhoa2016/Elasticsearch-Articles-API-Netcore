using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using elasticsearch_netcore.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace ElasticsearchArticlesApiNetcore.Tests.Integration;

/// <summary>
/// Base class for API integration tests with JWT authentication support.
/// </summary>
public abstract class ApiTestBase : TestBase
{
    protected readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerOptions.Default)
    {
        PropertyNameCaseInsensitive = true
    };

    protected ApiTestBase(TestWebApplicationFactory factory) : base(factory)
    {
    }

    /// <summary>
    /// Registers a user and returns a JWT token for authenticated requests.
    /// </summary>
    protected async Task<string> GetAuthTokenAsync()
    {
        var registerRequest = new RegisterRequest
        {
            Username = "testuser",
            Password = "password123",
            Email = "test@example.com"
        };

        var registerContent = new StringContent(
            JsonSerializer.Serialize(registerRequest),
            Encoding.UTF8,
            "application/json");

        var registerResponse = await _client.PostAsync("/api/auth/register", registerContent);

        // If user already exists (from previous test), try login instead
        if (registerResponse.StatusCode == System.Net.HttpStatusCode.BadRequest)
        {
            var loginRequest = new LoginRequest
            {
                Username = "testuser",
                Password = "password123"
            };

            var loginContent = new StringContent(
                JsonSerializer.Serialize(loginRequest),
                Encoding.UTF8,
                "application/json");

            var loginResponse = await _client.PostAsync("/api/auth/login", loginContent);
            loginResponse.EnsureSuccessStatusCode();

            var loginJson = await loginResponse.Content.ReadAsStringAsync();
            var authResponse = JsonSerializer.Deserialize<AuthResponse>(loginJson, _jsonOptions);
            return authResponse!.Token;
        }

        registerResponse.EnsureSuccessStatusCode();

        var json = await registerResponse.Content.ReadAsStringAsync();
        var response = JsonSerializer.Deserialize<AuthResponse>(json, _jsonOptions);
        return response!.Token;
    }

    /// <summary>
    /// Sets the JWT token on the HttpClient's Authorization header.
    /// </summary>
    protected void SetAuthToken(string token)
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    /// <summary>
    /// Creates a StringContent with JSON serialization.
    /// </summary>
    protected static StringContent CreateJsonContent(object data)
    {
        return new StringContent(
            JsonSerializer.Serialize(data),
            Encoding.UTF8,
            "application/json");
    }

    /// <summary>
    /// Deserializes response content to the specified type.
    /// </summary>
    protected async Task<T?> ReadResponseAsync<T>(HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(json, _jsonOptions);
    }
}
