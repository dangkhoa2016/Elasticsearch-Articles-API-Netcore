using elasticsearch_netcore.Repositories;
using elasticsearch_netcore.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Mime;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace elasticsearch_netcore.Controllers
{
    [Route("api/")]
    [ApiController]
    [Authorize]
    public class CategoryController : ControllerBase
    {
        private ICategoryRepository categoryRepository;
        private readonly ILogger<CategoryController> _logger;

        public CategoryController(ICategoryRepository categoryRepository, ILogger<CategoryController> logger)
        {
            this.categoryRepository = categoryRepository;
            _logger = logger;
        }

        [HttpGet]
        [Route("categories")]
        public async Task<IActionResult> GetCategories(int skip = 0, int take = 10,
            string title = "", bool showTotal = false)
        {
            try
            {
                var records = await categoryRepository.GetCategories(skip, take, title, showTotal);
                if (records == null)
                    return NotFound();

                return Content(JsonConvert.SerializeObject(records), MediaTypeNames.Application.Json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetCategories: skip={Skip}, take={Take}, title={Title}", skip, take, title);
                return StatusCode(500, new { error = "InternalServerError", message = "Failed to retrieve categories. Please try again later." });
            }
        }

        // GET: api/categories/5/articles
        [HttpGet("categories/{id}/articles")]
        public async Task<ActionResult> GetArticlesForCategory(long id, int skip, int take, bool loadRelation,
            string title = "", bool showTotal = false)
        {
            var records = await categoryRepository.GetArticlesForCategory(id, skip, take, title, loadRelation, showTotal);
            if (records == null)
                return NotFound();

            return Content(JsonConvert.SerializeObject(records), MediaTypeNames.Application.Json);
        }

        // GET: api/categories/5
        [HttpGet("categories/{id}")]
        public async Task<ActionResult> GetCategory(long id)
        {
            try
            {
                var record = await categoryRepository.GetCategory(id);
                if (record == null)
                    return NotFound();

                return Ok(record);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetCategory: id={Id}", id);
                return StatusCode(500, new { error = "InternalServerError", message = "Failed to retrieve category. Please try again later." });
            }
        }

        // delete: api/categories/5, get categories/5/delete
        [HttpGet("categories/{id}/delete"), HttpDelete("categories/{id}")]
        public async Task<ActionResult<bool>> DeleteCategory(long id)
        {
            try
            {
                return await categoryRepository.DeleteCategory(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DeleteCategory: id={Id}", id);
                return StatusCode(500, new { error = "InternalServerError", message = "Failed to delete category. Please try again later." });
            }
        }

        [HttpPut, HttpPatch]
        [Route("categories/{id}")]
        public async Task<ActionResult<CategoryViewModel>> UpdateCategory(CategoryViewModel category)
        {
            try
            {
                long categoryId = 0;
                long.TryParse(HttpContext.Request.RouteValues["id"].ToString(), out categoryId);
                category = await categoryRepository.UpdateCategory(categoryId, category);

                if (category == null)
                    return NotFound();

                return category;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UpdateCategory");
                return StatusCode(500, new { error = "InternalServerError", message = "Failed to update category. Please try again later." });
            }
        }

        [HttpPost]
        [Route("categories")]
        public async Task<ActionResult<CategoryViewModel>> CreateCategory(CategoryViewModel category)
        {
            try
            {
                category = await categoryRepository.CreateCategory(category);

                return CreatedAtAction("GetCategory", new { id = category.Id }, category);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CreateCategory");
                return StatusCode(500, new { error = "InternalServerError", message = "Failed to create category. Please try again later." });
            }
        }
    }
}