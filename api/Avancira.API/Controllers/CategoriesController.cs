using Avancira.Application.Catalog.Categories.Dtos;
using Avancira.Application.Services.Category;
using Avancira.Infrastructure.Auth.Policy;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Avancira.API.Controllers;

[Route("api/categories")]
public class CategoriesController : BaseApiController
{
    private readonly ICategoryService _categoryService;

    public CategoriesController(ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    [HttpGet]
    [RequiredPermission("Permissions.Categories.View")]
    [ProducesResponseType(typeof(IEnumerable<CategoryDto>), StatusCodes.Status200OK)]
    [SwaggerOperation(OperationId = "GetCategories")]
    public async Task<IActionResult> GetCategories()
    {
        var categories = await _categoryService.GetAllAsync();
        return Ok(categories);
    }

    [HttpGet("{id:guid}")]
    [RequiredPermission("Permissions.Categories.View")]
    [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status200OK)]
    [SwaggerOperation(OperationId = "GetCategoryById")]
    public async Task<IActionResult> GetCategoryById(Guid id)
    {
        var category = await _categoryService.GetByIdAsync(id);
        return Ok(category);
    }

    [HttpPost]
    [RequiredPermission("Permissions.Categories.Create")]
    [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status201Created)]
    [SwaggerOperation(OperationId = "CreateCategory")]
    public async Task<IActionResult> CreateCategory([FromBody] CategoryCreateDto request)
    {
        var result = await _categoryService.CreateAsync(request);
        return CreatedAtAction(nameof(GetCategoryById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [RequiredPermission("Permissions.Categories.Update")]
    [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status200OK)]
    [SwaggerOperation(OperationId = "UpdateCategory")]
    public async Task<IActionResult> UpdateCategory(Guid id, [FromBody] CategoryUpdateDto request)
    {
        if (id != request.Id)
        {
            return BadRequest("Category ID mismatch.");
        }

        var result = await _categoryService.UpdateAsync(request);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [RequiredPermission("Permissions.Categories.Delete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [SwaggerOperation(OperationId = "DeleteCategory")]
    public async Task<IActionResult> DeleteCategory(Guid id)
    {
        await _categoryService.DeleteAsync(id);
        return NoContent();
    }
}
