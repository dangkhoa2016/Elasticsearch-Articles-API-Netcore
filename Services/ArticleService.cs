using elasticsearch_netcore.Helpers;
using elasticsearch_netcore.Repositories;
using elasticsearch_netcore.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace elasticsearch_netcore.Services
{
    public class ArticleService : IArticleService
    {
        private readonly IArticleRepository _articleRepository;
        private readonly IBackgroundWorkerQueue _worker;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<ArticleService> _logger;

        public ArticleService(
            IArticleRepository articleRepository,
            IBackgroundWorkerQueue worker,
            IServiceScopeFactory serviceScopeFactory,
            ILogger<ArticleService> logger)
        {
            _articleRepository = articleRepository;
            _worker = worker;
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
        }

        public async Task<dynamic> GetArticlesAsync(int skip, int take, string title, bool loadRelation, bool showTotal)
        {
            return await _articleRepository.GetArticles(skip, take, title, loadRelation, showTotal);
        }

        public async Task<string> GetArticleJsonAsync(long id)
        {
            var record = await _articleRepository.GetArticle(id, true);
            if (record == null)
                return null;

            return JsonConvert.SerializeObject(record.AsIndexedJson());
        }

        public async Task<object> GetArticleAsync(long id, bool loadRelation)
        {
            var record = await _articleRepository.GetArticle(id, loadRelation);
            if (record == null)
                return null;

            JObject article = ArticleRepository.ConvertToJObject(record, loadRelation, ForPage.Detail);
            return article;
        }

        public async Task<dynamic> GetCommentsForArticleAsync(long id, int skip, int take, bool showTotal)
        {
            return await _articleRepository.GetCommentsForArticle(id, skip, take, showTotal);
        }

        public async Task<ArticleViewModel> CreateArticleAsync(JsonElement article)
        {
            var record = ConvertToModel(article);
            record.Id = 0;
            record = await _articleRepository.CreateArticle(record);

            return record;
        }

        public async Task<ArticleViewModel> UpdateArticleAsync(long id, JsonElement article)
        {
            var record = ConvertToModel(article);
            record.Id = id;
            record = await _articleRepository.UpdateArticle(id, record);

            return record;
        }

        public async Task<bool> DeleteArticleAsync(long id)
        {
            return await _articleRepository.DeleteArticle(id);
        }

        public async Task<string> ImportAsync()
        {
            TimeSpan startAt = DateTime.UtcNow.TimeOfDay;
            _logger.LogInformation("Starting import at {StartAt}", startAt);

            await _worker.QueueBackgroundWorkItemAsync(async token =>
            {
                await RunBulkIndex(_serviceScopeFactory);
                _logger.LogInformation("Done import at {EndAt}", DateTime.UtcNow.TimeOfDay);
            });

            return $"Bulk import starting in the background... at {startAt}";
        }

        ArticleViewModel ConvertToModel(JsonElement article)
        {
            var record = JsonConvert.DeserializeObject<ArticleViewModel>(article.ToString());

            if (article.TryGetProperty("categories", out JsonElement propertyCategories) && propertyCategories.ValueKind == JsonValueKind.Array)
            {
                record.ArticlesCategories = JArray.Parse(propertyCategories.ToString()).Select(c =>
                {
                    long? categoryId = null;
                    try
                    {
                        if (c.GetType() == typeof(JValue))
                            categoryId = Convert.ToInt64(c);
                        else
                        {
                            categoryId = c.Value<long?>("category_id");
                            if (!categoryId.HasValue)
                                categoryId = c.Value<long?>("id");
                        }
                    }
                    catch { }

                    if (!categoryId.HasValue)
                        return null;
                    else
                        return new ArticlesCategoryViewModel() { CategoryId = categoryId };
                }).Where(c => c != null).ToList();
            }

            if (article.TryGetProperty("authors", out JsonElement propertyAuthors) && propertyAuthors.ValueKind == JsonValueKind.Array)
            {
                record.Authorships = JArray.Parse(propertyAuthors.ToString()).Select(a =>
                {
                    long? authorId = null;
                    try
                    {
                        if (a.GetType() == typeof(JValue))
                            authorId = Convert.ToInt64(a);
                        else
                        {
                            authorId = a.Value<long?>("author_id");
                            if (!authorId.HasValue)
                                authorId = a.Value<long?>("id");
                        }
                    }
                    catch { }

                    if (!authorId.HasValue)
                        return null;
                    else
                        return new AuthorshipViewModel() { AuthorId = authorId };
                }).Where(a => a != null).ToList();
            }

            return record;
        }

        static async Task RunBulkIndex(IServiceScopeFactory serviceScopeFactory)
        {
            if (serviceScopeFactory == null)
            {
                Console.WriteLine("IServiceScopeFactory not provided.");
                return;
            }

            using (var scope = serviceScopeFactory.CreateScope())
            {
                var services = scope.ServiceProvider;
                var articleRepository = services.GetRequiredService<IArticleRepository>();
                await articleRepository.BulkIndex();
            }
        }
    }
}
