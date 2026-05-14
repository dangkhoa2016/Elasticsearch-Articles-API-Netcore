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
    public class CommentController : ControllerBase
    {
        private readonly ICommentService _commentService;
        private readonly ILogger<CommentController> _logger;

        public CommentController(ICommentService commentService, ILogger<CommentController> logger)
        {
            _commentService = commentService;
            _logger = logger;
        }

        [HttpGet]
        [Route("comments")]
        public async Task<IActionResult> GetComments(int skip = 0, int take = 10,
            bool loadRelation = false, bool showTotal = false)
        {
            try
            {
                var records = await _commentService.GetCommentsAsync(skip, take, loadRelation, showTotal);
                if (records == null)
                    return NotFound();

                return Content(JsonConvert.SerializeObject(records), MediaTypeNames.Application.Json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetComments: skip={Skip}, take={Take}", skip, take);
                return StatusCode(500, new { error = "InternalServerError", message = "Failed to retrieve comments. Please try again later." });
            }
        }

        // GET: api/comments/5
        [HttpGet("comments/{id}")]
        public async Task<ActionResult> GetComment(long id, bool loadRelation = false)
        {
            try
            {
                var json = await _commentService.GetCommentAsync(id, loadRelation);
                if (json == null)
                    return NotFound();

                return Content(json, MediaTypeNames.Application.Json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetComment: id={Id}", id);
                return StatusCode(500, new { error = "InternalServerError", message = "Failed to retrieve comment. Please try again later." });
            }
        }

        // delete: api/comments/5, get comments/5/delete
        [HttpGet("comments/{id}/delete"), HttpDelete("comments/{id}")]
        public async Task<ActionResult<bool>> DeleteComment(long id)
        {
            try
            {
                var result = await _commentService.DeleteCommentAsync(id);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DeleteComment: id={Id}", id);
                return StatusCode(500, new { error = "InternalServerError", message = "Failed to delete comment. Please try again later." });
            }
        }

        [HttpPut, HttpPatch]
        [Route("comments/{id}")]
        public async Task<ActionResult<CommentViewModel>> UpdateComment(CommentViewModel comment)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(comment.Body))
                    return BadRequest(new { error = "ValidationError", message = "body is required." });
                if (!comment.ArticleId.HasValue)
                    return BadRequest(new { error = "ValidationError", message = "article_id is required." });

                long commentId = 0;
                long.TryParse(HttpContext.Request.RouteValues["id"].ToString(), out commentId);

                var result = await _commentService.UpdateCommentAsync(commentId, comment);
                if (result == null)
                    return NotFound();

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UpdateComment");
                return StatusCode(500, new { error = "InternalServerError", message = "Failed to update comment. Please try again later." });
            }
        }

        [HttpPost]
        [Route("comments")]
        public async Task<ActionResult<CommentViewModel>> CreateComment(CommentViewModel comment)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(comment.Body))
                    return BadRequest(new { error = "ValidationError", message = "body is required." });
                if (!comment.ArticleId.HasValue)
                    return BadRequest(new { error = "ValidationError", message = "article_id is required." });

                var result = await _commentService.CreateCommentAsync(comment);

                return CreatedAtAction("GetComment", new { id = result.Id }, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CreateComment");
                return StatusCode(500, new { error = "InternalServerError", message = "Failed to create comment. Please try again later." });
            }
        }
    }
}
