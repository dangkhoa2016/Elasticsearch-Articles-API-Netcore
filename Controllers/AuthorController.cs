using elasticsearch_netcore.Constants;
using elasticsearch_netcore.Services;
using elasticsearch_netcore.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Net.Mime;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace elasticsearch_netcore.Controllers
{
    [Route("api/")]
    [ApiController]
    [Authorize]
    public class AuthorController : ControllerBase
    {
        private readonly IAuthorService _authorService;
        private readonly ILogger<AuthorController> _logger;

        public AuthorController(IAuthorService authorService, ILogger<AuthorController> logger)
        {
            _authorService = authorService;
            _logger = logger;
        }

        [HttpGet]
        [Route("authors")]
        public async Task<IActionResult> GetAuthors(int skip = 0, int take = AppConstants.DefaultPageSize,
            string name = "", bool showTotal = false)
        {
            try
            {
                var records = await _authorService.GetAuthorsAsync(skip, take, name, showTotal);
                if (records == null)
                    return NotFound();

                return Content(JsonConvert.SerializeObject(records), MediaTypeNames.Application.Json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetAuthors: skip={Skip}, take={Take}, name={Name}", skip, take, name);
                return StatusCode(AppConstants.HttpStatusCodeInternalServerError, new { error = "InternalServerError", message = "Failed to retrieve authors. Please try again later." });
            }
        }

        // GET: api/authors/5
        [HttpGet("authors/{id}")]
        public async Task<ActionResult> GetAuthor(long id)
        {
            try
            {
                var record = await _authorService.GetAuthorAsync(id);
                if (record == null)
                    return NotFound();

                return Ok(record);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetAuthor: id={Id}", id);
                return StatusCode(AppConstants.HttpStatusCodeInternalServerError, new { error = "InternalServerError", message = "Failed to retrieve author. Please try again later." });
            }
        }

        // GET: api/authors/5/articles
        [HttpGet("authors/{id}/articles")]
        public async Task<ActionResult> GetArticlesForAuthor(long id, int skip, int take, bool loadRelation,
            string title = "", bool showTotal = false)
        {
            var records = await _authorService.GetArticlesForAuthorAsync(id, skip, take, title, loadRelation, showTotal);
            if (records == null)
                return NotFound();

            return Content(JsonConvert.SerializeObject(records), MediaTypeNames.Application.Json);
        }

        // delete: api/authors/5, get authors/5/delete
        [HttpGet("authors/{id}/delete"), HttpDelete("authors/{id}")]
        public async Task<ActionResult<bool>> DeleteAuthor(long id)
        {
            try
            {
                var result = await _authorService.DeleteAuthorAsync(id);
                if (!result)
                    return NotFound();
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DeleteAuthor: id={Id}", id);
                return StatusCode(AppConstants.HttpStatusCodeInternalServerError, new { error = "InternalServerError", message = "Failed to delete author. Please try again later." });
            }
        }

        [HttpPut, HttpPatch]
        [Route("authors/{id}")]
        public async Task<ActionResult<AuthorViewModel>> UpdateAuthor(AuthorViewModel author)
        {
            try
            {
                long authorId = 0;
                long.TryParse(HttpContext.Request.RouteValues["id"].ToString(), out authorId);

                var result = await _authorService.UpdateAuthorAsync(authorId, author);
                if (result == null)
                {
                    if (!IsValid(author))
                        return BadRequest("Please provide first name or last name.");
                    return NotFound();
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UpdateAuthor");
                return StatusCode(AppConstants.HttpStatusCodeInternalServerError, new { error = "InternalServerError", message = "Failed to update author. Please try again later." });
            }
        }

        [HttpPost]
        [Route("authors")]
        public async Task<ActionResult<AuthorViewModel>> CreateAuthor(AuthorViewModel author)
        {
            if (!IsValid(author))
                return BadRequest("Please provide first name or last name.");

            try
            {
                var result = await _authorService.CreateAuthorAsync(author);

                return CreatedAtAction("GetAuthor", new { id = result.Id }, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CreateAuthor");
                return StatusCode(AppConstants.HttpStatusCodeInternalServerError, new { error = "InternalServerError", message = "Failed to create author. Please try again later." });
            }
        }

        bool IsValid(AuthorViewModel author)
        {
            return !string.IsNullOrWhiteSpace(author.FirstName) || !string.IsNullOrWhiteSpace(author.LastName);
        }
    }
}
