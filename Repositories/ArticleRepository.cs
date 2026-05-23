using elasticsearch_netcore.Constants;
using elasticsearch_netcore.Models;
using elasticsearch_netcore.ViewModels;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Nest;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace elasticsearch_netcore.Repositories
{
    public class ArticleRepository : GenericRepository<Article>, IArticleRepository
    {
        private readonly ILogger<ArticleRepository> _logger;
        private readonly Helpers.Helper _helper;
        private readonly IMemoryCache _cache;
        private readonly IConfiguration _configuration;
        private readonly AutoMapper.IMapper _mapper;

        public ArticleRepository(ElasticsearchDBContext db, ILogger<ArticleRepository> logger, Helpers.Helper helper, IMemoryCache cache, IConfiguration configuration, AutoMapper.IMapper mapper)
            : base(db)
        {
            _logger = logger;
            _helper = helper;
            _cache = cache;
            _configuration = configuration;
            _mapper = mapper;
        }

        #region action

        private void InvalidateArticleCache(long? articleId = null)
        {
            // Invalidate article list cache (all variations)
            var cacheKeys = new List<string>();
            
            // We can't enumerate all cache keys easily, so we'll use a simple approach
            // In production, consider using a cache key prefix pattern with Redis
            if (articleId.HasValue)
            {
                // Invalidate specific article cache
                _cache.Remove($"article_{articleId.Value}_true");
                _cache.Remove($"article_{articleId.Value}_false");
                _logger.LogInformation("Invalidated cache for article {ArticleId}", articleId.Value);
            }
            
            // For simplicity, we'll rely on cache expiration for list caches
            // In production with Redis, you could use key patterns to delete all matching keys
            _logger.LogInformation("Article cache invalidation triggered");
        }

        public async Task<dynamic> GetArticles(int skip, int take = AppConstants.DefaultPageSize, bool loadRelation = false,
            Expression<Func<Article, bool>> filter = null, bool showTotal = false)
        {
            if (_context != null)
            {
                if (skip < 0)
                    skip = 0;
                if (take > AppConstants.MaxPageSize || take <= 0)
                    take = AppConstants.DefaultPageSize;

                var cacheKey = $"articles_{skip}_{take}_{loadRelation}_{showTotal}_{filter?.ToString() ?? "nofilter"}";

                if (_cache.TryGetValue(cacheKey, out dynamic cachedResult))
                {
                    _logger.LogInformation("Cache hit for articles list (skip: {Skip}, take: {Take})", skip, take);
                    return cachedResult;
                }

                _logger.LogInformation("Cache miss for articles list (skip: {Skip}, take: {Take})", skip, take);

                var table = _context.Articles.AsQueryable().AsNoTracking();

                if (loadRelation)
                {
                    table = table.Include(a => a.Authorships).ThenInclude(a => a.Author)
                                .Include(a => a.ArticlesCategories).ThenInclude(a => a.Category)
                                .AsSplitQuery();
                }

                if (filter != null)
                    table = table.Where(filter);

                var records = await table.OrderBy(a => a.Title).ThenBy(a => a.CreatedAt).Skip(skip).Take(take).ToListAsync();

                var articles = _mapper.Map<List<ArticleViewModel>>(records);

                dynamic result;
                if (showTotal)
                {
                    var countQuery = _context.Articles.AsNoTracking();
                    if (filter != null)
                        countQuery = countQuery.Where(filter);
                    result = new { data = articles, total = await countQuery.CountAsync() };
                }
                else
                    result = articles;

                var cacheExpiration = _configuration.GetValue<int>("CacheSettings:ArticleListExpirationMinutes", 5);
                _cache.Set(cacheKey, (object)result, TimeSpan.FromMinutes(cacheExpiration));

                return result;
            }

            return null;
        }

        public async Task<dynamic> GetArticles(int skip, int take = AppConstants.DefaultPageSize,
            string title = "", bool loadRelation = false, bool showTotal = false)
        {
            if (_context != null)
            {
                if (skip < 0)
                    skip = 0;
                if (take > AppConstants.MaxPageSize || take <= 0)
                    take = AppConstants.DefaultPageSize;

                JArray articles = new JArray();

                IQueryable<Article> table = null;

                if (string.IsNullOrWhiteSpace(title))
                    table = _context.Articles.AsNoTracking();
                else
                    table = _context.Articles.AsNoTracking().Where(a => a.Title.Contains(title));

                if (loadRelation)
                {
                    table = table.Include(a => a.Authorships).ThenInclude(a => a.Author)
                                .Include(a => a.ArticlesCategories).ThenInclude(a => a.Category)
                                .AsSplitQuery();
                }

                var records = await table.OrderBy(a => a.Title).ThenBy(a => a.CreatedAt).Skip(skip).Take(take).ToListAsync();

                var articleViewModels = _mapper.Map<List<ArticleViewModel>>(records);
                foreach (var vm in articleViewModels)
                    articles.Add(ConvertToJObject(vm, loadRelation, ForPage.All));

                if (showTotal)
                {
                    var countQuery = _context.Articles.AsNoTracking();
                    if (!string.IsNullOrWhiteSpace(title))
                        countQuery = countQuery.Where(a => a.Title.Contains(title));
                    return new { data = articles, total = await countQuery.CountAsync() };
                }
                else
                    return articles;
            }

            return null;
        }

        public async Task<dynamic> GetCommentsForArticle(long id, int skip, int take, bool showTotal = false)
        {
            if (_context != null && id > 0)
                return await (new CommentRepository(_context, _mapper)).GetComments(skip, take, false, c => c.ArticleId == id, showTotal);

            return null;
        }

        public async Task<ArticleViewModel> CreateArticle(ArticleViewModel article)
        {
            if (_context != null)
            {
                if (string.IsNullOrWhiteSpace(article?.Title))
                    throw new ArgumentException("Title is required.", "title");
                if (string.IsNullOrWhiteSpace(article?.Content))
                    throw new ArgumentException("Content is required.", "content");

                var record = new Article();
                record.Title = article.Title;
                record.Content = article.Content;
                record.Abstract = article.Abstract;
                record.Shares = article.Shares;
                record.PublishedOn = article.PublishedOn;

                UpdateCategoriesRelation(record, article.ArticlesCategories != null ? article.ArticlesCategories.ToList() : null);
                UpdateAuthorsRelation(record, article.Authorships != null ? article.Authorships.ToList() : null);

                var result = await _context.Articles.AddAsync(record);
                await _context.SaveChangesAsync();

                await IndexDocument(result.Entity.Id);

                InvalidateArticleCache();

                return _mapper.Map<ArticleViewModel>(result.Entity);
            }

            return null;
        }

        public async Task<ArticleViewModel> UpdateArticle(long id, ArticleViewModel article)
        {
            if (_context != null && article != null && id > 0)
            {
                if (string.IsNullOrWhiteSpace(article?.Title))
                    throw new ArgumentException("Title is required.", "title");
                if (string.IsNullOrWhiteSpace(article?.Content))
                    throw new ArgumentException("Content is required.", "content");

                var found = await _context.Articles.Include(a => a.ArticlesCategories).Include(a => a.Authorships).FirstOrDefaultAsync(a => a.Id == id);
                if (found != null)
                {
                    found.Title = article.Title;
                    found.Content = article.Content;
                    found.Abstract = article.Abstract;
                    found.Shares = article.Shares;
                    found.PublishedOn = article.PublishedOn;
                    found.UpdatedAt = DateTime.Now;

                    UpdateCategoriesRelation(found, article.ArticlesCategories != null ? article.ArticlesCategories.ToList() : null);
                    UpdateAuthorsRelation(found, article.Authorships != null ? article.Authorships.ToList() : null);

                    await _context.SaveChangesAsync();

                    await IndexDocument(found.Id);

                    InvalidateArticleCache(found.Id);

                    return _mapper.Map<ArticleViewModel>(found);
                }
            }

            return null;
        }

        public async Task<ArticleViewModel> GetArticle(long id, bool loadRelation)
        {
            if (_context != null && id > 0)
            {
                var cacheKey = $"article_{id}_{loadRelation}";

                if (_cache.TryGetValue(cacheKey, out ArticleViewModel cachedArticle))
                {
                    _logger.LogInformation("Cache hit for article {ArticleId}", id);
                    return cachedArticle;
                }

                _logger.LogInformation("Cache miss for article {ArticleId}", id);

                var table = _context.Articles.AsQueryable().AsNoTracking();
                if (loadRelation)
                {
                    table = table.Include(a => a.Authorships).ThenInclude(a => a.Author)
                                .Include(a => a.ArticlesCategories).ThenInclude(a => a.Category)
                                .Include(a => a.Comments)
                                .AsSplitQuery();
                }

                var record = await table.SingleOrDefaultAsync(a => a.Id == id);
                if (record != null)
                {
                    var article = _mapper.Map<ArticleViewModel>(record);

                    var cacheExpiration = _configuration.GetValue<int>("CacheSettings:ArticleExpirationMinutes", AppConstants.DefaultCacheExpirationMinutes);
                    _cache.Set(cacheKey, article, TimeSpan.FromMinutes(cacheExpiration));

                    return article;
                }
            }

            return null;
        }

        public async Task<bool> DeleteArticle(long id)
        {
            if (_context != null && id > 0)
            {
                var article = await _context.Articles.FindAsync(id);
                if (article == null)
                    return false;

                var lstAC = _context.ArticlesCategories.Where(ac => ac.ArticleId == id).ToList();
                _context.ArticlesCategories.RemoveRange(lstAC);
                var lstAA = _context.Authorships.Where(aa => aa.ArticleId == id).ToList();
                _context.Authorships.RemoveRange(lstAA);
                var lstACC = _context.Comments.Where(acc => acc.ArticleId == id).ToList();
                _context.Comments.RemoveRange(lstACC);
                _context.Articles.Remove(article);
                await _context.SaveChangesAsync();

                await _helper.RemoveIndexDocument(id.ToString());
                return true;
            }

            return false;
        }

        public async Task BulkIndex()
        {
            bool isContinue = true;
            int pageSize = AppConstants.BulkIndexPageSize;
            int pageIndex = 1;
            int total = 0;
            while (isContinue)
            {
                var articles = await GetArticles((pageIndex - 1) * pageSize, pageSize, true, null, false);
                if (articles != null && articles.Count > 0)
                {
                    var lst = (articles as List<ArticleViewModel>).Select(a => a.AsIndexedJson()).ToDictionary(a => a.Value<string>("id"), a => JsonConvert.SerializeObject(a));
                    isContinue = await _helper.BulkIndexDocument(lst);
                    if (isContinue)
                    {
                        total += articles.Count;
                        _logger.LogInformation($"Done: {articles.Count}, Total: {total}");
                        pageIndex += 1;
                    }
                    else
                        _logger.LogInformation($"Can not continue, stop bulk index...."); ;
                }
                else
                {
                    isContinue = false;
                    _logger.LogInformation($"Total: {total}");
                }
            }
        }

        #endregion


        #region helper

        async Task IndexDocument(long articleId)
        {
            if (_helper == null)
                return;

            try
            {
                var articleViewModel = await GetArticle(articleId, true);
                await _helper.IndexDocument(articleId.ToString(), JsonConvert.SerializeObject(articleViewModel.AsIndexedJson()));
            }
            catch (Exception ex)
            {
                _logger.LogInformation("Can not index document: " + ex.Message);
            }
        }

        public static JObject ConvertToJObject(ArticleViewModel record, bool loadRelation = false, ForPage forPage = ForPage.All)
        {
            if (record == null)
                return null;

            JObject article = null;
            if (loadRelation)
            {
                var categories = JArray.FromObject((record.ArticlesCategories ?? Array.Empty<ArticlesCategoryViewModel>()).Select(x => x.Category));
                var authors = JArray.FromObject((record.Authorships ?? Array.Empty<AuthorshipViewModel>()).Select(x => x.Author));
                record.ArticlesCategories = null;
                record.Authorships = null;

                article = JObject.FromObject(record);

                article.Add("categories", categories);
                article.Add("authors", authors);
            }
            else
            {
                if (forPage == ForPage.Detail)
                {
                    var categories = JArray.FromObject((record.ArticlesCategories ?? Array.Empty<ArticlesCategoryViewModel>()).Select(x => new { category_id = x.CategoryId }));
                    var authors = JArray.FromObject((record.Authorships ?? Array.Empty<AuthorshipViewModel>()).Select(x => new { author_id = x.AuthorId }));
                    record.ArticlesCategories = null;
                    record.Authorships = null;

                    article = JObject.FromObject(record);

                    article.Add("categories", categories);
                    article.Add("authors", authors);
                }
                else
                    article = JObject.FromObject(record);
            }

            if ((loadRelation == true && forPage == ForPage.All) || loadRelation == false)
                article.Remove("comments");
            else
            {
                var comments = article.SelectTokens("comments.[*]").ToList();
                if (comments != null)
                {
                    for (int i = 0; i < comments.Count; i++)
                        (comments[i] as JObject).Remove("article");
                }
            }

            return article;
        }


        void UpdateCategoriesRelation(Article article, List<ArticlesCategoryViewModel> lst)
        {
            if (article == null)
                return;

            if (lst == null || lst.Count == 0)
            {
                article.ArticlesCategories = null;
                return;
            }

            if (article.ArticlesCategories == null || article.ArticlesCategories.Count == 0)
            {
                article.ArticlesCategories = lst.Select(ac => new ArticlesCategory() { CategoryId = ac.CategoryId }).ToList();
                return;
            }

            if (lst.Count < article.ArticlesCategories.Count)
            {
                while (lst.Count > article.ArticlesCategories.Count)
                    article.ArticlesCategories.Remove(article.ArticlesCategories.First());
            }

            int countExisted = article.ArticlesCategories.Count;
            for (int i = 0, j = lst.Count; i < j; i++)
            {
                if (i < countExisted)
                    article.ArticlesCategories.ElementAt(i).CategoryId = lst[i].CategoryId;
                else
                    article.ArticlesCategories.Add(new ArticlesCategory() { CategoryId = lst[i].CategoryId });
            }
        }

        void UpdateAuthorsRelation(Article article, List<AuthorshipViewModel> lst)
        {
            if (article == null)
                return;

            if (lst == null || lst.Count == 0)
            {
                article.Authorships = null;
                return;
            }

            if (article.Authorships == null || article.Authorships.Count == 0)
            {
                article.Authorships = lst.Select(ac => new Authorship() { AuthorId = ac.AuthorId }).ToList();
                return;
            }

            if (lst.Count < article.Authorships.Count)
            {
                while (lst.Count > article.Authorships.Count)
                    article.Authorships.Remove(article.Authorships.First());
            }

            int countExisted = article.Authorships.Count;
            for (int i = 0, j = lst.Count; i < j; i++)
            {
                if (i < countExisted)
                    article.Authorships.ElementAt(i).AuthorId = lst[i].AuthorId;
                else
                    article.Authorships.Add(new Authorship() { AuthorId = lst[i].AuthorId });
            }
        }

        #endregion
    }
}