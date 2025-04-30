using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avancira.Application.Catalog.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Backend.Controllers;

[Route("api/lessons")]
[ApiController]
public class LessonsAPIController : BaseController
{
    private readonly ILessonService _lessonService;
    private readonly ILogger<LessonsAPIController> _logger;

    public LessonsAPIController(
        ILessonService lessonService,
        ILogger<LessonsAPIController> logger
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
        var result = await _lessonService.ProposeLessonAsync(lessonDto, userId);
        return JsonOk(new { Message = "Lesson proposed successfully.", Lesson = result });
    }


    // Read
    [Authorize]
    [HttpGet("{contactId}/{listingId}")]
    public async Task<IActionResult> GetLessonsAsync(string contactId, int listingId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var userId = GetUserId();

        var lessons = await _lessonService.GetLessonsAsync(contactId, userId, listingId, page, pageSize);

        return JsonOk(new { Lessons = lessons });
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
        var lessons = await _lessonService.GetAllLessonsAsync(userId, filters);

        return JsonOk(new { Lessons = lessons });
    }
    // Update
    [Authorize]
    [HttpPut("respondToProposition/{lessonId}")]
    public async Task<IActionResult> RespondToPropositionAsync(int lessonId, [FromBody] bool accept)
    {
        var userId = GetUserId();
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

        return JsonOk(new
        {
            Message = accept ? "Proposition accepted." : "Proposition refused.",
            Lesson = updatedLesson
        });
    }

    // Delete
    [Authorize]
    [HttpDelete("{lessonId}/cancel")]
    public async Task<IActionResult> CancelLessonAsync(int lessonId)
    {
        try
        {
            var userId = GetUserId(); // Extract user ID from the token
            var canceledLesson = await _lessonService.UpdateLessonStatusAsync(lessonId, false, userId);

            return JsonOk(new
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
            return JsonError(ex.Message);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Message = "An error occurred while canceling the lesson.", Details = ex.Message });
        }
    }
}


