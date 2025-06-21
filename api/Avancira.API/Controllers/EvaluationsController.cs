using System.Threading.Tasks;
using Avancira.Application.Catalog;
using Avancira.Application.Catalog.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Avancira.API.Controllers;

[Route("api/evaluations")]
public class EvaluationsController : BaseApiController
{
    private readonly IEvaluationService _evaluationService;
    private readonly ILogger<EvaluationsController> _logger;

    public EvaluationsController(
        IEvaluationService evaluationService,
        ILogger<EvaluationsController> logger
    )
    {
        _evaluationService = evaluationService;
        _logger = logger;
    }

    // Create
    [Authorize]
    [HttpPost("review")]
    public async Task<IActionResult> LeaveReviewAsync([FromBody] ReviewDto reviewDto)
    {
        // TODO: Implement proper user ID extraction from claims
        // var userId = User.GetUserId();
        var userId = "temp-user-id"; // Temporary placeholder

        // Validate the input
        if (reviewDto == null)
        {
            return BadRequest("Review data is required.");
        }
        if (string.IsNullOrEmpty(reviewDto.RevieweeId) || string.IsNullOrWhiteSpace(reviewDto.Subject) || string.IsNullOrWhiteSpace(reviewDto.Feedback))
        {
            return BadRequest("Invalid review details.");
        }

        // Check if the user is authorized to write this review (optional logic)
        if (!await _evaluationService.SubmitReviewAsync(reviewDto, userId))
        {
            return BadRequest("You are not authorized to leave this review.");
        }

        return Ok(new
        {
            success = true,
            message = "Review submitted successfully."
        });
    }

    [Authorize]
    [HttpPost("recommendation")]
    public async Task<IActionResult> SubmitRecommendation([FromBody] ReviewDto dto)
    {
        // TODO: Implement proper user ID extraction from claims
        // var userId = User.GetUserId();
        var userId = "temp-user-id"; // Temporary placeholder

        if (string.IsNullOrEmpty(dto.RevieweeId) || string.IsNullOrEmpty(dto.Feedback))
            return BadRequest("Invalid data.");

        if (!await _evaluationService.SubmitRecommendationAsync(dto, userId))
        {
            return BadRequest("Failed to submit recommendation.");
        }

        return Ok(new
        {
            success = true,
            message = "Recommendation submitted successfully."
        });
    }

    // Read
    [Authorize]
    [HttpGet]
    public async Task<IActionResult> GetEvaluationsAsync()
    {
        // TODO: Implement proper user ID extraction from claims
        // var userId = User.GetUserId();
        var userId = "temp-user-id"; // Temporary placeholder

        // Identify pending reviews based on the user's role
        var pendingReviews = await _evaluationService.GetPendingReviewsAsync(userId);
        var receivedReviews = await _evaluationService.GetReceivedReviewsAsync(userId);
        var sentReviews = await _evaluationService.GetSentReviewsAsync(userId);
        var recommendations = await _evaluationService.GetRecommendationsAsync(userId);

        return Ok(new
        {
            PendingReviews = pendingReviews,
            ReceivedReviews = receivedReviews,
            SentReviews = sentReviews,
            Recommendations = recommendations
        });

    }
}
