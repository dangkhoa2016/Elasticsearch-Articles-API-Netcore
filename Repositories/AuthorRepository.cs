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
    public class AuthorRepository : GenericRepository<Author>, IAuthorRepository
    {
        private readonly Helpers.Helper _helper;
        private readonly ILogger<AuthorRepository> _logger;
        private readonly AutoMapper.IMapper _mapper;

        public AuthorRepository(ElasticsearchDBContext db, ILogger<AuthorRepository> logger, Helpers.Helper helper, AutoMapper.IMapper mapper)
            : base(db)
        {
            _helper = helper;
            _logger = logger;
            _mapper = mapper;
        }

        public async Task<dynamic> GetAuthors(int skip, int take = AppConstants.DefaultPageSize,
            Expression<Func<Author, bool>> filter = null, bool showTotal = false)
        {
            if (_context != null)
            {
                if (skip < 0)
                    skip = 0;
                if (take > AppConstants.MaxPageSize || take <= 0)
                    take = AppConstants.DefaultPageSize;

                var table = _context.Authors.AsQueryable().AsNoTracking();

                if (filter != null)
                    table = table.Where(filter);

                var records = await table.OrderBy(a => a.FirstName).ThenBy(a => a.LastName).ThenBy(a => a.CreatedAt)
                    .Skip(skip).Take(take).ToListAsync();

                var authors = _mapper.Map<List<AuthorViewModel>>(records);

                if (showTotal)
                    return new { data = authors, total = await table.CountAsync() };
                else
                    return authors;
            }

            return null;
        }

        public async Task<dynamic> GetAuthors(int skip, int take = AppConstants.DefaultPageSize, string name = "", bool showTotal = false)
        {
            if (_context != null)
            {
                if (skip < 0)
                    skip = 0;
                if (take > AppConstants.MaxPageSize || take <= 0)
                    take = AppConstants.DefaultPageSize;

                JArray authors = new JArray();

                IQueryable<Author> table = _context.Authors.AsNoTracking();

                if (!string.IsNullOrWhiteSpace(name))
                {
                    table = table.Where(a => (a.FirstName + " " + a.LastName).Contains(name));
                }

                var records = await table.OrderBy(a => a.FirstName).ThenBy(a => a.LastName).ThenBy(a => a.CreatedAt)
                    .Skip(skip).Take(take).ToListAsync();

                foreach (var r in records)
                    authors.Add(JObject.FromObject(_mapper.Map<AuthorViewModel>(r)));

                if (showTotal)
                    return new { data = authors, total = await table.CountAsync() };
                else
                    return authors;
            }

            return null;
        }

        public async Task<AuthorViewModel> CreateAuthor(AuthorViewModel author)
        {
            if (_context != null)
            {
                var record = new Author();

                record.FirstName = author.FirstName;
                record.LastName = author.LastName;
                record.UpdatedAt = DateTime.Now;
                record.CreatedAt = DateTime.Now;

                var result = await _context.Authors.AddAsync(record);
                await _context.SaveChangesAsync();
                return _mapper.Map<AuthorViewModel>(result.Entity);
            }

            return null;
        }

        public async Task<AuthorViewModel> UpdateAuthor(long id, AuthorViewModel author)
        {
            if (_context != null && author != null && id > 0)
            {
                var found = await _context.Authors.FindAsync(id);
                if (found != null)
                {
                    try
                    {
                        var entry = _context.Entry(found);
                        entry.State = EntityState.Modified;

                        found.FirstName = author.FirstName;
                        found.LastName = author.LastName;
                        found.UpdatedAt = DateTime.Now;

                        await _context.SaveChangesAsync();

                        // index articles
                        await BulkIndexArticles(id);

                        return _mapper.Map<AuthorViewModel>(found);
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
            if (_context != null && id > 0)
            {
                var record = await _context.Authors.AsQueryable().AsNoTracking().SingleOrDefaultAsync(a => a.Id == id);
                if (record != null)
                    return _mapper.Map<AuthorViewModel>(record);
            }

            return null;
        }

        public async Task<dynamic> GetArticlesForAuthor(long id, int skip, int take, string title = "",
            bool loadRelation = false, bool showTotal = false)
        {
            if (_context != null && id > 0)
            {
                if (skip < 0)
                    skip = 0;
                if (take > AppConstants.MaxPageSize || take <= 0)
                    take = AppConstants.DefaultPageSize;

                JArray articles = new JArray();
                IQueryable<Authorship> table = _context.Authorships.AsNoTracking().Where(a => a.AuthorId == id);

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
            if (_context != null && id > 0)
            {
                var record = await _context.Authors.FindAsync(id);
                if (record == null)
                    return false;

                _context.Authors.Remove(record);
                await _context.SaveChangesAsync();
                return true;
            }

            return false;
        }

        async Task BulkIndexArticles(long id)
        {
            var records = await _context.Authorships.AsNoTracking()
                            .Where(a => a.AuthorId == id)
                            .Include(a => a.Article)
                            .ThenInclude(a => a.ArticlesCategories)
                            .ThenInclude(a => a.Category)
                            .Include(a => a.Article)
                            .ThenInclude(a => a.Comments)
                            .Include(a => a.Author)
                            .ToListAsync();
            IEnumerable<ArticleViewModel> articles = records.Select(r => _mapper.Map<ArticleViewModel>(r.Article));

            if (articles != null && articles.Count() > 0)
            {
                var lst = articles.Select(a => a.AsIndexedJson()).ToDictionary(a => a.Value<string>("id"), a => JsonConvert.SerializeObject(a));
                await _helper.BulkIndexDocument(lst);
                _logger.LogInformation($"Done: {articles.Count()}");
            }
        }
    }
}