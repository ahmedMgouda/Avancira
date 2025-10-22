using System.Collections.Generic;
using Avancira.Application.Subjects;
using Avancira.Application.Subjects.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Avancira.API.Controllers;

[Route("api/subjects")]
public class SubjectsController : BaseApiController
{
    private readonly ISubjectService _subjectService;

    public SubjectsController(ISubjectService subjectService)
    {
        _subjectService = subjectService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<SubjectDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSubjects([FromQuery] int? categoryId)
    {
        var subjects = await _subjectService.GetAllAsync(categoryId);
        return Ok(subjects);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(SubjectDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSubjectById(int id)
    {
        var subject = await _subjectService.GetByIdAsync(id);
        return Ok(subject);
    }

    [HttpPost]
    [ProducesResponseType(typeof(SubjectDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateSubject([FromBody] SubjectCreateDto request)
    {
        var subject = await _subjectService.CreateAsync(request);
        return CreatedAtAction(nameof(GetSubjectById), new { id = subject.Id }, subject);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(SubjectDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateSubject(int id, [FromBody] SubjectUpdateDto request)
    {
        if (id != request.Id)
        {
            return BadRequest("Subject ID mismatch.");
        }

        var subject = await _subjectService.UpdateAsync(request);
        return Ok(subject);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteSubject(int id)
    {
        await _subjectService.DeleteAsync(id);
        return NoContent();
    }
}
