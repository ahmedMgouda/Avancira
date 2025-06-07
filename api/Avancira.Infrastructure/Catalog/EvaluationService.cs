using Avancira.Application.Catalog;
using Avancira.Application.Catalog.Dtos;
using Avancira.Domain.Catalog;
using Avancira.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Avancira.Infrastructure.Catalog
{
    public class EvaluationService : IEvaluationService
    {
        private readonly AvanciraDbContext _dbContext;
        private readonly INotificationService _notificationService;
        private readonly ILogger<EvaluationService> _logger;

        public EvaluationService(
            AvanciraDbContext dbContext,
            INotificationService notificationService,
            ILogger<EvaluationService> logger
        )
        {
            _dbContext = dbContext;
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task<bool> SubmitReviewAsync(ReviewDto reviewDto, string userId)
        {
            try
            {
                // Validate the review
                if (reviewDto.Rating < 1 || reviewDto.Rating > 5)
                    throw new ArgumentException("Rating must be between 1 and 5");

                if (string.IsNullOrWhiteSpace(reviewDto.Feedback))
                    throw new ArgumentException("Review feedback is required");

                // Parse the reviewee ID as Guid (assuming it's a listing ID)
                if (!Guid.TryParse(reviewDto.RevieweeId, out var listingId))
                    throw new ArgumentException("Invalid listing ID");

                // Check if user has already reviewed this listing
                var existingReview = await _dbContext.Reviews
                    .FirstOrDefaultAsync(r => r.ListingId == listingId && r.StudentId == userId);

                if (existingReview != null)
                    throw new InvalidOperationException("You have already reviewed this listing");

                // Verify the listing exists
                var listing = await _dbContext.Listings.FindAsync(listingId);
                if (listing == null)
                    throw new KeyNotFoundException("Listing not found");

                // Create the review using the domain constructor
                var review = new ListingReview(listingId, userId, reviewDto.Rating ?? 0, reviewDto.Feedback);

                await _dbContext.Reviews.AddAsync(review);
                await _dbContext.SaveChangesAsync();

                // Notify the listing owner
                await _notificationService.NotifyAsync(
                    listing.UserId ?? string.Empty,
                    Domain.Catalog.Enums.NotificationEvent.NewReviewReceived,
                    "You have received a new review for your listing",
                    new { ListingId = listing.Id, Rating = reviewDto.Rating }
                );

                _logger.LogInformation("Review submitted successfully by user {UserId} for listing {ListingId}", userId, listingId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting review for listing {RevieweeId} by user {UserId}", reviewDto.RevieweeId, userId);
                throw;
            }
        }

        public async Task<bool> SubmitRecommendationAsync(ReviewDto reviewDto, string userId)
        {
            try
            {
                // For recommendations, we'll use the same review system but with a different context
                // This could be extended to have a separate recommendation entity if needed
                return await SubmitReviewAsync(reviewDto, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting recommendation for {RevieweeId} by user {UserId}", reviewDto.RevieweeId, userId);
                throw;
            }
        }

        public async Task<IEnumerable<ReviewDto>> GetPendingReviewsAsync(string userId)
        {
            try
            {
                // Get completed lessons where the user was a student and hasn't reviewed yet
                var pendingReviews = await (from lesson in _dbContext.Lessons
                                          join listing in _dbContext.Listings on lesson.ListingId equals listing.Id
                                          join tutor in _dbContext.Users on listing.UserId equals tutor.Id
                                          where lesson.StudentId == userId && 
                                                lesson.Status == Backend.Domain.Lessons.LessonStatus.Completed &&
                                                !_dbContext.Reviews.Any(r => r.ListingId == listing.Id && r.StudentId == userId)
                                          select new ReviewDto
                                          {
                                              RevieweeId = listing.Id.ToString(),
                                              Name = $"{tutor.FirstName} {tutor.LastName}",
                                              Subject = listing.Name,
                                              Date = lesson.Date,
                                              Avatar = tutor.ImageUrl != null ? tutor.ImageUrl.ToString() : null
                                          }).ToListAsync();

                _logger.LogInformation("Retrieved {Count} pending reviews for user {UserId}", pendingReviews.Count, userId);
                return pendingReviews;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pending reviews for user {UserId}", userId);
                throw;
            }
        }

        public async Task<IEnumerable<ReviewDto>> GetReceivedReviewsAsync(string userId)
        {
            try
            {
                var receivedReviews = await (from review in _dbContext.Reviews
                                           join listing in _dbContext.Listings on review.ListingId equals listing.Id
                                           join reviewer in _dbContext.Users on review.StudentId equals reviewer.Id
                                           where listing.UserId == userId
                                           orderby review.RatingDate descending
                                           select new ReviewDto
                                           {
                                               RevieweeId = listing.Id.ToString(),
                                               Name = $"{reviewer.FirstName} {reviewer.LastName}",
                                               Subject = listing.Name,
                                               Feedback = review.Comment,
                                               Rating = (int)review.RatingValue,
                                               Date = review.RatingDate,
                                               Avatar = reviewer.ImageUrl != null ? reviewer.ImageUrl.ToString() : null
                                           }).ToListAsync();

                _logger.LogInformation("Retrieved {Count} received reviews for user {UserId}", receivedReviews.Count, userId);
                return receivedReviews;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting received reviews for user {UserId}", userId);
                throw;
            }
        }

        public async Task<IEnumerable<ReviewDto>> GetSentReviewsAsync(string userId)
        {
            try
            {
                var sentReviews = await (from review in _dbContext.Reviews
                                       join listing in _dbContext.Listings on review.ListingId equals listing.Id
                                       join tutor in _dbContext.Users on listing.UserId equals tutor.Id
                                       where review.StudentId == userId
                                       orderby review.RatingDate descending
                                       select new ReviewDto
                                       {
                                           RevieweeId = listing.Id.ToString(),
                                           Name = $"{tutor.FirstName} {tutor.LastName}",
                                           Subject = listing.Name,
                                           Feedback = review.Comment,
                                           Rating = (int)review.RatingValue,
                                           Date = review.RatingDate,
                                           Avatar = tutor.ImageUrl != null ? tutor.ImageUrl.ToString() : null
                                       }).ToListAsync();

                _logger.LogInformation("Retrieved {Count} sent reviews for user {UserId}", sentReviews.Count, userId);
                return sentReviews;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting sent reviews for user {UserId}", userId);
                throw;
            }
        }

        public async Task<IEnumerable<ReviewDto>> GetRecommendationsAsync(string userId)
        {
            try
            {
                // For now, recommendations are the same as received reviews
                // This could be extended to filter by a specific recommendation flag if needed
                return await GetReceivedReviewsAsync(userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recommendations for user {UserId}", userId);
                throw;
            }
        }
    }
}
