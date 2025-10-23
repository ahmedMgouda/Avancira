using System;
using System.Linq;
using System.Threading;
using Avancira.Application.Persistence;
using Avancira.Application.TutorProfiles.Dtos;
using Avancira.Application.TutorProfiles.Specifications;
using Avancira.Application.TutorSubjects.Dtos;
using Avancira.Domain.Common.Exceptions;
using Avancira.Domain.Tutors;
using Mapster;

namespace Avancira.Application.TutorProfiles;

public class TutorProfileService : ITutorProfileService
{
    private readonly IRepository<TutorProfile> _tutorProfileRepository;

    public TutorProfileService(IRepository<TutorProfile> tutorProfileRepository)
    {
        _tutorProfileRepository = tutorProfileRepository;
    }

    public async Task<TutorProfileDto> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        var spec = new TutorProfileByUserIdSpec(userId);
        var profile = await _tutorProfileRepository.FirstOrDefaultAsync(spec, cancellationToken)
            ?? throw new AvanciraNotFoundException($"Tutor profile for user '{userId}' not found.");

        return MapToDto(profile);
    }

    public async Task<TutorProfileDto> UpdateAsync(TutorProfileUpdateDto request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.MinSessionDurationMinutes > request.MaxSessionDurationMinutes)
        {
            throw new AvanciraException("Minimum session duration cannot exceed maximum session duration.");
        }

        var spec = new TutorProfileByUserIdSpec(request.UserId);
        var profile = await _tutorProfileRepository.FirstOrDefaultAsync(spec, cancellationToken)
            ?? throw new AvanciraNotFoundException($"Tutor profile for user '{request.UserId}' not found.");

        profile.UpdateOverview(
            request.Headline,
            request.Description,
            request.YearsOfExperience,
            request.TeachingPhilosophy,
            request.Languages,
            request.Specializations);

        profile.UpdateLessonSettings(
            request.MinSessionDurationMinutes,
            request.MaxSessionDurationMinutes,
            request.OffersTrialLesson,
            request.TrialLessonRate,
            request.TrialLessonDurationMinutes,
            request.AllowsInstantBooking);

        profile.UpdateMedia(request.IntroVideoUrl, request.IntroVideoDurationSeconds);

        UpdateAvailabilities(profile, request.Availabilities);

        await _tutorProfileRepository.UpdateAsync(profile, cancellationToken);

        return MapToDto(profile);
    }

    public async Task<TutorProfileDto> VerifyAsync(TutorProfileVerificationDto request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var spec = new TutorProfileByUserIdSpec(request.UserId);
        var profile = await _tutorProfileRepository.FirstOrDefaultAsync(spec, cancellationToken)
            ?? throw new AvanciraNotFoundException($"Tutor profile for user '{request.UserId}' not found.");

        if (request.Approve)
        {
            profile.Verify();
        }
        else
        {
            profile.Reject(request.AdminComment);
        }

        await _tutorProfileRepository.UpdateAsync(profile, cancellationToken);

        return MapToDto(profile);
    }

    private static void UpdateAvailabilities(TutorProfile profile, IReadOnlyCollection<TutorAvailabilityUpsertDto> availabilities)
    {
        var existing = profile.Availabilities.ToDictionary(av => av.Id);

        foreach (var availability in profile.Availabilities.ToList())
        {
            if (availabilities.All(dto => dto.Id != availability.Id))
            {
                profile.Availabilities.Remove(availability);
            }
        }

        foreach (var dto in availabilities)
        {
            if (dto.Id.HasValue && existing.TryGetValue(dto.Id.Value, out var current))
            {
                current.Update(dto.DayOfWeek, dto.StartTime, dto.EndTime);
            }
            else
            {
                profile.Availabilities.Add(TutorAvailability.Create(profile.UserId, dto.DayOfWeek, dto.StartTime, dto.EndTime));
            }
        }
    }

    private static TutorProfileDto MapToDto(TutorProfile profile)
    {
        var dto = profile.Adapt<TutorProfileDto>();

        dto.Subjects = profile.Subjects
            .Select(subject => new TutorSubjectDto
            {
                Id = subject.Id,
                TutorId = subject.TutorId,
                SubjectId = subject.SubjectId,
                SubjectName = subject.Subject.Name,
                SubjectDescription = subject.Subject.Description,
                HourlyRate = subject.HourlyRate,
                IsActive = subject.IsActive,
                IsApproved = subject.IsApproved,
                IsVisible = subject.IsVisible,
                IsFeatured = subject.IsFeatured,
                SortOrder = subject.SortOrder,
                AverageRating = subject.AverageRating,
                TotalReviews = subject.TotalReviews,
                CreatedOnUtc = subject.CreatedOnUtc
            })
            .ToList();

        dto.Availabilities = profile.Availabilities
            .Select(availability => new TutorAvailabilityDto
            {
                Id = availability.Id,
                DayOfWeek = availability.DayOfWeek,
                StartTime = availability.StartTime,
                EndTime = availability.EndTime
            })
            .ToList();

        return dto;
    }
}
