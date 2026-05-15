using elasticsearch_netcore.Constants;
using elasticsearch_netcore.Repositories;
using elasticsearch_netcore.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Net.Mime;
using System.Threading.Tasks;

namespace elasticsearch_netcore.Controllers
{
    [Route("api/")]
    [ApiController]
    [Authorize]
    public class AuthorshipController : ControllerBase
    {
        private readonly IAuthorshipRepository _authorshipRepository;
        private readonly ILogger<AuthorshipController> _logger;

        public AuthorshipController(IAuthorshipRepository authorshipRepository, ILogger<AuthorshipController> logger)
        {
            _authorshipRepository = authorshipRepository;
            _logger = logger;
        }

        [HttpGet]
        [Route("authorships")]
        public async Task<IActionResult> GetAuthorships(int skip = 0, int take = AppConstants.DefaultPageSize, bool loadRelation = false, bool showTotal = false)
        {
            try
            {
                var records = await _authorshipRepository.GetAuthorships(skip, take, loadRelation, null, showTotal);
                if (records == null)
                    return NotFound();

                return Content(JsonConvert.SerializeObject(records), MediaTypeNames.Application.Json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetAuthorships");
                return StatusCode(AppConstants.HttpStatusCodeInternalServerError, new { error = "InternalServerError", message = "Failed to retrieve authorships. Please try again later." });
            }
        }

        [HttpGet("authorships/{id}")]
        public async Task<ActionResult> GetAuthorship(long id, bool loadRelation = false)
        {
            try
            {
                var record = await _authorshipRepository.GetAuthorship(id, loadRelation);

                if (record == null)
                    return NotFound();

                return Content(AuthorshipRepository.ConvertToJObject(record, loadRelation).ToString(Formatting.None), MediaTypeNames.Application.Json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetAuthorship: id={Id}", id);
                return StatusCode(AppConstants.HttpStatusCodeInternalServerError, new { error = "InternalServerError", message = "Failed to retrieve authorship. Please try again later." });
            }
        }

        [HttpGet("authorships/{id}/delete"), HttpDelete("authorships/{id}")]
        public async Task<ActionResult<bool>> DeleteAuthorship(long id)
        {
            try
            {
                return await _authorshipRepository.DeleteAuthorship(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DeleteAuthorship: id={Id}", id);
                return StatusCode(AppConstants.HttpStatusCodeInternalServerError, new { error = "InternalServerError", message = "Failed to delete authorship. Please try again later." });
            }
        }

        [HttpPut, HttpPatch]
        [Route("authorships/{id}")]
        public async Task<ActionResult> UpdateAuthorship(AuthorshipViewModel authorship)
        {
            try
            {
                if (!authorship.ArticleId.HasValue)
                    return BadRequest(new { error = "ValidationError", message = "article_id is required." });
                if (!authorship.AuthorId.HasValue)
                    return BadRequest(new { error = "ValidationError", message = "author_id is required." });

                long authorshipId = 0;
                long.TryParse(HttpContext.Request.RouteValues["id"].ToString(), out authorshipId);
                authorship = await _authorshipRepository.UpdateAuthorship(authorshipId, authorship);

                if (authorship == null)
                    return NotFound();

                return Content(AuthorshipRepository.ConvertToJObject(authorship, false).ToString(Formatting.None), MediaTypeNames.Application.Json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UpdateAuthorship");
                return StatusCode(AppConstants.HttpStatusCodeInternalServerError, new { error = "InternalServerError", message = "Failed to update authorship. Please try again later." });
            }
        }

        [HttpPost]
        [Route("authorships")]
        public async Task<ActionResult> CreateAuthorship(AuthorshipViewModel authorship)
        {
            try
            {
                if (!authorship.ArticleId.HasValue)
                    return BadRequest(new { error = "ValidationError", message = "article_id is required." });
                if (!authorship.AuthorId.HasValue)
                    return BadRequest(new { error = "ValidationError", message = "author_id is required." });

                authorship = await _authorshipRepository.CreateAuthorship(authorship);

                if (authorship == null)
                    return Conflict(new { error = "DuplicateError", message = "This author already exists for the specified article." });

                return Content(AuthorshipRepository.ConvertToJObject(authorship, false).ToString(Formatting.None), MediaTypeNames.Application.Json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CreateAuthorship");
                return StatusCode(AppConstants.HttpStatusCodeInternalServerError, new { error = "InternalServerError", message = "Failed to create authorship. Please try again later." });
            }
        }
    }
}