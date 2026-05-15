using elasticsearch_netcore.Constants;
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
    public class CategoryRepository : GenericRepository<Category>, ICategoryRepository
    {
        private readonly Helpers.Helper _helper;
        private readonly ILogger<CategoryRepository> _logger;

        public CategoryRepository(ElasticsearchDBContext db, ILogger<CategoryRepository> logger, Helpers.Helper helper)
            : base(db)
        {
            _helper = helper;
            _logger = logger;
        }

        public async Task<dynamic> GetCategories(int skip, int take = AppConstants.DefaultPageSize,
            Expression<Func<Category, bool>> filter = null, bool showTotal = false)
        {
            if (_context != null)
            {
                if (skip < 0)
                    skip = 0;
                if (take > AppConstants.MaxPageSize || take <= 0)
                    take = AppConstants.DefaultPageSize;

                List<CategoryViewModel> categories = new List<CategoryViewModel>();

                var table = _context.Categories.AsQueryable().AsNoTracking();

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

        public async Task<dynamic> GetCategories(int skip, int take = AppConstants.DefaultPageSize,
            string title = "", bool showTotal = false)
        {
            if (_context != null)
            {
                if (skip < 0)
                    skip = 0;
                if (take > AppConstants.MaxPageSize || take <= 0)
                    take = AppConstants.DefaultPageSize;

                JArray categories = new JArray();

                IQueryable<Category> table = _context.Categories.AsNoTracking();

                if (!string.IsNullOrWhiteSpace(title))
                {
                    table = table.Where(c => c.Title.Contains(title));
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
            if (_context != null)
            {
                var record = new Category();

                record.Title = category.Title;
                record.UpdatedAt = DateTime.Now;
                record.CreatedAt = DateTime.Now;

                var result = await _context.Categories.AddAsync(record);
                await _context.SaveChangesAsync();
                return new CategoryViewModel(result.Entity);
            }

            return null;
        }

        public async Task<CategoryViewModel> UpdateCategory(long id, CategoryViewModel category)
        {
            if (_context != null && category != null && id > 0)
            {
                var found = await _context.Categories.FindAsync(id);
                if (found != null)
                {
                    try
                    {
                        var entry = _context.Entry(found);
                        entry.State = EntityState.Modified;

                        found.Title = category.Title;
                        found.UpdatedAt = DateTime.Now;

                        await _context.SaveChangesAsync();

                        // index articles
                        await BulkIndexArticles(id);

                        return new CategoryViewModel(found);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error updating category {CategoryId}", id);
                    }
                }
            }

            return null;
        }

        public async Task<CategoryViewModel> GetCategory(long id)
        {
            if (_context != null && id > 0)
            {
                var record = await _context.Categories.AsQueryable().AsNoTracking().SingleOrDefaultAsync(a => a.Id == id);
                if (record != null)
                    return new CategoryViewModel(record, false);
            }

            return null;
        }

        public async Task<dynamic> GetArticlesForCategory(long id, int skip, int take, string title = "",
           bool loadRelation = false, bool showTotal = false)
        {
            if (_context != null && id > 0)
            {
                if (skip < 0)
                    skip = 0;
                if (take > AppConstants.MaxPageSize || take <= 0)
                    take = AppConstants.DefaultPageSize;

                JArray articles = new JArray();
                IQueryable<ArticlesCategory> table = _context.ArticlesCategories.AsNoTracking().Where(a => a.CategoryId == id);

                if (!string.IsNullOrWhiteSpace(title))
                {
                    table = table.Where(ac => ac.Article.Title.Contains(title));
                }

                if (loadRelation)
                {
                    table = table.Include(a => a.Article)
                                .ThenInclude(a => a.Authorships).ThenInclude(a => a.Author)
                                .AsSplitQuery();
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
            if (_context != null && id > 0)
            {
                _context.Categories.Remove(new Category() { Id = id });
                await _context.SaveChangesAsync();
                return true;
            }

            return false;
        }

        async Task BulkIndexArticles(long id)
        {
            var records = await _context.ArticlesCategories.AsNoTracking()
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