using Avancira.Application.Lessons.Dtos;
using Avancira.Application.Lessons;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Avancira.API.Controllers;

[Route("api/lessons")]
public class LessonsController : BaseApiController
{
    private readonly ILessonService _lessonService;
    private readonly ILogger<LessonsController> _logger;

    public LessonsController(
        ILessonService lessonService,
        ILogger<LessonsController> logger
    )
    {
        _lessonService = lessonService;
        _logger = logger;
    }

    // Create
    [Authorize]
    [HttpPost("proposeLesson")]
    public async Task<IActionResult> ProposeLessonAsync([FromBody] LessonDto lessonDto)
    {
        var userId = GetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }
        var result = await _lessonService.ProposeLessonAsync(lessonDto, userId);
        return Ok(new { Message = "Lesson proposed successfully.", Lesson = result });
    }

    // Read
    [Authorize]
    [HttpGet("{contactId}/{listingId}")]
    public async Task<IActionResult> GetLessonsAsync(string contactId, Guid listingId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var userId = GetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }
        var lessons = await _lessonService.GetLessonsAsync(contactId, userId, listingId, page, pageSize);
        return Ok(new { Lessons = lessons });
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> GetAllLessonsAsync([FromQuery] LessonFilter filters)
    {
        if (filters.Page <= 0 || filters.PageSize <= 0)
        {
            return BadRequest("Invalid page or pageSize parameters.");
        }

        var userId = GetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }
        var lessons = await _lessonService.GetAllLessonsAsync(userId, filters);
        return Ok(new { Lessons = lessons });
    }
    
    // Update
    [Authorize]
    [HttpPut("respondToProposition/{lessonId}")]
    public async Task<IActionResult> RespondToPropositionAsync(Guid lessonId, [FromBody] bool accept)
    {
        var userId = GetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }
        LessonDto updatedLesson;
        try
        {
            updatedLesson = await _lessonService.UpdateLessonStatusAsync(lessonId, accept, userId);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { Message = "Lesson not found." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Message = "An error occurred while responding to the proposition.", Details = ex.Message });
        }

        return Ok(new
        {
            Message = accept ? "Proposition accepted." : "Proposition refused.",
            Lesson = updatedLesson
        });
    }

    // Delete
    [Authorize]
    [HttpDelete("{lessonId}/cancel")]
    public async Task<IActionResult> CancelLessonAsync(Guid lessonId)
    {
        try
        {
            var userId = GetUserId();
            if (userId is null)
            {
                return Unauthorized();
            }
            var canceledLesson = await _lessonService.UpdateLessonStatusAsync(lessonId, false, userId);

            return Ok(new
            {
                Message = "Lesson canceled successfully.",
                Lesson = new
                {
                    canceledLesson.Id,
                    canceledLesson.Status,
                    canceledLesson.Date,
                    canceledLesson.Duration
                }
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { Message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Message = "An error occurred while canceling the lesson.", Details = ex.Message });
        }
    }
}
