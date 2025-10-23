using System.Collections.Generic;
using System.Threading;
using Avancira.Application.TutorSubjects;
using Avancira.Application.TutorSubjects.Dtos;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Avancira.API.Controllers;

[Route("api/tutors/subjects")]
public class TutorSubjectsController : BaseApiController
{
    private readonly ITutorSubjectService _tutorSubjectService;

    public TutorSubjectsController(ITutorSubjectService tutorSubjectService)
    {
        _tutorSubjectService = tutorSubjectService;
    }

    [HttpGet("{tutorId}")]
    [ProducesResponseType(typeof(IReadOnlyCollection<TutorSubjectDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTutorSubjects(string tutorId, CancellationToken cancellationToken)
    {
        var subjects = await _tutorSubjectService.GetByTutorIdAsync(tutorId, cancellationToken);
        return Ok(subjects);
    }

    [HttpPost]
    [ProducesResponseType(typeof(TutorSubjectDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateTutorSubject([FromBody] TutorSubjectCreateDto request, CancellationToken cancellationToken)
    {
        var subject = await _tutorSubjectService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetTutorSubjects), new { tutorId = subject.TutorId }, subject);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(TutorSubjectDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateTutorSubject(int id, [FromBody] TutorSubjectUpdateDto request, CancellationToken cancellationToken)
    {
        if (id != request.Id)
        {
            return BadRequest("Tutor subject ID mismatch.");
        }

        var subject = await _tutorSubjectService.UpdateAsync(request, cancellationToken);
        return Ok(subject);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteTutorSubject(int id, CancellationToken cancellationToken)
    {
        await _tutorSubjectService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
