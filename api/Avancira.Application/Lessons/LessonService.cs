using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avancira.Application.Lessons.Dtos;
using Avancira.Application.Lessons.Specifications;
using Avancira.Application.Persistence;
using Avancira.Application.Listings.Specifications;
using Avancira.Domain.Common.Exceptions;
using Avancira.Domain.Lessons;
using Avancira.Domain.Students;
using Avancira.Domain.Tutors;
using Mapster;

namespace Avancira.Application.Lessons;

public class LessonService : ILessonService
{
    private readonly IRepository<Lesson> _lessonRepository;
    private readonly IRepository<StudentProfile> _studentProfileRepository;
    private readonly IReadRepository<Listing> _listingRepository;

    public LessonService(
        IRepository<Lesson> lessonRepository,
        IRepository<StudentProfile> studentProfileRepository,
        IReadRepository<Listing> listingRepository)
    {
        _lessonRepository = lessonRepository;
        _studentProfileRepository = studentProfileRepository;
        _listingRepository = listingRepository;
    }

    public async Task<LessonDto> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var spec = new LessonByIdSpec(id);
        var lesson = await _lessonRepository.FirstOrDefaultAsync(spec, cancellationToken)
            ?? throw new AvanciraNotFoundException($"Lesson '{id}' not found.");

        return lesson.Adapt<LessonDto>();
    }

    public async Task<LessonDto> CreateAsync(LessonCreateDto request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.DurationMinutes <= 0)
        {
            throw new AvanciraException("Duration must be greater than zero.");
        }

        if (request.ScheduledAtUtc <= DateTime.UtcNow)
        {
            throw new AvanciraException("Lesson must be scheduled in the future.");
        }

        var listing = await _listingRepository.FirstOrDefaultAsync(new ListingWithTutorSpec(request.ListingId), cancellationToken)
            ?? throw new AvanciraNotFoundException($"Listing '{request.ListingId}' not found.");

        if (!listing.IsActive || !listing.IsApproved || !listing.IsVisible)
        {
            throw new AvanciraException("Selected listing is not available for booking.");
        }

        var tutorProfile = listing.Tutor
            ?? throw new AvanciraException("Tutor profile not found for listing.");

        var studentProfile = await _studentProfileRepository.GetByIdAsync(request.StudentId, cancellationToken)
            ?? throw new AvanciraException("Student profile not found.");

        var duration = TimeSpan.FromMinutes(request.DurationMinutes);
        if (duration.TotalMinutes < tutorProfile.MinSessionDurationMinutes || duration.TotalMinutes > tutorProfile.MaxSessionDurationMinutes)
        {
            throw new AvanciraException($"Lesson duration must be between {tutorProfile.MinSessionDurationMinutes} and {tutorProfile.MaxSessionDurationMinutes} minutes.");
        }

        bool canUseTrial = request.UseTrialLesson && tutorProfile.OffersTrialLesson && !studentProfile.HasUsedTrialLesson;
        decimal finalPrice;
        if (canUseTrial)
        {
            finalPrice = tutorProfile.TrialLessonRate ?? listing.HourlyRate;
            studentProfile.MarkTrialLessonUsed();
            await _studentProfileRepository.UpdateAsync(studentProfile, cancellationToken);
        }
        else
        {
            finalPrice = listing.HourlyRate * (decimal)duration.TotalMinutes / 60m;
        }

        var lesson = Lesson.Create(
            request.StudentId,
            request.TutorId,
            request.ListingId,
            request.ScheduledAtUtc,
            duration,
            LessonStatus.Pending,
            decimal.Round(finalPrice, 2, MidpointRounding.AwayFromZero),
            request.InstantBooking && tutorProfile.AllowsInstantBooking);

        lesson.UpdateMeetingDetails(request.MeetingUrl, request.MeetingId, request.MeetingPassword);

        await _lessonRepository.AddAsync(lesson, cancellationToken);

        return lesson.Adapt<LessonDto>();
    }

    public async Task<LessonDto> ConfirmAsync(int lessonId, CancellationToken cancellationToken = default)
    {
        var lesson = await GetTrackedLessonAsync(lessonId, cancellationToken);

        if (lesson.Status != LessonStatus.Pending)
        {
            throw new AvanciraException("Only pending lessons can be confirmed.");
        }

        lesson.Confirm();
        await _lessonRepository.UpdateAsync(lesson, cancellationToken);

        return lesson.Adapt<LessonDto>();
    }

    public async Task<LessonDto> DeclineAsync(LessonDeclineDto request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var lesson = await GetTrackedLessonAsync(request.LessonId, cancellationToken);

        if (lesson.Status != LessonStatus.Pending)
        {
            throw new AvanciraException("Only pending lessons can be declined.");
        }

        lesson.Cancel(request.TutorId, request.Reason);
        await _lessonRepository.UpdateAsync(lesson, cancellationToken);

        return lesson.Adapt<LessonDto>();
    }

    public async Task<LessonDto> StartAsync(int lessonId, CancellationToken cancellationToken = default)
    {
        var lesson = await GetTrackedLessonAsync(lessonId, cancellationToken);

        if (lesson.Status != LessonStatus.Scheduled)
        {
            throw new AvanciraException("Only scheduled lessons can be started.");
        }

        lesson.Start();
        await _lessonRepository.UpdateAsync(lesson, cancellationToken);

        return lesson.Adapt<LessonDto>();
    }

    public async Task<LessonDto> CompleteAsync(LessonCompleteDto request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var lesson = await GetTrackedLessonAsync(request.LessonId, cancellationToken);

        if (lesson.Status != LessonStatus.InProgress && lesson.Status != LessonStatus.Scheduled)
        {
            throw new AvanciraException("Only in-progress or scheduled lessons can be completed.");
        }

        lesson.Complete(request.ActualDuration, request.SessionSummary, request.TutorNotes);
        await _lessonRepository.UpdateAsync(lesson, cancellationToken);

        return lesson.Adapt<LessonDto>();
    }

    public async Task<LessonDto> CancelAsync(LessonCancelDto request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var lesson = await GetTrackedLessonAsync(request.LessonId, cancellationToken);

        if (lesson.Status != LessonStatus.Scheduled && lesson.Status != LessonStatus.Pending)
        {
            throw new AvanciraException("Only scheduled or pending lessons can be canceled.");
        }

        lesson.Cancel(request.CanceledBy, request.Reason);
        await _lessonRepository.UpdateAsync(lesson, cancellationToken);

        return lesson.Adapt<LessonDto>();
    }

    public async Task<LessonDto> RescheduleAsync(LessonRescheduleDto request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var lesson = await GetTrackedLessonAsync(request.LessonId, cancellationToken);

        if (lesson.Status != LessonStatus.Scheduled && lesson.Status != LessonStatus.Pending)
        {
            throw new AvanciraException("Only scheduled or pending lessons can be rescheduled.");
        }

        if (request.DurationMinutes <= 0)
        {
            throw new AvanciraException("Duration must be greater than zero.");
        }

        if (request.NewScheduledAtUtc <= DateTime.UtcNow)
        {
            throw new AvanciraException("New lesson time must be in the future.");
        }

        var listing = await _listingRepository.FirstOrDefaultAsync(new ListingWithTutorSpec(lesson.ListingId), cancellationToken)
            ?? throw new AvanciraNotFoundException("Listing not found.");

        var tutorProfile = listing.Tutor
            ?? throw new AvanciraException("Tutor profile not found.");

        var duration = TimeSpan.FromMinutes(request.DurationMinutes);
        if (duration.TotalMinutes < tutorProfile.MinSessionDurationMinutes || duration.TotalMinutes > tutorProfile.MaxSessionDurationMinutes)
        {
            throw new AvanciraException($"Lesson duration must be between {tutorProfile.MinSessionDurationMinutes} and {tutorProfile.MaxSessionDurationMinutes} minutes.");
        }

        var newLesson = Lesson.Create(
            lesson.StudentId,
            lesson.TutorId,
            lesson.ListingId,
            request.NewScheduledAtUtc,
            duration,
            LessonStatus.Pending,
            lesson.FinalPrice,
            false);

        await _lessonRepository.AddAsync(newLesson, cancellationToken);

        lesson.MarkRescheduled(newLesson.Id);
        await _lessonRepository.UpdateAsync(lesson, cancellationToken);

        return newLesson.Adapt<LessonDto>();
    }

    private async Task<Lesson> GetTrackedLessonAsync(int lessonId, CancellationToken cancellationToken)
    {
        return await _lessonRepository.GetByIdAsync(lessonId, cancellationToken)
            ?? throw new AvanciraNotFoundException($"Lesson '{lessonId}' not found.");
    }
}
