using System.Threading.Tasks;
using Avancira.Domain.Catalog;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Backend.Controllers;

[Route("api/lesson/categories")]
[ApiController]
public class LessonCategoriesAPIController : BaseController
{
    private readonly ILessonCategoryService _categoryService;
    private readonly ILogger<LessonCategoriesAPIController> _logger;

    public LessonCategoriesAPIController(ILessonCategoryService categoryService, ILogger<LessonCategoriesAPIController> logger)
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
            return JsonError();
        }

        var createdCategory = _categoryService.CreateCategory(category);
        return CreatedAtAction(nameof(GetCategories), new { id = createdCategory.Id }, createdCategory);
    }

    // Read
    [HttpGet]
    public async Task<IActionResult> GetCategories([FromQuery] string? query, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var categories = await _categoryService.SearchCategoriesAsync(query, page, pageSize);
        return JsonOk(categories);
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

        return JsonOk(category);
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


