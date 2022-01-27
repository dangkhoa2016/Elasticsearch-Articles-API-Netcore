using elasticsearch_netcore.Repositories;
using elasticsearch_netcore.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Net.Mime;
using System.Threading.Tasks;

namespace elasticsearch_netcore.Controllers
{
    [Route("api/")]
    [ApiController]
    public class AuthorController : ControllerBase
    {
        private IAuthorRepository authorRepository;

        public AuthorController(IAuthorRepository authorRepository)
        {
            this.authorRepository = authorRepository;
        }

        [HttpGet]
        [Route("authors")]
        public async Task<IActionResult> GetAuthors(int skip = 0, int take = 10,
            string name = "", bool showTotal = false)
        {
            try
            {
                /*
                var records = await authorRepository.GetAuthors(skip, take,
                    //a => (a.FirstName + " " + a.LastName).Contains(name) -> match case sensitive
                    a => (a.FirstName + " " + a.LastName).ToLower().Contains(name.ToLower())
                    );
                */
                var records = await authorRepository.GetAuthors(skip, take, name, showTotal);
                if (records == null)
                    return NotFound();

                return Content(JsonConvert.SerializeObject(records), MediaTypeNames.Application.Json);
            }
            catch (Exception ex)
            {
                return BadRequest();
            }
        }

        // GET: api/authors/5
        [HttpGet("authors/{id}")]
        public async Task<ActionResult> GetAuthor(long id)
        {
            try
            {
                var record = await authorRepository.GetAuthor(id);
                if (record == null)
                    return NotFound();

                return Ok(record);
            }
            catch (Exception ex)
            {
                return BadRequest();
            }
        }

        // GET: api/authors/5/articles
        [HttpGet("authors/{id}/articles")]
        public async Task<ActionResult> GetArticlesForAuthor(long id, int skip, int take, bool loadRelation,
            string title = "", bool showTotal = false)
        {
            var records = await authorRepository.GetArticlesForAuthor(id, skip, take, title, loadRelation, showTotal);
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
                return await authorRepository.DeleteAuthor(id);
            }
            catch (Exception ex)
            {
                return BadRequest();
            }
        }

        [HttpPut, HttpPatch]
        [Route("authors/{id}")]
        public async Task<ActionResult<AuthorViewModel>> UpdateAuthor(AuthorViewModel author)
        {
            if (!IsValid(author))
                return BadRequest("Please provide first name or last name.");

            try
            {
                long authorId = 0;
                long.TryParse(HttpContext.Request.RouteValues["id"].ToString(), out authorId);
                author = await authorRepository.UpdateAuthor(authorId, author);

                if (author == null)
                    return NotFound();

                return author;
            }
            catch (Exception ex)
            {
                return BadRequest();
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
                author = await authorRepository.CreateAuthor(author);

                return CreatedAtAction("GetAuthor", new { id = author.Id }, author);
            }
            catch (Exception ex)
            {
                return BadRequest();
            }
        }

        bool IsValid(AuthorViewModel author)
        {
            return string.IsNullOrWhiteSpace(author.FirstName) == false || string.IsNullOrWhiteSpace(author.LastName) == false;
        }
    }
}