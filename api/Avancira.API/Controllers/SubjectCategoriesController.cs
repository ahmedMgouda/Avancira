using System.Collections.Generic;
using Avancira.Application.SubjectCategories;
using Avancira.Application.SubjectCategories.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Avancira.API.Controllers;

[Route("api/subject-categories")]
public class SubjectCategoriesController : BaseApiController
{
    private readonly ISubjectCategoryService _subjectCategoryService;

    public SubjectCategoriesController(ISubjectCategoryService subjectCategoryService)
    {
        _subjectCategoryService = subjectCategoryService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<SubjectCategoryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSubjectCategories()
    {
        var categories = await _subjectCategoryService.GetAllAsync();
        return Ok(categories);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(SubjectCategoryDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSubjectCategoryById(int id)
    {
        var category = await _subjectCategoryService.GetByIdAsync(id);
        return Ok(category);
    }

    [HttpPost]
    [ProducesResponseType(typeof(SubjectCategoryDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateSubjectCategory([FromBody] SubjectCategoryCreateDto request)
    {
        var category = await _subjectCategoryService.CreateAsync(request);
        return CreatedAtAction(nameof(GetSubjectCategoryById), new { id = category.Id }, category);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(SubjectCategoryDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateSubjectCategory(int id, [FromBody] SubjectCategoryUpdateDto request)
    {
        if (id != request.Id)
        {
            return BadRequest("Subject category ID mismatch.");
        }

        var category = await _subjectCategoryService.UpdateAsync(request);
        return Ok(category);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteSubjectCategory(int id)
    {
        await _subjectCategoryService.DeleteAsync(id);
        return NoContent();
    }
}
