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
    public class CategoryRepository : ICategoryRepository
    {
        private ElasticsearchDBContext db;
        private readonly Helpers.Helper _helper;
        private readonly ILogger<CategoryRepository> _logger;

        public CategoryRepository(ElasticsearchDBContext db, ILogger<CategoryRepository> logger, Helpers.Helper helper)
        {
            this.db = db;
            _logger = logger;
            _helper = helper;
        }

        public async Task<dynamic> GetCategories(int skip, int take = 10,
            Expression<Func<Category, bool>> filter = null, bool showTotal = false)
        {
            if (db != null)
            {
                if (skip < 0)
                    skip = 0;
                if (take > 50 || take <= 0)
                    take = 10;

                List<CategoryViewModel> categories = new List<CategoryViewModel>();

                var table = db.Categories.AsQueryable().AsNoTracking();

                if (filter != null)
                    table = table.Where(filter);

                var records = await table.OrderBy(a => a.Title).ThenBy(a => a.CreatedAt).Skip(skip).Take(take).ToListAsync();

                foreach (var r in records)
                    categories.Add(new CategoryViewModel(r, false));

                if (showTotal)
                    return new { data = categories, total = await table.CountAsync() };
                else
                    return categories;
            }

            return null;
        }

        public async Task<dynamic> GetCategories(int skip, int take = 10,
            string title = "", bool showTotal = false)
        {
            if (db != null)
            {
                if (skip < 0)
                    skip = 0;
                if (take > 50 || take <= 0)
                    take = 10;

                JArray categories = new JArray();

                IQueryable<Category> table = null;

                if (string.IsNullOrWhiteSpace(title))
                    table = db.Categories.AsNoTracking();
                else
                {
                    var titleParam = new SqliteParameter("@title", string.Format("%{0}%", title));
                    table = db.Categories.FromSqlRaw("select * from categories WHERE title LIKE @title", titleParam);
                }

                var records = await table.OrderBy(a => a.Title).ThenBy(a => a.CreatedAt).Skip(skip).Take(take).ToListAsync();

                foreach (var r in records)
                    categories.Add(JObject.FromObject(new CategoryViewModel(r, false)));

                if (showTotal)
                    return new { data = categories, total = await table.CountAsync() };
                else
                    return categories;
            }

            return null;
        }

        public async Task<CategoryViewModel> CreateCategory(CategoryViewModel category)
        {
            if (db != null)
            {
                var record = new Category();

                record.Title = category.Title;
                record.UpdatedAt = DateTime.Now;
                record.CreatedAt = DateTime.Now;

                var result = await db.Categories.AddAsync(record);
                await db.SaveChangesAsync();
                return new CategoryViewModel(result.Entity);
            }

            return null;
        }

        public async Task<CategoryViewModel> UpdateCategory(long id, CategoryViewModel category)
        {
            if (db != null && category != null && id > 0)
            {
                var found = await db.Categories.FindAsync(id);
                if (found != null)
                {
                    try
                    {
                        var entry = db.Entry(found);
                        entry.State = EntityState.Modified;

                        found.Title = category.Title;
                        found.UpdatedAt = DateTime.Now;

                        await db.SaveChangesAsync();

                        // index articles
                        await BulkIndexArticles(id);

                        return new CategoryViewModel(found);
                    }
                    catch (Exception ex)
                    {
                    }
                }
            }

            return null;
        }

        public async Task<CategoryViewModel> GetCategory(long id)
        {
            if (db != null && id > 0)
            {
                var record = await db.Categories.AsQueryable().AsNoTracking().SingleOrDefaultAsync(a => a.Id == id);
                if (record != null)
                    return new CategoryViewModel(record, false);
            }

            return null;
        }

        public async Task<dynamic> GetArticlesForCategory(long id, int skip, int take, string title = "",
           bool loadRelation = false, bool showTotal = false)
        {
            if (db != null && id > 0)
            {
                if (skip < 0)
                    skip = 0;
                if (take > 50 || take <= 0)
                    take = 10;

                JArray articles = new JArray();
                IQueryable<ArticlesCategory> table = null;

                if (string.IsNullOrWhiteSpace(title))
                    table = db.ArticlesCategories.AsNoTracking().Where(a => a.CategoryId == id);
                else
                {
                    var titleParam = new SqliteParameter("@title", string.Format("%{0}%", title));
                    var categoryParam = new SqliteParameter("@categoryId", id);
                    table = db.ArticlesCategories.FromSqlRaw("select * from articles_categories where category_id = @categoryId and " +
                        "article_id in (select id from articles WHERE title LIKE @title)", titleParam, categoryParam);
                }

                if (loadRelation)
                {
                    table = table.Include(a => a.Article)
                                .ThenInclude(a => a.Authorships).ThenInclude(a => a.Author);
                }
                else
                    table = table.Include(a => a.Article);

                var records = await table.OrderBy(x => x.Article.Title).Skip(skip).Take(take).ToListAsync();

                foreach (var r in records)
                {
                    var article = ArticleRepository.ConvertToJObject(new ArticleViewModel(r.Article, true), loadRelation);
                    article.Remove("categories");
                    articles.Add(article);
                }

                if (showTotal)
                    return new { data = articles, total = await table.CountAsync() };
                else
                    return articles;
            }

            return null;
        }

        public async Task<bool> DeleteCategory(long id)
        {
            if (db != null && id > 0)
            {
                db.Categories.Remove(new Category() { Id = id });
                await db.SaveChangesAsync();
                return true;
            }

            return false;
        }

        async Task BulkIndexArticles(long id)
        {
            var records = await db.ArticlesCategories.AsNoTracking()
                            .Where(a => a.CategoryId == id)
                            .Include(a => a.Article)
                            .ThenInclude(a => a.Authorships)
                            .ThenInclude(a => a.Author)
                            .Include(a => a.Article)
                            .ThenInclude(a => a.Comments)
                            .Include(a => a.Category)
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