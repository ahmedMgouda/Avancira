using System;
using System.Linq;
using System.Threading;
using Avancira.Application.Lessons.Specifications;
using Avancira.Application.Persistence;
using Avancira.Application.StudentReviews.Dtos;
using Avancira.Application.StudentReviews.Specifications;
using Avancira.Application.TutorSubjects.Specifications;
using Avancira.Domain.Common.Exceptions;
using Avancira.Domain.Lessons;
using Avancira.Domain.Reviews;
using Avancira.Domain.Tutors;
using Mapster;

namespace Avancira.Application.StudentReviews;

public class StudentReviewService : IStudentReviewService
{
    private readonly IRepository<StudentReview> _studentReviewRepository;
    private readonly IRepository<Lesson> _lessonRepository;
    private readonly IRepository<TutorSubject> _tutorSubjectRepository;

    public StudentReviewService(
        IRepository<StudentReview> studentReviewRepository,
        IRepository<Lesson> lessonRepository,
        IRepository<TutorSubject> tutorSubjectRepository)
    {
        _studentReviewRepository = studentReviewRepository;
        _lessonRepository = lessonRepository;
        _tutorSubjectRepository = tutorSubjectRepository;
    }

    public async Task<StudentReviewDto> GetByLessonIdAsync(int lessonId, CancellationToken cancellationToken = default)
    {
        var review = await _studentReviewRepository.FirstOrDefaultAsync(r => r.LessonId == lessonId, cancellationToken)
            ?? throw new AvanciraNotFoundException($"Review for lesson '{lessonId}' not found.");

        return review.Adapt<StudentReviewDto>();
    }

    public async Task<StudentReviewDto> CreateAsync(StudentReviewCreateDto request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var lesson = await _lessonRepository.FirstOrDefaultAsync(new LessonByIdSpec(request.LessonId), cancellationToken)
            ?? throw new AvanciraNotFoundException($"Lesson '{request.LessonId}' not found.");

        if (lesson.Status != LessonStatus.Completed)
        {
            throw new AvanciraException("Reviews can only be left for completed lessons.");
        }

        if (lesson.Review is not null)
        {
            throw new AvanciraException("Lesson already has a review.");
        }

        if (!string.Equals(lesson.StudentId, request.StudentId, StringComparison.Ordinal))
        {
            throw new AvanciraException("Only the student who attended the lesson can leave a review.");
        }

        if (request.Rating is < 1 or > 5)
        {
            throw new AvanciraException("Rating must be between 1 and 5.");
        }

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

        await UpdateTutorSubjectRatingAsync(lesson.TutorSubjectId, cancellationToken);

        return review.Adapt<StudentReviewDto>();
    }

    public async Task<StudentReviewDto> RespondAsync(StudentReviewResponseDto request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var review = await _studentReviewRepository.GetByIdAsync(request.ReviewId, cancellationToken)
            ?? throw new AvanciraNotFoundException($"Review '{request.ReviewId}' not found.");

        if (string.IsNullOrWhiteSpace(request.TutorResponse))
        {
            throw new AvanciraException("Tutor response cannot be empty.");
        }

        review.Respond(request.TutorResponse);
        await _studentReviewRepository.UpdateAsync(review, cancellationToken);

        return review.Adapt<StudentReviewDto>();
    }

    private async Task UpdateTutorSubjectRatingAsync(int tutorSubjectId, CancellationToken cancellationToken)
    {
        var reviews = await _studentReviewRepository.ListAsync(new StudentReviewsByTutorSubjectSpec(tutorSubjectId), cancellationToken);
        var tutorSubject = await _tutorSubjectRepository.FirstOrDefaultAsync(new TutorSubjectByIdSpec(tutorSubjectId), cancellationToken)
            ?? throw new AvanciraNotFoundException($"Tutor subject '{tutorSubjectId}' not found.");

        if (reviews.Count == 0)
        {
            tutorSubject.UpdateRatings(0, 0);
        }
        else
        {
            var averageRating = reviews.Average(r => r.Rating);
            tutorSubject.UpdateRatings(averageRating, reviews.Count);
        }

        await _tutorSubjectRepository.UpdateAsync(tutorSubject, cancellationToken);

        if (tutorSubject.Tutor is not null)
        {
            var tutorSubjects = await _tutorSubjectRepository.ListAsync(new TutorSubjectsByTutorSpec(tutorSubject.TutorId), cancellationToken);
            if (tutorSubjects.Count > 0)
            {
                var totalReviews = tutorSubjects.Sum(ts => ts.TotalReviews);
                var averageRating = totalReviews == 0 ? 0 : tutorSubjects.Sum(ts => ts.AverageRating * ts.TotalReviews) / totalReviews;
                tutorSubject.Tutor.UpdateMetrics(averageRating, tutorSubject.Tutor.AverageResponseTimeMinutes, tutorSubject.Tutor.BookingAcceptanceRate);
            }
        }
    }
}
