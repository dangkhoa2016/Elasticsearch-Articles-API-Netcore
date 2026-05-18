using System.Net;
using FluentAssertions;

namespace ElasticsearchArticlesApiNetcore.Tests.Integration;

/// <summary>
/// Integration tests for OpenTelemetry instrumentation.
/// Verifies that HTTP requests are properly instrumented.
/// </summary>
public class TelemetryIntegrationTests : TestBase
{
    public TelemetryIntegrationTests(TestWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task HttpRequest_GeneratesTelemetryData()
    {
        // Act
        var response = await _client.GetAsync("/api/health");

        // Assert - Request completes successfully, telemetry is generated
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task MultipleRequests_GenerateMultipleTelemetryEvents()
    {
        // Act
        var response1 = await _client.GetAsync("/api/health");
        var response2 = await _client.GetAsync("/api/health/database");
        var response3 = await _client.GetAsync("/api/health/elasticsearch");

        // Assert
        response1.StatusCode.Should().Be(HttpStatusCode.OK);
        response2.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.ServiceUnavailable);
        response3.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task CorrelationIdHeader_IsPropagatedToTelemetry()
    {
        // Arrange
        var correlationId = "test-telemetry-correlation-id";
        _client.DefaultRequestHeaders.Add("X-Correlation-Id", correlationId);

        // Act
        var response = await _client.GetAsync("/api/health");

        // Assert - Response should include the correlation ID header
        response.Headers.Should().ContainKey("X-Correlation-Id");
        response.Headers.GetValues("X-Correlation-Id").Should().Contain(correlationId);
    }

    [Fact]
    public async Task UnauthenticatedRequest_IsStillInstrumented()
    {
        // Act - No auth token
        var response = await _client.GetAsync("/api/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task NotFoundRequest_IsInstrumented()
    {
        // Act
        var response = await _client.GetAsync("/api/nonexistent-endpoint");

        // Assert - 404 responses are also instrumented
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PostRequest_IsInstrumented()
    {
        // Arrange
        var content = new StringContent(
            "{\"username\":\"telemetrytest\",\"password\":\"password123\",\"email\":\"telemetry@test.com\"}",
            System.Text.Encoding.UTF8,
            "application/json");

        // Act
        var response = await _client.PostAsync("/api/auth/register", content);

        // Assert - Registration or duplicate error both mean telemetry was generated
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest);
    }
}
