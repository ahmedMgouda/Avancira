using Avancira.Application.Catalog.Dtos;
using System.Collections.Generic;
using System.Threading.Tasks;

public interface IEvaluationService
{
    // Create
    Task<bool> SubmitReviewAsync(ReviewDto reviewDto, string userId);
    Task<bool> SubmitRecommendationAsync(ReviewDto reviewDto, string userId);

    // Read
    Task<IEnumerable<ReviewDto>> GetPendingReviewsAsync(string userId);
    Task<IEnumerable<ReviewDto>> GetReceivedReviewsAsync(string userId);
    Task<IEnumerable<ReviewDto>> GetSentReviewsAsync(string userId);
    Task<IEnumerable<ReviewDto>> GetRecommendationsAsync(string userId);
}

