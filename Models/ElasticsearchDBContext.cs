using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Logging;

#nullable disable

namespace elasticsearch_netcore.Models
{
    public partial class ElasticsearchDBContext : DbContext
    {
        public ElasticsearchDBContext()
        {
        }

        public ElasticsearchDBContext(DbContextOptions<ElasticsearchDBContext> options)
            : base(options)
        {
        }

        public static readonly LoggerFactory _myLoggerFactory = new LoggerFactory(new[] {
            new Microsoft.Extensions.Logging.Debug.DebugLoggerProvider()
        });

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.EnableSensitiveDataLogging(true)
                            .UseLoggerFactory(_myLoggerFactory)
                            .LogTo(Console.WriteLine);
        }

        public virtual DbSet<Article> Articles { get; set; }
        public virtual DbSet<ArticlesCategory> ArticlesCategories { get; set; }
        public virtual DbSet<Author> Authors { get; set; }
        public virtual DbSet<Authorship> Authorships { get; set; }
        public virtual DbSet<Category> Categories { get; set; }
        public virtual DbSet<Comment> Comments { get; set; }

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
                    .HasDefaultValue(DateTime.Now);
                entity.Property(e => e.UpdatedAt)
                    .HasDefaultValue(DateTime.Now);
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
            });

            modelBuilder.Entity<Author>(entity =>
            {
                entity.Property(e => e.FirstName).IsRequired();
                //entity.Property(e => e.LastName).IsRequired();
                entity.Property(e => e.Id).ValueGeneratedOnAdd();

                entity.HasMany(d => d.Authorships)
                    .WithOne(p => p.Author)
                    .HasForeignKey(d => d.AuthorId);
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
            });

            modelBuilder.Entity<Category>(entity =>
            {
                entity.Property(e => e.Title).IsRequired();
                entity.Property(e => e.Id).ValueGeneratedOnAdd();

                entity.HasMany(d => d.ArticlesCategories)
                    .WithOne(p => p.Category)
                    .HasForeignKey(d => d.CategoryId);
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
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
