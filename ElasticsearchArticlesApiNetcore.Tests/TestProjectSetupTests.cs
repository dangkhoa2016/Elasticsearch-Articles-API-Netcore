global using Xunit;
using FluentAssertions;

namespace ElasticsearchArticlesApiNetcore.Tests;

/// <summary>
/// Verifies that the test project is correctly set up.
/// </summary>
public class TestProjectSetupTests
{
    [Fact]
    public void TestProject_ShouldBuildSuccessfully()
    {
        // Arrange & Act
        var result = true;

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Moq_ShouldBeAvailable()
    {
        // Arrange & Act
        var mock = new Moq.Mock<IServiceProvider>();

        // Assert
        Assert.NotNull(mock);
    }

    [Fact]
    public void FluentAssertions_ShouldBeAvailable()
    {
        // Arrange & Act
        var value = 42;

        // Assert
        value.Should().Be(42);
    }
}
