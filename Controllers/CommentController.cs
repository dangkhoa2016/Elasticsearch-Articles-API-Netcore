using elasticsearch_netcore.Repositories;
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
        private ICommentRepository commentRepository;
        private readonly ILogger<CommentController> _logger;

        public CommentController(ICommentRepository commentRepository, ILogger<CommentController> logger)
        {
            this.commentRepository = commentRepository;
            _logger = logger;
        }

        [HttpGet]
        [Route("comments")]
        public async Task<IActionResult> GetComments(int skip = 0, int take = 10,
            bool loadRelation = false, bool showTotal = false)
        {
            try
            {
                var records = await commentRepository.GetComments(skip, take, loadRelation, null, showTotal);
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
                var record = await commentRepository.GetComment(id, loadRelation);
                if (record == null)
                    return NotFound();

                return Content(CommentRepository.ConvertToJObject(record, loadRelation).ToString(Formatting.None), MediaTypeNames.Application.Json);
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
                return await commentRepository.DeleteComment(id);
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
                long commentId = 0;
                long.TryParse(HttpContext.Request.RouteValues["id"].ToString(), out commentId);
                comment = await commentRepository.UpdateComment(commentId, comment);

                if (comment == null)
                    return NotFound();

                return comment;
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
                comment = await commentRepository.CreateComment(comment);

                return CreatedAtAction("GetComment", new { id = comment.Id }, comment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CreateComment");
                return StatusCode(500, new { error = "InternalServerError", message = "Failed to create comment. Please try again later." });
            }
        }
    }
}