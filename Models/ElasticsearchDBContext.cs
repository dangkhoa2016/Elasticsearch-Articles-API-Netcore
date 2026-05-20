using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using elasticsearch_netcore.Factories;

#nullable disable

namespace elasticsearch_netcore.Models
{
    public partial class ElasticsearchDBContext : DbContext
    {
        private static readonly IConfiguration _configuration;

        static ElasticsearchDBContext()
        {
            _configuration = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();
        }

        public ElasticsearchDBContext()
        {
        }

        public ElasticsearchDBContext(DbContextOptions<ElasticsearchDBContext> options)
            : base(options)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                DatabaseProviderFactory.ConfigureProvider(optionsBuilder, _configuration);
            }

            optionsBuilder.EnableSensitiveDataLogging(true)
                            .LogTo(Console.WriteLine);
        }

        public virtual DbSet<Article> Articles { get; set; }
        public virtual DbSet<ArticlesCategory> ArticlesCategories { get; set; }
        public virtual DbSet<Author> Authors { get; set; }
        public virtual DbSet<Authorship> Authorships { get; set; }
        public virtual DbSet<Category> Categories { get; set; }
        public virtual DbSet<Comment> Comments { get; set; }
        public virtual DbSet<User> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Article>(entity =>
            {
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
                entity.Property(e => e.Title).IsRequired();
                entity.Property(e => e.Content).IsRequired();

                entity.HasMany(d => d.Authorships)
                    .WithOne(p => p.Article)
                    .HasForeignKey(d => d.AuthorId)
                    .OnDelete(DeleteBehavior.ClientSetNull);
                entity.HasMany(d => d.Comments)
                    .WithOne(p => p.Article)
                    .HasForeignKey(d => d.ArticleId)
                    .OnDelete(DeleteBehavior.ClientSetNull);
                entity.HasMany(d => d.ArticlesCategories)
                    .WithOne(p => p.Article)
                    .HasForeignKey(d => d.ArticleId)
                    .OnDelete(DeleteBehavior.ClientSetNull);

                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("datetime('now')");
                entity.Property(e => e.UpdatedAt)
                    .HasDefaultValueSql("datetime('now')");

                entity.HasIndex(e => e.Title).HasDatabaseName("index_articles_on_title");
                entity.HasIndex(e => e.CreatedAt).HasDatabaseName("index_articles_on_created_at");
            });

            modelBuilder.Entity<ArticlesCategory>(entity =>
            {
                entity.Property(e => e.ArticleId).IsRequired();
                entity.Property(e => e.CategoryId).IsRequired();
                entity.Property(e => e.Id).ValueGeneratedOnAdd();

                entity.HasOne(d => d.Article)
                    .WithMany(p => p.ArticlesCategories)
                    .HasForeignKey(d => d.ArticleId);
                entity.HasOne(d => d.Category)
                    .WithMany(p => p.ArticlesCategories)
                    .HasForeignKey(d => d.CategoryId);

                entity.HasIndex(e => e.ArticleId).HasDatabaseName("index_articles_categories_on_article_id");
                entity.HasIndex(e => e.CategoryId).HasDatabaseName("index_articles_categories_on_category_id");
            });

            modelBuilder.Entity<Author>(entity =>
            {
                entity.Property(e => e.FirstName).IsRequired();
                //entity.Property(e => e.LastName).IsRequired();
                entity.Property(e => e.Id).ValueGeneratedOnAdd();

                entity.HasMany(d => d.Authorships)
                    .WithOne(p => p.Author)
                    .HasForeignKey(d => d.AuthorId);

                entity.HasIndex(e => new { e.FirstName, e.LastName }).HasDatabaseName("index_authors_on_first_name_last_name");
            });

            modelBuilder.Entity<Authorship>(entity =>
            {
                entity.Property(e => e.ArticleId).IsRequired();
                entity.Property(e => e.AuthorId).IsRequired();
                entity.Property(e => e.Id).ValueGeneratedOnAdd();

                entity.HasOne(d => d.Article)
                    .WithMany(p => p.Authorships)
                    .HasForeignKey(d => d.ArticleId);
                entity.HasOne(d => d.Author)
                    .WithMany(p => p.Authorships)
                    .HasForeignKey(d => d.AuthorId);

                entity.HasIndex(e => e.ArticleId).HasDatabaseName("index_authorships_on_article_id");
                entity.HasIndex(e => e.AuthorId).HasDatabaseName("index_authorships_on_author_id");
            });

            modelBuilder.Entity<Category>(entity =>
            {
                entity.Property(e => e.Title).IsRequired();
                entity.Property(e => e.Id).ValueGeneratedOnAdd();

                entity.HasMany(d => d.ArticlesCategories)
                    .WithOne(p => p.Category)
                    .HasForeignKey(d => d.CategoryId);

                entity.HasIndex(e => e.Title).HasDatabaseName("index_categories_on_title");
            });

            modelBuilder.Entity<Comment>(entity =>
            {
                //entity.Property(e => e.Body).IsRequired();
                entity.Property(e => e.ArticleId).IsRequired();
                entity.Property(e => e.User).IsRequired();
                entity.Property(e => e.Id).ValueGeneratedOnAdd();

                entity.HasOne(d => d.Article)
                    .WithMany(p => p.Comments)
                    .HasForeignKey(d => d.ArticleId);

                entity.HasIndex(e => e.ArticleId).HasDatabaseName("index_comments_on_article_id");
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
