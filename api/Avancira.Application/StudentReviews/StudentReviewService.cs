using Avancira.Application.Lessons.Specifications;
using Avancira.Application.Persistence;
using Avancira.Application.StudentReviews.Dtos;
using Avancira.Application.StudentReviews.Specifications;
using Avancira.Application.Listings.Specifications;
using Avancira.Domain.Common.Exceptions;
using Avancira.Domain.Lessons;
using Avancira.Domain.Reviews;
using Avancira.Domain.Tutors;
using Mapster;

namespace Avancira.Application.StudentReviews;

public sealed class StudentReviewService : IStudentReviewService
{
    private readonly IRepository<StudentReview> _studentReviewRepository;
    private readonly IRepository<Lesson> _lessonRepository;
    private readonly IRepository<Listing> _listingRepository;

    public StudentReviewService(
        IRepository<StudentReview> studentReviewRepository,
        IRepository<Lesson> lessonRepository,
        IRepository<Listing> listingRepository)
    {
        _studentReviewRepository = studentReviewRepository;
        _lessonRepository = lessonRepository;
        _listingRepository = listingRepository;
    }

    public async Task<StudentReviewDto> GetByLessonIdAsync(int lessonId, CancellationToken cancellationToken = default)
    {
        var spec = new StudentReviewByLessonIdSpec(lessonId);
        var review = await _studentReviewRepository.FirstOrDefaultAsync(spec, cancellationToken)
            ?? throw new AvanciraNotFoundException($"Review for lesson '{lessonId}' not found.");

        return review.Adapt<StudentReviewDto>();
    }

    public async Task<StudentReviewDto> CreateAsync(StudentReviewCreateDto request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var lesson = await _lessonRepository.FirstOrDefaultAsync(new LessonByIdSpec(request.LessonId), cancellationToken)
            ?? throw new AvanciraNotFoundException($"Lesson '{request.LessonId}' not found.");

        if (lesson.Status != LessonStatus.Completed)
            throw new AvanciraException("Reviews can only be left for completed lessons.");

        if (lesson.Review is not null)
            throw new AvanciraException("Lesson already has a review.");

        if (!string.Equals(lesson.StudentId, request.StudentId, StringComparison.Ordinal))
            throw new AvanciraException("Only the student who attended the lesson can leave a review.");

        if (request.Rating is < 1 or > 5)
            throw new AvanciraException("Rating must be between 1 and 5.");

        var review = StudentReview.Create(
            request.StudentId,
            request.LessonId,
            request.Rating,
            request.Comment,
            request.CommunicationRating,
            request.KnowledgeRating,
            request.ProfessionalismRating,
            request.ValueRating);

        await _studentReviewRepository.AddAsync(review, cancellationToken);
        await UpdateListingRatingAsync(lesson.ListingId, cancellationToken);

        return review.Adapt<StudentReviewDto>();
    }

    public async Task<StudentReviewDto> RespondAsync(StudentReviewResponseDto request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var review = await _studentReviewRepository.GetByIdAsync(request.ReviewId, cancellationToken)
            ?? throw new AvanciraNotFoundException($"Review '{request.ReviewId}' not found.");

        if (string.IsNullOrWhiteSpace(request.TutorResponse))
            throw new AvanciraException("Tutor response cannot be empty.");

        review.Respond(request.TutorResponse);
        await _studentReviewRepository.UpdateAsync(review, cancellationToken);

        return review.Adapt<StudentReviewDto>();
    }

    private async Task UpdateListingRatingAsync(int listingId, CancellationToken cancellationToken)
    {
        var reviews = await _studentReviewRepository.ListAsync(new StudentReviewsByListingSpec(listingId), cancellationToken);
        var listing = await _listingRepository.FirstOrDefaultAsync(new ListingByIdSpec(listingId), cancellationToken)
            ?? throw new AvanciraNotFoundException($"Listing '{listingId}' not found.");

        if (reviews.Count == 0)
        {
            listing.UpdateRatings(0, 0);
        }
        else
        {
            var averageRating = reviews.Average(r => r.Rating);
            listing.UpdateRatings(averageRating, reviews.Count);
        }

        await _listingRepository.UpdateAsync(listing, cancellationToken);

        if (listing.Tutor is not null)
        {
            var tutorListings = await _listingRepository.ListAsync(new ListingsByTutorSpec(listing.TutorId), cancellationToken);

            if (tutorListings.Count > 0)
            {
                var totalReviews = tutorListings.Sum(l => l.TotalReviews);
                var averageRating = totalReviews == 0
                    ? 0
                    : tutorListings.Sum(l => l.AverageRating * l.TotalReviews) / totalReviews;

                listing.Tutor.UpdateMetrics(
                    averageRating,
                    listing.Tutor.AverageResponseTimeMinutes,
                    listing.Tutor.BookingAcceptanceRate);
            }
        }
    }
}
