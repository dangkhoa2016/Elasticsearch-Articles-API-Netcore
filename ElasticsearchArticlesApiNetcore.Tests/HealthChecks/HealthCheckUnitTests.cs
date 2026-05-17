using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Moq;
using elasticsearch_netcore.HealthChecks;
using elasticsearch_netcore.Models;

namespace ElasticsearchArticlesApiNetcore.Tests.HealthChecks;

/// <summary>
/// Unit tests for DatabaseHealthCheck.
/// </summary>
public class DatabaseHealthCheckTests
{
    [Fact]
    public async Task CheckHealthAsync_ReturnsHealthy_WhenDatabaseIsAccessible()
    {
        // Arrange
        await using var connection = new Microsoft.Data.Sqlite.SqliteConnection("Data Source=:memory:");
        connection.Open();

        var dbOptions = new DbContextOptionsBuilder<ElasticsearchDBContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new ElasticsearchDBContext(dbOptions);
        await context.Database.EnsureCreatedAsync();

        var healthCheck = new DatabaseHealthCheck(context);

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().Be("Database connection is healthy");
    }
}

/// <summary>
/// Unit tests for ElasticsearchHealthCheck.
/// </summary>
public class ElasticsearchHealthCheckTests
{
    [Fact]
    public async Task CheckHealthAsync_ReturnsHealthy_WhenPingSucceedsAndClusterIsGreen()
    {
        // Arrange
        var mockClient = new Mock<IElasticsearchHealthClient>();
        mockClient.Setup(c => c.PingAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        mockClient.Setup(c => c.GetClusterHealthAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ClusterHealthInfo { ClusterName = "test-cluster", Status = "Green" });

        var healthCheck = new ElasticsearchHealthCheck(mockClient.Object);

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Status.Should().Be(HealthStatus.Healthy);
        result.Data.Should().ContainKey("status");
        result.Data["status"].Should().Be("Green");
        result.Data.Should().ContainKey("cluster");
        result.Data["cluster"].Should().Be("test-cluster");
    }

    [Fact]
    public async Task CheckHealthAsync_ReturnsHealthy_WhenClusterIsYellow()
    {
        // Arrange
        var mockClient = new Mock<IElasticsearchHealthClient>();
        mockClient.Setup(c => c.PingAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        mockClient.Setup(c => c.GetClusterHealthAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ClusterHealthInfo { ClusterName = "test-cluster", Status = "Yellow" });

        var healthCheck = new ElasticsearchHealthCheck(mockClient.Object);

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Status.Should().Be(HealthStatus.Healthy);
    }

    [Fact]
    public async Task CheckHealthAsync_ReturnsUnhealthy_WhenClusterIsRed()
    {
        // Arrange
        var mockClient = new Mock<IElasticsearchHealthClient>();
        mockClient.Setup(c => c.PingAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        mockClient.Setup(c => c.GetClusterHealthAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ClusterHealthInfo { ClusterName = "test-cluster", Status = "Red" });

        var healthCheck = new ElasticsearchHealthCheck(mockClient.Object);

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Contain("Red");
    }

    [Fact]
    public async Task CheckHealthAsync_ReturnsUnhealthy_WhenPingFails()
    {
        // Arrange
        var mockClient = new Mock<IElasticsearchHealthClient>();
        mockClient.Setup(c => c.PingAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var healthCheck = new ElasticsearchHealthCheck(mockClient.Object);

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Be("Elasticsearch ping failed");
    }

    [Fact]
    public async Task CheckHealthAsync_ReturnsUnhealthy_WhenClusterHealthFails()
    {
        // Arrange
        var mockClient = new Mock<IElasticsearchHealthClient>();
        mockClient.Setup(c => c.PingAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        mockClient.Setup(c => c.GetClusterHealthAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ClusterHealthInfo { ClusterName = string.Empty, Status = string.Empty });

        var healthCheck = new ElasticsearchHealthCheck(mockClient.Object);

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Be("Elasticsearch cluster health check failed");
    }

    [Fact]
    public async Task CheckHealthAsync_ReturnsUnhealthy_WhenExceptionThrown()
    {
        // Arrange
        var mockClient = new Mock<IElasticsearchHealthClient>();
        var expectedException = new HttpRequestException("Elasticsearch is down");

        mockClient.Setup(c => c.PingAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(expectedException);

        var healthCheck = new ElasticsearchHealthCheck(mockClient.Object);

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Be("Elasticsearch connection failed");
        result.Exception.Should().Be(expectedException);
    }
}
