using FluentAssertions;
using elasticsearch_netcore.Models;
using elasticsearch_netcore.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace ElasticsearchArticlesApiNetcore.Tests.Integration;

/// <summary>
/// Database integration tests using in-memory SQLite.
/// Tests direct repository/database operations.
/// </summary>
public class DatabaseTests : TestBase
{
    public DatabaseTests(TestWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Database_CanCreateAndRetrieveArticle()
    {
        using var scope = CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ElasticsearchDBContext>();

        var article = new Article
        {
            Title = "DB Test Article",
            Content = "Database integration test content",
            Abstract = "Test",
            Url = "https://example.com/db-test",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Articles.Add(article);
        await context.SaveChangesAsync();

        // Verify
        var retrieved = await context.Articles.FindAsync(article.Id);
        retrieved.Should().NotBeNull();
        retrieved!.Title.Should().Be("DB Test Article");
        retrieved.Content.Should().Be("Database integration test content");
    }

    [Fact]
    public async Task Database_CanCreateAndRetrieveAuthor()
    {
        using var scope = CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ElasticsearchDBContext>();

        var author = new Author
        {
            FirstName = "DB",
            LastName = "Author",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Authors.Add(author);
        await context.SaveChangesAsync();

        var retrieved = await context.Authors.FindAsync(author.Id);
        retrieved.Should().NotBeNull();
        retrieved!.FirstName.Should().Be("DB");
        retrieved!.LastName.Should().Be("Author");
    }

    [Fact]
    public async Task Database_CanCreateAndRetrieveCategory()
    {
        using var scope = CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ElasticsearchDBContext>();

        var category = new Category
        {
            Title = "DB Category",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Categories.Add(category);
        await context.SaveChangesAsync();

        var retrieved = await context.Categories.FindAsync(category.Id);
        retrieved.Should().NotBeNull();
        retrieved!.Title.Should().Be("DB Category");
    }

    [Fact]
    public async Task Database_CanCreateAndRetrieveComment()
    {
        using var scope = CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ElasticsearchDBContext>();

        // Need an article first
        var article = new Article
        {
            Title = "Article For Comment",
            Content = "Content",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Articles.Add(article);
        await context.SaveChangesAsync();

        var comment = new Comment
        {
            Body = "Great article!",
            User = "TestUser",
            ArticleId = article.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Comments.Add(comment);
        await context.SaveChangesAsync();

        var retrieved = await context.Comments.FindAsync(comment.Id);
        retrieved.Should().NotBeNull();
        retrieved!.Body.Should().Be("Great article!");
        retrieved.User.Should().Be("TestUser");
        retrieved.ArticleId.Should().Be(article.Id);
    }

    [Fact]
    public async Task Database_CanCreateAuthorship()
    {
        using var scope = CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ElasticsearchDBContext>();

        var article = new Article
        {
            Title = "Article For Authorship",
            Content = "Content",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var author = new Author { FirstName = "Author", LastName = "Ship", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };

        context.Articles.Add(article);
        context.Authors.Add(author);
        await context.SaveChangesAsync();

        var authorship = new Authorship
        {
            ArticleId = article.Id,
            AuthorId = author.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Authorships.Add(authorship);
        await context.SaveChangesAsync();

        var retrieved = await context.Authorships.FindAsync(authorship.Id);
        retrieved.Should().NotBeNull();
        retrieved!.ArticleId.Should().Be(article.Id);
        retrieved.AuthorId.Should().Be(author.Id);
    }

    [Fact]
    public async Task Database_CanCreateArticlesCategory()
    {
        using var scope = CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ElasticsearchDBContext>();

        var article = new Article
        {
            Title = "Article For Category",
            Content = "Content",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var category = new Category { Title = "Category Link", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };

        context.Articles.Add(article);
        context.Categories.Add(category);
        await context.SaveChangesAsync();

        var articlesCategory = new ArticlesCategory
        {
            ArticleId = article.Id,
            CategoryId = category.Id
        };

        context.ArticlesCategories.Add(articlesCategory);
        await context.SaveChangesAsync();

        var retrieved = await context.ArticlesCategories.FindAsync(articlesCategory.Id);
        retrieved.Should().NotBeNull();
        retrieved!.ArticleId.Should().Be(article.Id);
        retrieved.CategoryId.Should().Be(category.Id);
    }

    [Fact]
    public async Task Database_CanRegisterAndLoginUser()
    {
        using var scope = CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ElasticsearchDBContext>();

        var user = new User
        {
            Username = "dbtestuser",
            Email = "dbtest@example.com",
            PasswordHash = "testhash",
            CreatedAt = DateTime.UtcNow
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();

        var retrieved = await context.Users.FindAsync(user.Id);
        retrieved.Should().NotBeNull();
        retrieved!.Username.Should().Be("dbtestuser");
        retrieved.Email.Should().Be("dbtest@example.com");
    }

    [Fact]
    public async Task Database_SupportsTransactions()
    {
        using var scope = CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var article = new Article
        {
            Title = "Transaction Test",
            Content = "Content",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var articleRepo = unitOfWork.Repository<Article>();
        await articleRepo.AddAsync(article);
        await unitOfWork.SaveChangesAsync();

        var count = await articleRepo.CountAsync(a => a.Title == "Transaction Test");
        count.Should().Be(1);
    }

    [Fact]
    public async Task Database_RepositorySupportsFind()
    {
        using var scope = CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var author = new Author
        {
            FirstName = "Find",
            LastName = "Test",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var authorRepo = unitOfWork.Repository<Author>();
        await authorRepo.AddAsync(author);
        await unitOfWork.SaveChangesAsync();

        var found = await authorRepo.GetByIdAsync(author.Id);
        found.Should().NotBeNull();
        found!.FirstName.Should().Be("Find");
    }

    [Fact]
    public async Task Database_RepositorySupportsDelete()
    {
        using var scope = CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var category = new Category
        {
            Title = "Delete Test",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var categoryRepo = unitOfWork.Repository<Category>();
        await categoryRepo.AddAsync(category);
        await unitOfWork.SaveChangesAsync();

        var countBefore = await categoryRepo.CountAsync();
        await categoryRepo.DeleteAsync(category);
        await unitOfWork.SaveChangesAsync();
        var countAfter = await categoryRepo.CountAsync();

        countAfter.Should().Be(countBefore - 1);
    }
}
