using Avancira.Application.SubjectCategories;
using Avancira.Application.SubjectCategories.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Avancira.API.Controllers;

[Route("api/subject-categories")]
[ApiController]
public sealed class SubjectCategoriesController : ControllerBase
{
    private readonly ISubjectCategoryService _subjectCategoryService;

    public SubjectCategoriesController(ISubjectCategoryService subjectCategoryService)
    {
        _subjectCategoryService = subjectCategoryService;
    }

    [HttpGet]
    public async Task<IActionResult> GetSubjectCategories([FromQuery] SubjectCategoryFilter filter)
    {
        var result = await _subjectCategoryService.GetAllAsync(filter);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetSubjectCategoryById(int id)
    {
        var category = await _subjectCategoryService.GetByIdAsync(id);
        return Ok(category);
    }

    [HttpPost]
    public async Task<IActionResult> CreateSubjectCategory([FromBody] SubjectCategoryCreateDto request)
    {
        var category = await _subjectCategoryService.CreateAsync(request);
        return CreatedAtAction(nameof(GetSubjectCategoryById), new { id = category.Id }, category);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateSubjectCategory(int id, [FromBody] SubjectCategoryUpdateDto request)
    {
        if (id != request.Id)
            return BadRequest("Subject category ID mismatch.");

        var category = await _subjectCategoryService.UpdateAsync(request);
        return Ok(category);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteSubjectCategory(int id)
    {
        await _subjectCategoryService.DeleteAsync(id);
        return NoContent();
    }


    [HttpPut("reorder")]
    public async Task<IActionResult> ReorderCategories([FromBody] ReorderRequest request)
    {
        await _subjectCategoryService.ReorderAsync(request.CategoryIds);
        return NoContent();
    }

    [HttpPut("{id:int}/move")]
    public async Task<IActionResult> MoveCategory(int id, [FromBody] MoveRequest request)
    {
        await _subjectCategoryService.MoveToPositionAsync(id, request.TargetSortOrder);
        return NoContent();
    }

}

public class ReorderRequest
{
    public int[] CategoryIds { get; set; } = default!;
}

public class MoveRequest
{
    public int TargetSortOrder { get; set; }
}
