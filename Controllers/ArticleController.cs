using elasticsearch_netcore.Services;
using elasticsearch_netcore.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Net.Mime;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;

namespace elasticsearch_netcore.Controllers
{
    [Route("api/")]
    [ApiController]
    [Authorize]
    public class ArticleController : ControllerBase
    {
        private readonly IArticleService _articleService;
        private readonly ILogger _logger;

        public ArticleController(IArticleService articleService, ILogger<ArticleController> logger)
        {
            _articleService = articleService;
            _logger = logger;
        }

        [HttpGet]
        [Route("articles/{id}/as_indexed_json")]
        public async Task<IActionResult> GetArticleJson(long id)
        {
            var json = await _articleService.GetArticleJsonAsync(id);
            if (json == null)
                return NotFound();

            return Content(json, MediaTypeNames.Application.Json);
        }

        [HttpGet]
        [Route("articles")]
        public async Task<IActionResult> GetArticles(int skip = 0, int take = 10, string title = "",
            bool loadRelation = false, bool showTotal = false)
        {
            try
            {
                var records = await _articleService.GetArticlesAsync(skip, take, title, loadRelation, showTotal);
                if (records == null)
                    return NotFound();

                return Content(JsonConvert.SerializeObject(records), MediaTypeNames.Application.Json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetArticles: skip={Skip}, take={Take}, title={Title}", skip, take, title);
                return StatusCode(500, new { error = "InternalServerError", message = "Failed to retrieve articles. Please try again later." });
            }
        }

        // GET: api/articles/5
        [HttpGet("articles/{id}")]
        public async Task<IActionResult> GetArticle(long id, bool loadRelation = false)
        {
            try
            {
                var article = await _articleService.GetArticleAsync(id, loadRelation);
                if (article == null)
                    return NotFound();

                return Content(article.ToString(), MediaTypeNames.Application.Json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetArticle: id={Id}", id);
                return StatusCode(500, new { error = "InternalServerError", message = "Failed to retrieve article. Please try again later." });
            }
        }

        // GET: api/articles/5/comments
        [HttpGet("articles/{id}/comments")]
        public async Task<ActionResult> GetCommentsForArticle(long id, int skip, int take, bool showTotal = false)
        {
            var records = await _articleService.GetCommentsForArticleAsync(id, skip, take, showTotal);
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
                await _articleService.DeleteArticleAsync(id);
                return Content(JsonConvert.SerializeObject(new { msg = "Article with id:[" + id + "] has been deleted." }),
                    MediaTypeNames.Application.Json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DeleteArticle: id={Id}", id);
                return StatusCode(500, new { error = "InternalServerError", message = "Failed to delete article. Please try again later." });
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

                var record = await _articleService.UpdateArticleAsync(articleId, article);

                if (record == null)
                    return NotFound();

                return Ok(record);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation error in UpdateArticle: {Message}", ex.Message);
                return BadRequest(new { error = "ValidationError", message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UpdateArticle");
                return StatusCode(500, new { error = "InternalServerError", message = "Failed to update article. Please try again later." });
            }
        }

        [HttpPost]
        [Route("articles")]
        public async Task<ActionResult<ArticleViewModel>> CreateArticle([FromBody] JsonElement article)
        {
            try
            {
                var record = await _articleService.CreateArticleAsync(article);

                if (record == null)
                    return UnprocessableEntity();

                return CreatedAtAction("GetArticle", new { id = record.Id }, record);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation error in CreateArticle: {Message}", ex.Message);
                return BadRequest(new { error = "ValidationError", message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CreateArticle");
                return StatusCode(500, new { error = "InternalServerError", message = "Failed to create article. Please try again later." });
            }
        }

        [HttpPost]
        [Route("articles/import")]
        public async Task<IActionResult> Import()
        {
            try
            {
                var message = await _articleService.ImportAsync();
                return Content(JsonConvert.SerializeObject(new { msg = message }),
                  MediaTypeNames.Application.Json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Import");
                return StatusCode(500, new { error = "InternalServerError", message = "Failed to start bulk import. Please try again later." });
            }
        }
    }
}
