using elasticsearch_netcore.Models;
using elasticsearch_netcore.ViewModels;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace elasticsearch_netcore.Repositories
{
    public class AuthorRepository : IAuthorRepository
    {
        private ElasticsearchDBContext db;

        private readonly Helpers.Helper _helper;
        private readonly ILogger<AuthorRepository> _logger;

        public AuthorRepository(ElasticsearchDBContext db, ILogger<AuthorRepository> logger, Helpers.Helper helper)
        {
            this.db = db;
            _logger = logger;
            _helper = helper;
        }

        public async Task<dynamic> GetAuthors(int skip, int take = 10,
            Expression<Func<Author, bool>> filter = null, bool showTotal = false)
        {
            if (db != null)
            {
                if (skip < 0)
                    skip = 0;
                if (take > 50 || take <= 0)
                    take = 10;

                List<AuthorViewModel> authors = new List<AuthorViewModel>();

                var table = db.Authors.AsQueryable().AsNoTracking();

                if (filter != null)
                    table = table.Where(filter);

                var records = await table.OrderBy(a => a.FirstName).ThenBy(a => a.LastName).ThenBy(a => a.CreatedAt)
                    .Skip(skip).Take(take).ToListAsync();

                foreach (var r in records)
                    authors.Add(new AuthorViewModel(r, false));

                if (showTotal)
                    return new { data = authors, total = await table.CountAsync() };
                else
                    return authors;
            }

            return null;
        }

        public async Task<dynamic> GetAuthors(int skip, int take = 10, string name = "", bool showTotal = false)
        {
            if (db != null)
            {
                if (skip < 0)
                    skip = 0;
                if (take > 50 || take <= 0)
                    take = 10;

                JArray authors = new JArray();

                IQueryable<Author> table = db.Authors.AsNoTracking();

                if (!string.IsNullOrWhiteSpace(name))
                {
                    table = table.Where(a => (a.FirstName + " " + a.LastName).Contains(name));
                }

                var records = await table.OrderBy(a => a.FirstName).ThenBy(a => a.LastName).ThenBy(a => a.CreatedAt)
                    .Skip(skip).Take(take).ToListAsync();

                foreach (var r in records)
                    authors.Add(JObject.FromObject(new AuthorViewModel(r, false)));

                if (showTotal)
                    return new { data = authors, total = await table.CountAsync() };
                else
                    return authors;
            }

            return null;
        }

        public async Task<AuthorViewModel> CreateAuthor(AuthorViewModel author)
        {
            if (db != null)
            {
                var record = new Author();

                record.FirstName = author.FirstName;
                record.LastName = author.LastName;
                record.UpdatedAt = DateTime.Now;
                record.CreatedAt = DateTime.Now;

                var result = await db.Authors.AddAsync(record);
                await db.SaveChangesAsync();
                return new AuthorViewModel(result.Entity);
            }

            return null;
        }

        public async Task<AuthorViewModel> UpdateAuthor(long id, AuthorViewModel author)
        {
            if (db != null && author != null && id > 0)
            {
                var found = await db.Authors.FindAsync(id);
                if (found != null)
                {
                    try
                    {
                        var entry = db.Entry(found);
                        entry.State = EntityState.Modified;

                        found.FirstName = author.FirstName;
                        found.LastName = author.LastName;
                        found.UpdatedAt = DateTime.Now;

                        await db.SaveChangesAsync();

                        // index articles
                        await BulkIndexArticles(id);

                        return new AuthorViewModel(found);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error updating author {AuthorId}", id);
                    }
                }
            }

            return null;
        }

        public async Task<AuthorViewModel> GetAuthor(long id)
        {
            if (db != null && id > 0)
            {
                var record = await db.Authors.AsQueryable().AsNoTracking().SingleOrDefaultAsync(a => a.Id == id);
                if (record != null)
                    return new AuthorViewModel(record, false);
            }

            return null;
        }

        public async Task<dynamic> GetArticlesForAuthor(long id, int skip, int take, string title = "",
            bool loadRelation = false, bool showTotal = false)
        {
            if (db != null && id > 0)
            {
                if (skip < 0)
                    skip = 0;
                if (take > 50 || take <= 0)
                    take = 10;

                JArray articles = new JArray();
                IQueryable<Authorship> table = db.Authorships.AsNoTracking().Where(a => a.AuthorId == id);

                if (!string.IsNullOrWhiteSpace(title))
                {
                    table = table.Where(a => a.Article.Title.Contains(title));
                }

                if (loadRelation)
                {
                    table = table.Include(a => a.Article)
                                .ThenInclude(a => a.ArticlesCategories).ThenInclude(a => a.Category)
                                .AsSplitQuery();
                }
                else
                    table = table.Include(a => a.Article);

                var records = await table.OrderBy(x => x.Article.Title).Skip(skip).Take(take).ToListAsync();

                foreach (var r in records)
                {
                    var article = ArticleRepository.ConvertToJObject(new ArticleViewModel(r.Article, true), loadRelation);
                    article.Remove("authors");
                    articles.Add(article);
                }

                if (showTotal)
                    return new { data = articles, total = await table.CountAsync() };
                else
                    return articles;
            }

            return null;
        }

        public async Task<bool> DeleteAuthor(long id)
        {
            if (db != null && id > 0)
            {
                db.Authors.Remove(new Author() { Id = id });
                await db.SaveChangesAsync();
                return true;
            }

            return false;
        }

        async Task BulkIndexArticles(long id)
        {
            var records = await db.Authorships.AsNoTracking()
                            .Where(a => a.AuthorId == id)
                            .Include(a => a.Article)
                            .ThenInclude(a => a.ArticlesCategories)
                            .ThenInclude(a => a.Category)
                            .Include(a => a.Article)
                            .ThenInclude(a => a.Comments)
                            .Include(a => a.Author)
                            .ToListAsync();
            IEnumerable<ArticleViewModel> articles = records.Select(r => new ArticleViewModel(r.Article, true));

            if (articles != null && articles.Count() > 0)
            {
                var lst = articles.Select(a => a.AsIndexedJson()).ToDictionary(a => a.Value<string>("id"), a => JsonConvert.SerializeObject(a));
                await _helper.BulkIndexDocument(lst);
                _logger.LogInformation($"Done: {articles.Count()}");
            }
        }
    }
}