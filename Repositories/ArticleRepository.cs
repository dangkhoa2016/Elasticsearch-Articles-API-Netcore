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
using Newtonsoft.Json;
using Microsoft.Data.Sqlite;

namespace elasticsearch_netcore.Repositories
{
    public class ArticleRepository : IArticleRepository
    {
        private ElasticsearchDBContext db;

        private readonly ILogger<ArticleRepository> _logger;
        private readonly Helpers.Helper _helper;
        public ArticleRepository(ElasticsearchDBContext db, ILogger<ArticleRepository> logger, Helpers.Helper helper)
        {
            this.db = db;
            _logger = logger;
            _helper = helper;
        }

        #region action

        public async Task<dynamic> GetArticles(int skip, int take = 10, bool loadRelation = false,
            Expression<Func<Article, bool>> filter = null, bool showTotal = false)
        {
            if (db != null)
            {
                if (skip < 0)
                    skip = 0;
                if (take > 50 || take <= 0)
                    take = 10;

                List<ArticleViewModel> articles = new List<ArticleViewModel>();

                var table = db.Articles.AsQueryable().AsNoTracking();

                if (loadRelation)
                {
                    table = table.Include(a => a.Authorships).ThenInclude(a => a.Author)
                                .Include(a => a.ArticlesCategories).ThenInclude(a => a.Category);
                    //.Include(a => a.Comments);
                }

                if (filter != null)
                    table = table.Where(filter);

                var records = await table.OrderBy(a => a.Title).ThenBy(a => a.CreatedAt).Skip(skip).Take(take).ToListAsync();

                foreach (var r in records)
                    articles.Add(new ArticleViewModel(r, true));

                if (showTotal)
                    return new { data = articles, total = await table.CountAsync() };
                else
                    return articles;
            }

            return null;
        }

        public async Task<dynamic> GetArticles(int skip, int take = 10,
            string title = "", bool loadRelation = false, bool showTotal = false)
        {
            if (db != null)
            {
                if (skip < 0)
                    skip = 0;
                if (take > 50 || take <= 0)
                    take = 10;

                JArray articles = new JArray();

                IQueryable<Article> table = null;

                if (string.IsNullOrWhiteSpace(title))
                    table = db.Articles.AsNoTracking();
                else
                {
                    var titleParam = new SqliteParameter("@title", string.Format("%{0}%", title));
                    table = db.Articles.FromSqlRaw("select * from articles WHERE title LIKE @title", titleParam);
                }

                if (loadRelation)
                {
                    table = table.Include(a => a.Authorships).ThenInclude(a => a.Author)
                                .Include(a => a.ArticlesCategories).ThenInclude(a => a.Category);
                    //.Include(a => a.Comments);
                }

                var records = await table.OrderBy(a => a.Title).ThenBy(a => a.CreatedAt).Skip(skip).Take(take).ToListAsync();

                foreach (var r in records)
                    articles.Add(ConvertToJObject(new ArticleViewModel(r, true), loadRelation, ForPage.All));

                if (showTotal)
                    return new { data = articles, total = await table.CountAsync() };
                else
                    return articles;
            }

            return null;
        }

        public async Task<dynamic> GetCommentsForArticle(long id, int skip, int take, bool showTotal = false)
        {
            if (db != null && id > 0)
                return await (new CommentRepository(db)).GetComments(skip, take, false, c => c.ArticleId == id, showTotal);

            return null;
        }

        public async Task<ArticleViewModel> CreateArticle(ArticleViewModel article)
        {
            if (db != null)
            {
                var record = new Article();
                record.Title = article.Title;
                record.Content = article.Content;
                record.Abstract = article.Abstract;
                record.Shares = article.Shares;
                record.PublishedOn = article.PublishedOn;
                //record.UpdatedAt = DateTime.Now;
                //record.CreatedAt = DateTime.Now;

                UpdateCategoriesRelation(record, article.ArticlesCategories != null ? article.ArticlesCategories.ToList() : null);
                UpdateAuthorsRelation(record, article.Authorships != null ? article.Authorships.ToList() : null);

                var result = await db.Articles.AddAsync(record);
                await db.SaveChangesAsync();

                await IndexDocument(result.Entity.Id);

                return new ArticleViewModel(result.Entity, true);
            }

            return null;
        }

        public async Task<ArticleViewModel> UpdateArticle(long id, ArticleViewModel article)
        {
            if (db != null && article != null && id > 0)
            {
                var found = await db.Articles.Include(a => a.ArticlesCategories).Include(a => a.Authorships).FirstOrDefaultAsync(a => a.Id == id);
                if (found != null)
                {
                    found.Title = article.Title;
                    found.Content = article.Content;
                    found.Abstract = article.Abstract;
                    found.Shares = article.Shares;
                    found.PublishedOn = article.PublishedOn;
                    found.UpdatedAt = DateTime.Now;
                    //found.CreatedAt = DateTime.Now;

                    UpdateCategoriesRelation(found, article.ArticlesCategories != null ? article.ArticlesCategories.ToList() : null);
                    UpdateAuthorsRelation(found, article.Authorships != null ? article.Authorships.ToList() : null);

                    await db.SaveChangesAsync();

                    await IndexDocument(found.Id);

                    return new ArticleViewModel(found, true);
                }
            }

            return null;
        }

        public async Task<ArticleViewModel> GetArticle(long id, bool loadRelation)
        {
            if (db != null && id > 0)
            {
                var table = db.Articles.AsQueryable().AsNoTracking();
                if (loadRelation)
                {
                    table = table.Include(a => a.Authorships).ThenInclude(a => a.Author)
                                .Include(a => a.ArticlesCategories).ThenInclude(a => a.Category)
                                .Include(a => a.Comments);
                }
                else
                {
                    //table = table.Include(a => a.Authorships)
                    //            .Include(a => a.ArticlesCategories);
                }

                var record = await table.SingleOrDefaultAsync(a => a.Id == id);
                if (record != null)
                {
                    return new ArticleViewModel(record, true);
                }
            }

            return null;
        }

        public async Task<bool> DeleteArticle(long id)
        {
            if (db != null && id > 0)
            {
                try
                {
                    var lstAC = db.ArticlesCategories.Where(ac => ac.ArticleId == id).ToList();
                    db.ArticlesCategories.RemoveRange(lstAC);
                    var lstAA = db.Authorships.Where(aa => aa.ArticleId == id).ToList();
                    db.Authorships.RemoveRange(lstAA);
                    var lstACC = db.Comments.Where(acc => acc.ArticleId == id).ToList();
                    db.Comments.RemoveRange(lstACC);
                    db.Articles.Remove(new Article() { Id = id });
                    await db.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                }

                await _helper.RemoveIndexDocument(id.ToString());
                return true;
            }

            return false;
        }

        public async Task BulkIndex()
        {
            bool isContinue = true;
            int pageSize = 30;
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
            JObject article = null;
            if (loadRelation)
            {
                var categories = JArray.FromObject(record.ArticlesCategories.Select(x => x.Category));
                var authors = JArray.FromObject(record.Authorships.Select(x => x.Author));
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
                    var categories = JArray.FromObject(record.ArticlesCategories.Select(x => new { category_id = x.CategoryId }));
                    var authors = JArray.FromObject(record.Authorships.Select(x => new { author_id = x.AuthorId }));
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