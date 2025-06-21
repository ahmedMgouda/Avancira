using System.Threading.Tasks;
using Avancira.Application.Catalog;
using Avancira.Domain.Catalog;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Avancira.API.Controllers;

[Route("api/lesson/categories")]
public class LessonCategoriesController : BaseApiController
{
    private readonly ILessonCategoryService _categoryService;
    private readonly ILogger<LessonCategoriesController> _logger;

    public LessonCategoriesController(ILessonCategoryService categoryService, ILogger<LessonCategoriesController> logger)
    {
        _categoryService = categoryService;
        _logger = logger;
    }


    // Create
    [HttpPost]
    public IActionResult CreateCategory([FromBody] Category category)
    {
        if (category == null)
        {
            return BadRequest("Category data is required.");
        }

        var createdCategory = _categoryService.CreateCategory(category);
        return CreatedAtAction(nameof(GetCategories), new { id = createdCategory.Id }, createdCategory);
    }

    // Read
    [HttpGet]
    public async Task<IActionResult> GetCategories([FromQuery] string? query, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var categories = await _categoryService.SearchCategoriesAsync(query, page, pageSize);
        return Ok(categories);
    }

    // Update
    [HttpPut("{id}")]
    public IActionResult UpdateCategory(int id, [FromBody] Category updatedCategory)
    {
        var category = _categoryService.UpdateCategory(id, updatedCategory);
        if (category == null)
        {
            return NotFound();
        }

        return Ok(category);
    }

    // Delete
    [HttpDelete("{id}")]
    public IActionResult DeleteCategory(int id)
    {
        var result = _categoryService.DeleteCategory(id);
        if (!result)
        {
            return NotFound();
        }

        return NoContent();
    }
}
