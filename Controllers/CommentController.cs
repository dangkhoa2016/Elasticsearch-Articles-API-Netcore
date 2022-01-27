using elasticsearch_netcore.Repositories;
using elasticsearch_netcore.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Net.Mime;
using System.Threading.Tasks;

namespace elasticsearch_netcore.Controllers
{
    [Route("api/")]
    [ApiController]
    public class CommentController : ControllerBase
    {
        private ICommentRepository commentRepository;

        public CommentController(ICommentRepository commentRepository)
        {
            this.commentRepository = commentRepository;
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
                return BadRequest();
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
                return BadRequest();
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
                return BadRequest();
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
                return BadRequest();
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
                return BadRequest();
            }
        }
    }
}