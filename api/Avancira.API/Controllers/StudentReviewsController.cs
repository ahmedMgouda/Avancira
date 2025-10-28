using System.Threading;
using Avancira.Application.StudentReviews;
using Avancira.Application.StudentReviews.Dtos;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Avancira.API.Controllers;

[Route("api/reviews")]
public class StudentReviewsController : BaseApiController
{
    private readonly IStudentReviewService _studentReviewService;

    public StudentReviewsController(IStudentReviewService studentReviewService)
    {
        _studentReviewService = studentReviewService;
    }

    [HttpGet("lesson/{lessonId:int}")]
    [ProducesResponseType(typeof(StudentReviewDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetReviewByLesson(int lessonId, CancellationToken cancellationToken)
    {
        var review = await _studentReviewService.GetByLessonIdAsync(lessonId, cancellationToken);
        return Ok(review);
    }

    [HttpPost]
    [ProducesResponseType(typeof(StudentReviewDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateReview([FromBody] StudentReviewCreateDto request, CancellationToken cancellationToken)
    {
        var review = await _studentReviewService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetReviewByLesson), new { lessonId = review.LessonId }, review);
    }

    [HttpPost("respond")]
    [ProducesResponseType(typeof(StudentReviewDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> RespondToReview([FromBody] StudentReviewResponseDto request, CancellationToken cancellationToken)
    {
        var review = await _studentReviewService.RespondAsync(request, cancellationToken);
        return Ok(review);
    }
}
