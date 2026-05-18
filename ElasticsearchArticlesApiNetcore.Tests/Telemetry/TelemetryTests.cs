using System.Diagnostics;
using System.Diagnostics.Metrics;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using elasticsearch_netcore.Telemetry;

namespace ElasticsearchArticlesApiNetcore.Tests.Telemetry;

/// <summary>
/// Unit tests for ApiMetrics custom metrics.
/// </summary>
public class ApiMetricsTests
{
    [Fact]
    public void ArticlesProcessed_ReturnsCounterInstance()
    {
        // Act
        var counter = ApiMetrics.ArticlesProcessed;

        // Assert
        counter.Should().NotBeNull();
        counter.GetType().Should().BeAssignableTo<Counter<long>>();
    }

    [Fact]
    public void SearchQueries_ReturnsCounterInstance()
    {
        // Act
        var counter = ApiMetrics.SearchQueries;

        // Assert
        counter.Should().NotBeNull();
        counter.GetType().Should().BeAssignableTo<Counter<long>>();
    }

    [Fact]
    public void AuthAttempts_ReturnsCounterInstance()
    {
        // Act
        var counter = ApiMetrics.AuthAttempts;

        // Assert
        counter.Should().NotBeNull();
        counter.GetType().Should().BeAssignableTo<Counter<long>>();
    }

    [Fact]
    public void ElasticsearchOperations_ReturnsCounterInstance()
    {
        // Act
        var counter = ApiMetrics.ElasticsearchOperations;

        // Assert
        counter.Should().NotBeNull();
        counter.GetType().Should().BeAssignableTo<Counter<long>>();
    }

    [Fact]
    public void RequestDuration_ReturnsHistogramInstance()
    {
        // Act
        var histogram = ApiMetrics.RequestDuration;

        // Assert
        histogram.Should().NotBeNull();
        histogram.GetType().Should().BeAssignableTo<Histogram<double>>();
    }

    [Fact]
    public void ArticlesProcessed_CanIncrement()
    {
        // Arrange
        var counter = ApiMetrics.ArticlesProcessed;

        // Act - should not throw
        counter.Add(1);
        counter.Add(1, new KeyValuePair<string, object?>("operation", "create"));

        // Assert - no exception means success
        true.Should().BeTrue();
    }

    [Fact]
    public void RequestDuration_CanRecord()
    {
        // Arrange
        var histogram = ApiMetrics.RequestDuration;

        // Act - should not throw
        histogram.Record(150.5);
        histogram.Record(200.0, new KeyValuePair<string, object?>("endpoint", "/api/articles"));

        // Assert - no exception means success
        true.Should().BeTrue();
    }
}

/// <summary>
/// Unit tests for TelemetryEnrichmentMiddleware.
/// </summary>
public class TelemetryEnrichmentMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_AddsTagsToActivity_WhenActivityExists()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Method = "GET";
        context.Request.Path = "/api/articles";
        context.Request.Headers["User-Agent"] = "TestAgent";
        context.Request.Headers["X-Correlation-Id"] = "test-correlation-123";
        context.Response.StatusCode = 200;

        RequestDelegate next = async (ctx) =>
        {
            ctx.Response.StatusCode = 200;
            await Task.CompletedTask;
        };

        var middleware = new TelemetryEnrichmentMiddleware(next);

        // Create a test activity to simulate ASP.NET Core tracing
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            SampleUsingParentId = (ref ActivityCreationOptions<string> _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);

        using var source = new ActivitySource("TestSource");
        using var activity = source.StartActivity("TestActivity");

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        if (Activity.Current != null)
        {
            var tags = Activity.Current.TagObjects;
            tags.Should().ContainKey("http.endpoint");
            tags.Should().ContainKey("http.method");
            tags.Should().ContainKey("http.user_agent");
            tags.Should().ContainKey("correlation.id");
        }
    }

    [Fact]
    public async Task InvokeAsync_CallsNextMiddleware()
    {
        // Arrange
        var nextCalled = false;
        var context = new DefaultHttpContext();

        RequestDelegate next = async (ctx) =>
        {
            nextCalled = true;
            await Task.CompletedTask;
        };

        var middleware = new TelemetryEnrichmentMiddleware(next);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_DoesNotThrow_WhenNoActivity()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Method = "GET";
        context.Request.Path = "/api/health";

        RequestDelegate next = async (ctx) =>
        {
            await Task.CompletedTask;
        };

        var middleware = new TelemetryEnrichmentMiddleware(next);

        // Act & Assert - should not throw even without an activity
        var exception = await Record.ExceptionAsync(() => middleware.InvokeAsync(context));
        exception.Should().BeNull();
    }
}
