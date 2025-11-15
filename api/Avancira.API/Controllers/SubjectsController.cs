using Avancira.Application.Subjects;
using Avancira.Application.Subjects.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Avancira.API.Controllers;

[Route("api/subjects")]
[ApiController]
public class SubjectsController : ControllerBase
{
    private readonly ISubjectService _subjectService;
    private readonly ILogger<SubjectsController> _logger;

    public SubjectsController(
        ISubjectService subjectService,
        ILogger<SubjectsController> logger)
    {
        _subjectService = subjectService;
        _logger = logger;
    }

    /// <summary>
    /// Get all subjects, optionally filtered by category
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<SubjectDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSubjects(
        [FromQuery] int? categoryId,
        CancellationToken cancellationToken)
    {
        var subjects = await _subjectService.GetAllAsync(categoryId, cancellationToken);
        return Ok(subjects);
    }

    /// <summary>
    /// Get a subject by ID
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(SubjectDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSubject(int id, CancellationToken cancellationToken)
    {
        var subject = await _subjectService.GetByIdAsync(id, cancellationToken);
        return Ok(subject);
    }

    [HttpPost]
    [Consumes("multipart/form-data", "application/json")]
    [ProducesResponseType(typeof(SubjectDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateSubject(
        [FromForm] SubjectCreateDto request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var subject = await _subjectService.CreateAsync(request, cancellationToken);

        return CreatedAtAction(
            nameof(GetSubject),
            new { id = subject.Id },
            subject);
    }

    /// <summary>
    /// Update an existing subject with optional icon replacement
    /// </summary>
    [HttpPut("{id:int}")]
    [Consumes("multipart/form-data", "application/json")]
    [ProducesResponseType(typeof(SubjectDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateSubject(
        int id,
        [FromForm] SubjectUpdateDto request,
        CancellationToken cancellationToken)
    {
        if (id != request.Id)
            return BadRequest(new { message = "Subject ID mismatch between route and body" });

        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var subject = await _subjectService.UpdateAsync(request, cancellationToken);
        return Ok(subject);
    }

    /// <summary>
    /// Delete a subject and its associated icon
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteSubject(int id, CancellationToken cancellationToken)
    {
        await _subjectService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Upload or replace subject icon separately
    /// </summary>
    [HttpPost("{id:int}/icon")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UploadSubjectIcon(
        int id,
        IFormFile icon,
        CancellationToken cancellationToken)
    {
        if (icon == null || icon.Length == 0)
            return BadRequest(new { message = "Icon file is required" });

        // Fetch subject to ensure it exists
        var subject = await _subjectService.GetByIdAsync(id, cancellationToken);

        // Update with new icon
        var updateDto = new SubjectUpdateDto
        {
            Id = subject.Id,
            Name = subject.Name,
            Description = subject.Description,
            Icon = icon,
            IsActive = subject.IsActive,
            IsVisible = subject.IsVisible,
            IsFeatured = subject.IsFeatured,
            SortOrder = subject.SortOrder,
            CategoryId = subject.CategoryId
        };

        var updated = await _subjectService.UpdateAsync(updateDto, cancellationToken);

        return Ok(new
        {
            message = "Icon uploaded successfully",
            iconUrl = updated.IconUrl
        });
    }
}