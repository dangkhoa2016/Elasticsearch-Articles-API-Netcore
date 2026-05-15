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
    public class CategoryController : ControllerBase
    {
        private readonly ICategoryService _categoryService;
        private readonly ILogger<CategoryController> _logger;

        public CategoryController(ICategoryService categoryService, ILogger<CategoryController> logger)
        {
            _categoryService = categoryService;
            _logger = logger;
        }

        [HttpGet]
        [Route("categories")]
        public async Task<IActionResult> GetCategories(int skip = 0, int take = AppConstants.DefaultPageSize,
            string title = "", bool showTotal = false)
        {
            try
            {
                var records = await _categoryService.GetCategoriesAsync(skip, take, title, showTotal);
                if (records == null)
                    return NotFound();

                return Content(JsonConvert.SerializeObject(records), MediaTypeNames.Application.Json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetCategories: skip={Skip}, take={Take}, title={Title}", skip, take, title);
                return StatusCode(AppConstants.HttpStatusCodeInternalServerError, new { error = "InternalServerError", message = "Failed to retrieve categories. Please try again later." });
            }
        }

        // GET: api/categories/5/articles
        [HttpGet("categories/{id}/articles")]
        public async Task<ActionResult> GetArticlesForCategory(long id, int skip, int take, bool loadRelation,
            string title = "", bool showTotal = false)
        {
            var records = await _categoryService.GetArticlesForCategoryAsync(id, skip, take, title, loadRelation, showTotal);
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
                var record = await _categoryService.GetCategoryAsync(id);
                if (record == null)
                    return NotFound();

                return Ok(record);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetCategory: id={Id}", id);
                return StatusCode(AppConstants.HttpStatusCodeInternalServerError, new { error = "InternalServerError", message = "Failed to retrieve category. Please try again later." });
            }
        }

        // delete: api/categories/5, get categories/5/delete
        [HttpGet("categories/{id}/delete"), HttpDelete("categories/{id}")]
        public async Task<ActionResult<bool>> DeleteCategory(long id)
        {
            try
            {
                var result = await _categoryService.DeleteCategoryAsync(id);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DeleteCategory: id={Id}", id);
                return StatusCode(AppConstants.HttpStatusCodeInternalServerError, new { error = "InternalServerError", message = "Failed to delete category. Please try again later." });
            }
        }

        [HttpPut, HttpPatch]
        [Route("categories/{id}")]
        public async Task<ActionResult<CategoryViewModel>> UpdateCategory(CategoryViewModel category)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(category.Title))
                    return BadRequest(new { error = "ValidationError", message = "title is required." });

                long categoryId = 0;
                long.TryParse(HttpContext.Request.RouteValues["id"].ToString(), out categoryId);

                var result = await _categoryService.UpdateCategoryAsync(categoryId, category);
                if (result == null)
                    return NotFound();

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UpdateCategory");
                return StatusCode(AppConstants.HttpStatusCodeInternalServerError, new { error = "InternalServerError", message = "Failed to update category. Please try again later." });
            }
        }

        [HttpPost]
        [Route("categories")]
        public async Task<ActionResult<CategoryViewModel>> CreateCategory(CategoryViewModel category)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(category.Title))
                    return BadRequest(new { error = "ValidationError", message = "title is required." });

                var result = await _categoryService.CreateCategoryAsync(category);

                return CreatedAtAction("GetCategory", new { id = result.Id }, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CreateCategory");
                return StatusCode(AppConstants.HttpStatusCodeInternalServerError, new { error = "InternalServerError", message = "Failed to create category. Please try again later." });
            }
        }
    }
}
