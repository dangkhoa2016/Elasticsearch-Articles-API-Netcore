using elasticsearch_netcore.Repositories;
using elasticsearch_netcore.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Net.Mime;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using elasticsearch_netcore.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace elasticsearch_netcore.Controllers
{
    [Route("api/")]
    [ApiController]
    public class ArticleController : ControllerBase
    {
        private IArticleRepository articleRepository;
        private readonly IBackgroundWorkerQueue _worker;
        private readonly ILogger _logger;

        public ArticleController(IArticleRepository articleRepository, ILogger<ArticleController> logger, IBackgroundWorkerQueue worker)
        {
            this.articleRepository = articleRepository;
            _logger = logger;
            _worker = worker;
        }

        [HttpGet]
        [Route("articles/{id}/as_indexed_json")]
        public async Task<IActionResult> GetArticleJson(long id)
        {
            var record = await articleRepository.GetArticle(id, true);
            if (record == null)
                return NotFound();

            return Content(JsonConvert.SerializeObject(record.AsIndexedJson()), MediaTypeNames.Application.Json);
        }

        [HttpGet]
        [Route("articles")]
        public async Task<IActionResult> GetArticles(int skip = 0, int take = 10, string title = "",
            bool loadRelation = false, bool showTotal = false)
        {
            try
            {
                var records = await articleRepository.GetArticles(skip, take, title, loadRelation, showTotal);
                if (records == null)
                    return NotFound();

                return Content(JsonConvert.SerializeObject(records), MediaTypeNames.Application.Json);
            }
            catch (Exception ex)
            {
                return BadRequest();
            }
        }

        // GET: api/articles/5
        [HttpGet("articles/{id}")]
        public async Task<IActionResult> GetArticle(long id, bool loadRelation = false)
        {
            try
            {
                var record = await articleRepository.GetArticle(id, loadRelation);
                if (record == null)
                    return NotFound();

                JObject article = ArticleRepository.ConvertToJObject(record, loadRelation, ForPage.Detail);

                return Content(article.ToString(Formatting.None), MediaTypeNames.Application.Json);
            }
            catch (Exception ex)
            {
                return BadRequest();
            }
        }

        // GET: api/articles/5/comments
        [HttpGet("articles/{id}/comments")]
        public async Task<ActionResult> GetCommentsForArticle(long id, int skip, int take, bool showTotal = false)
        {
            var records = await articleRepository.GetCommentsForArticle(id, skip, take, showTotal);
            if (records == null)
                return NotFound();

            return Content(JsonConvert.SerializeObject(records), MediaTypeNames.Application.Json);
        }

        // delete: api/articles/5, get articles/5/delete
        [HttpGet("articles/{id}/delete"), HttpDelete("articles/{id}")]
        public async Task<ActionResult<bool>> DeleteArticle(long id)
        {
            try
            {
                await articleRepository.DeleteArticle(id);
                return Content(JsonConvert.SerializeObject(new { msg = "Article with id:[" + id + "] has been deleted." }),
                    MediaTypeNames.Application.Json);
            }
            catch (Exception ex)
            {
                return BadRequest();
            }
        }

        [HttpPut, HttpPatch]
        [Route("articles/{id}")]
        public async Task<ActionResult<ArticleViewModel>> UpdateArticle([FromBody] JsonElement article)
        {
            try
            {
                long articleId = 0;
                long.TryParse(HttpContext.Request.RouteValues["id"].ToString(), out articleId);

                if (articleId == 0)
                    return UnprocessableEntity();

                var record = ConvertToModel(article);
                record.Id = articleId;
                record = await articleRepository.UpdateArticle(articleId, record);

                if (record == null)
                    return NotFound();

                return record;
            }
            catch (Exception ex)
            {
                return BadRequest();
            }
        }

        [HttpPost]
        [Route("articles")]
        public async Task<ActionResult<ArticleViewModel>> CreateArticle([FromBody] JsonElement article)
        {
            try
            {
                var record = ConvertToModel(article);
                record.Id = 0;
                record = await articleRepository.CreateArticle(record);

                if (record == null)
                    return UnprocessableEntity();

                return CreatedAtAction("GetArticle", new { id = record.Id }, record);
            }
            catch (Exception ex)
            {
                return BadRequest();
            }
        }


        static void RunBulkIndex(IServiceScopeFactory serviceScopeFactory)
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
                articleRepository.BulkIndex().Wait();
            }
        }

        // demo only
        [HttpPost]
        [Route("articles/import")]
        public async Task<IActionResult> Import()
        {
            try
            {
                TimeSpan startAt = DateTime.UtcNow.TimeOfDay;
                _logger.LogInformation($"Starting import at {startAt}");
                await _worker.QueueBackgroundWorkItemAsync(async token =>
                {
                    RunBulkIndex((IServiceScopeFactory)HttpContext.RequestServices.GetService(typeof(IServiceScopeFactory)));
                    _logger.LogInformation($"Done import at {DateTime.UtcNow.TimeOfDay}");
                });

                return Content(JsonConvert.SerializeObject(new { msg = $"Bulk import starting in the background... at {startAt}" }),
                  MediaTypeNames.Application.Json);
            }
            catch (Exception ex)
            {
                return BadRequest();
            }
        }

        ArticleViewModel ConvertToModel(JsonElement article)
        {
            var record = JsonConvert.DeserializeObject<ArticleViewModel>(article.ToString());

            JsonElement propertyCategories = article.GetProperty("categories");
            if (propertyCategories.ValueKind == JsonValueKind.Array)
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
                    catch (Exception ex) { }

                    if (!categoryId.HasValue)
                        return null;
                    else
                        return new ArticlesCategoryViewModel() { CategoryId = categoryId };
                }).Where(c => c != null).ToList();
            }

            var propertyAuthors = article.GetProperty("authors");
            if (propertyAuthors.ValueKind == JsonValueKind.Array)
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
                            authorId = a.Value<long?>("category_id");
                            if (!authorId.HasValue)
                                authorId = a.Value<long?>("id");
                        }
                    }
                    catch (Exception ex) { }

                    if (!authorId.HasValue)
                        return null;
                    else
                        return new AuthorshipViewModel() { AuthorId = authorId };
                }).Where(a => a != null).ToList();
            }

            return record;
        }

    }
}