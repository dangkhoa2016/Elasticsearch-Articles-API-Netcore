# Test Project Documentation

> 🌐 Language / Ngôn ngữ: **English** | [Tiếng Việt](README.vi.md)

## Overview

The `ElasticsearchArticlesApiNetcore.Tests` project provides unit tests and integration tests for the Article Management API with Elasticsearch. It uses xUnit as the testing framework, Moq for mocking, FluentAssertions for readable assertions, and `Microsoft.AspNetCore.Mvc.Testing` for integration testing.

## Prerequisites

To ensure tests (especially integration tests) run successfully, your machine must meet the following conditions:

1. **.NET SDK:** Version 10.0 or later.
2. **Docker:** Required for integration testing with Elasticsearch.
* Before running tests, make sure the Elasticsearch instance for the test environment is started:
```bash
docker run -d --name es-test -p 9200:9200 -e "discovery.type=single-node" -e "xpack.security.enabled=false" elasticsearch:8.11.0

```


3. **SQLite:** No configuration needed since the project uses SQLite In-Memory (automatically initialized in memory at runtime).

---

## Directory Structure

```
ElasticsearchArticlesApiNetcore.Tests/
├── ElasticsearchArticlesApiNetcore.Tests.csproj  # Test project configuration
├── appsettings.Testing.json                       # Test environment configuration
├── TestBase.cs                                    # Base class for integration tests
├── TestProjectSetupTests.cs                       # Verifies test infrastructure is working
├── Services/                                      # Unit tests for service layer
│   └── (ArticleServiceTests.cs, etc.)
├── Validators/                                    # Unit tests for FluentValidation validators
│   └── (ArticleViewModelValidatorTests.cs, etc.)
└── Integration/                                   # Integration tests for API
    └── (ArticlesApiTests.cs, DatabaseTests.cs, etc.)

```

---

## Technologies Used

| Package | Version | Purpose |
| --- | --- | --- |
| xUnit | 2.4.2 | Testing framework |
| Moq | 4.20.72 | Mocking framework |
| FluentAssertions | 8.2.0 | Readable assertion syntax |
| Microsoft.AspNetCore.Mvc.Testing | 10.0.8 | Integration testing with WebApplicationFactory |
| Microsoft.NET.Test.Sdk | 17.6.0 | Testing SDK |
| coverlet.collector | 6.0.0 | Code coverage collection |

---

## Running Tests

Before running, ensure the .NET environment variable is set correctly in your terminal:

```bash
export PATH="$HOME/.dotnet:$PATH"

```

### 1. Basic Commands

* **Run all tests:**

```bash
  dotnet test ElasticsearchArticlesApiNetcore.Tests/ElasticsearchArticlesApiNetcore.Tests.csproj

```

* **Run with verbose output (View directly in terminal):**

```bash
  dotnet test ElasticsearchArticlesApiNetcore.Tests/ElasticsearchArticlesApiNetcore.Tests.csproj --verbosity normal

```

* **Run a specific test class:**

```bash
  dotnet test ElasticsearchArticlesApiNetcore.Tests/ElasticsearchArticlesApiNetcore.Tests.csproj --filter "FullyQualifiedName~TestProjectSetupTests"

```

* **Run with code coverage statistics:**

```bash
  dotnet test ElasticsearchArticlesApiNetcore.Tests/ElasticsearchArticlesApiNetcore.Tests.csproj --collect:"XPlat Code Coverage"

```

### 2. Export Logs to File (For CI/CD or Tracing)

When redirecting logs to a file, you need to disable shared compilation (`UseSharedCompilation=false`) to prevent MSBuild from buffering and truncating/losing logs.

* **Export all logs to `logs/test.log` (Real-time writing):**

```bash
  mkdir -p logs && dotnet test ElasticsearchArticlesApiNetcore.Tests/ElasticsearchArticlesApiNetcore.Tests.csproj /p:UseSharedCompilation=false > logs/test.log 2>&1

```

* **Watch logs in real-time from another terminal:**

```bash
  tail -f logs/test.log

```

### 3. Run Sequentially (Avoid Data Conflicts)

By default, xUnit runs test classes in parallel. If integration tests interfere with each other by sharing an Elasticsearch Index or creating/deleting SQLite data, force sequential execution with:

```bash
dotnet test ElasticsearchArticlesApiNetcore.Tests/ElasticsearchArticlesApiNetcore.Tests.csproj -- .CollectionBehavior.MaxParallelThreads=1

```

---

## Test Configuration

### appsettings.Testing.json

The test environment uses a separate configuration optimized for testing and fully isolated from Production:

| Setting | Value | Reason |
| --- | --- | --- |
| Database | `Data Source=:memory:` | In-memory SQLite, fast and isolated testing |
| Rate Limiting | Disabled | Avoid test failures due to rate limits |
| Elasticsearch Index | `articles-test` | Separate index, does not affect production data |
| JWT Secret | Test key | Isolated from production credentials |
| Cache Expiry | 1 minute | Fast cache rotation for test scenarios |

---

## Writing Tests

### Unit Test Example

```csharp
using Moq;
using FluentAssertions;

namespace ElasticsearchArticlesApiNetcore.Tests.Services;

public class ArticleServiceTests
{
    [Fact]
    public async Task GetArticles_ShouldReturnPagedResults()
    {
        // Arrange
        var mockRepository = new Mock<IArticleRepository>();
        mockRepository.Setup(r => r.GetArticlesAsync(1, 10))
            .ReturnsAsync(new { articles = new List<Article>(), total = 50 });

        // Act
        var result = await mockRepository.Object.GetArticlesAsync(1, 10);

        // Assert
        result.total.Should().Be(50);
        mockRepository.Verify(r => r.GetArticlesAsync(1, 10), Times.Once);
    }
}

```

### Integration Test Example

```csharp
using System.Net;
using FluentAssertions;

namespace ElasticsearchArticlesApiNetcore.Tests.Integration;

public class ArticlesApiTests : TestBase
{
    public ArticlesApiTests(TestWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task GetArticles_ShouldReturnOk()
    {
        // Act
        var response = await _client.GetAsync("/api/articles");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}

```

---

## TestBase Class

The `TestBase` class provides a pre-configured `WebApplicationFactory<Program>` for integration testing:

```csharp
public abstract class TestBase : IClassFixture<TestBase.TestWebApplicationFactory>
{
    protected readonly HttpClient _client;
    protected readonly TestWebApplicationFactory _factory;

    protected TestBase(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    protected IServiceScope CreateScope()
        => _factory.Services.CreateScope();
}

```

> 💡 **How it works:** This class automatically sets the environment to `Testing`, runs Migrations to set up data tables on a fresh SQLite In-Memory database for each test run, and cleans up thoroughly after testing.

---

## Best Practices

1. **Test isolation**: Each test must be independent. Use mocks for external dependencies in Unit Tests.
2. **Descriptive naming**: Test method names should describe the scenario and expected outcome (e.g., `MethodName_StateUnderTest_ExpectedBehavior`).
3. **Arrange-Act-Assert**: Always structure test code with 3 clear parts: arrange, act, assert.
4. **Use FluentAssertions**: Prefer `value.Should().Be(expected)` over `Assert.Equal(expected, value)` for more natural and readable error messages when tests fail.
5. **Clean up resources**: Use `IClassFixture` for heavy shared setups (like initializing WebApplicationFactory) and `IDisposable` if you need to clean up leftover data after each test case.
