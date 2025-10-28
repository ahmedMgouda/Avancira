using Avancira.Application.Lessons;
using Avancira.Application.Lessons.Dtos;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Avancira.API.Controllers;

[Route("api/lessons")]
public class LessonsController : BaseApiController
{
    private readonly ILessonService _lessonService;

    public LessonsController(ILessonService lessonService)
    {
        _lessonService = lessonService;
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(LessonDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLesson(int id, CancellationToken cancellationToken)
    {
        var lesson = await _lessonService.GetByIdAsync(id, cancellationToken);
        return Ok(lesson);
    }

    [HttpPost]
    [ProducesResponseType(typeof(LessonDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateLesson([FromBody] LessonCreateDto request, CancellationToken cancellationToken)
    {
        var lesson = await _lessonService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetLesson), new { id = lesson.Id }, lesson);
    }

    [HttpPost("{id:int}/confirm")]
    [ProducesResponseType(typeof(LessonDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ConfirmLesson(int id, CancellationToken cancellationToken)
    {
        var lesson = await _lessonService.ConfirmAsync(id, cancellationToken);
        return Ok(lesson);
    }

    [HttpPost("decline")]
    [ProducesResponseType(typeof(LessonDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeclineLesson([FromBody] LessonDeclineDto request, CancellationToken cancellationToken)
    {
        var lesson = await _lessonService.DeclineAsync(request, cancellationToken);
        return Ok(lesson);
    }

    [HttpPost("{id:int}/start")]
    [ProducesResponseType(typeof(LessonDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> StartLesson(int id, CancellationToken cancellationToken)
    {
        var lesson = await _lessonService.StartAsync(id, cancellationToken);
        return Ok(lesson);
    }

    [HttpPost("complete")]
    [ProducesResponseType(typeof(LessonDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> CompleteLesson([FromBody] LessonCompleteDto request, CancellationToken cancellationToken)
    {
        var lesson = await _lessonService.CompleteAsync(request, cancellationToken);
        return Ok(lesson);
    }

    [HttpPost("cancel")]
    [ProducesResponseType(typeof(LessonDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> CancelLesson([FromBody] LessonCancelDto request, CancellationToken cancellationToken)
    {
        var lesson = await _lessonService.CancelAsync(request, cancellationToken);
        return Ok(lesson);
    }

    [HttpPost("reschedule")]
    [ProducesResponseType(typeof(LessonDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> RescheduleLesson([FromBody] LessonRescheduleDto request, CancellationToken cancellationToken)
    {
        var lesson = await _lessonService.RescheduleAsync(request, cancellationToken);
        return Ok(lesson);
    }
}
