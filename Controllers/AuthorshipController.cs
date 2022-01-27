using elasticsearch_netcore.Repositories;
using elasticsearch_netcore.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Net.Mime;
using System;
using System.Threading.Tasks;

namespace elasticsearch_netcore.Controllers
{
    [Route("api/")]
    [ApiController]
    public class AuthorshipController : ControllerBase
    {
        private IAuthorshipRepository authorshipRepository;

        public AuthorshipController(IAuthorshipRepository authorshipRepository)
        {
            this.authorshipRepository = authorshipRepository;
        }

        [HttpGet]
        [Route("authorships")]
        public async Task<IActionResult> GetAuthorships(int skip = 0, int take = 10, bool loadRelation = false, bool showTotal = false)
        {
            try
            {
                var records = await authorshipRepository.GetAuthorships(skip, take, loadRelation, null, showTotal);
                if (records == null)
                    return NotFound();

                return Content(JsonConvert.SerializeObject(records), MediaTypeNames.Application.Json);
            }
            catch (Exception ex)
            {
                return BadRequest();
            }
        }

        // GET: api/authorships/5
        [HttpGet("authorships/{id}")]
        public async Task<ActionResult> GetAuthorship(long id, bool loadRelation = false)
        {
            try
            {
                var record = await authorshipRepository.GetAuthorship(id, loadRelation);

                if (record == null)
                    return NotFound();

                return Content(AuthorshipRepository.ConvertToJObject(record, loadRelation).ToString(Formatting.None), MediaTypeNames.Application.Json);
            }
            catch (Exception ex)
            {
                return BadRequest();
            }
        }

        // delete: api/authorships/5, get authorships/5/delete
        [HttpGet("authorships/{id}/delete"), HttpDelete("authorships/{id}")]
        public async Task<ActionResult<bool>> DeleteAuthorship(long id)
        {
            try
            {
                return await authorshipRepository.DeleteAuthorship(id);
            }
            catch (Exception ex)
            {
                return BadRequest();
            }
        }

        [HttpPut, HttpPatch]
        [Route("authorships/{id}")]
        public async Task<ActionResult> UpdateAuthorship(AuthorshipViewModel authorship)
        {
            try
            {
                long authorshipId = 0;
                long.TryParse(HttpContext.Request.RouteValues["id"].ToString(), out authorshipId);
                authorship = await authorshipRepository.UpdateAuthorship(authorshipId, authorship);

                if (authorship == null)
                    return NotFound();

                return Content(AuthorshipRepository.ConvertToJObject(authorship, false).ToString(Formatting.None), MediaTypeNames.Application.Json);
                //return authorship;
            }
            catch (Exception ex)
            {
                return BadRequest();
            }
        }

        [HttpPost]
        [Route("authorships")]
        public async Task<ActionResult> CreateAuthorship(AuthorshipViewModel authorship)
        {
            try
            {
                authorship = await authorshipRepository.CreateAuthorship(authorship);

                return Content(AuthorshipRepository.ConvertToJObject(authorship, false).ToString(Formatting.None), MediaTypeNames.Application.Json);
                //return CreatedAtAction("GetAuthorship", new { id = authorship.Id }, authorship);
            }
            catch (Exception ex)
            {
                return BadRequest();
            }
        }
    }
}