using System.Linq;
using System.Threading;
using Avancira.Application.Persistence;
using Avancira.Application.TutorProfiles.Dtos;
using Avancira.Application.TutorProfiles.Specifications;
using Avancira.Domain.Common.Exceptions;
using Avancira.Domain.Tutors;
using Avancira.Application.Listings.Dtos;
using Mapster;

namespace Avancira.Application.TutorProfiles;

public sealed class TutorProfileService : ITutorProfileService
{
    private readonly IRepository<TutorProfile> _repository;

    public TutorProfileService(IRepository<TutorProfile> repository)
    {
        _repository = repository;
    }

    public async Task<TutorProfileDto> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        var spec = new TutorProfileByUserIdSpec(userId);
        var profile = await _repository.FirstOrDefaultAsync(spec, cancellationToken)
            ?? throw new AvanciraNotFoundException($"Tutor profile for user '{userId}' not found.");

        return MapToDto(profile);
    }

    public async Task<TutorProfileDto> UpdateAsync(TutorProfileUpdateDto request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.MinSessionDurationMinutes > request.MaxSessionDurationMinutes)
            throw new AvanciraException("Minimum session duration cannot exceed maximum session duration.");

        var spec = new TutorProfileByUserIdSpec(request.UserId);
        var profile = await _repository.FirstOrDefaultAsync(spec, cancellationToken)
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

        await _repository.UpdateAsync(profile, cancellationToken);
        return MapToDto(profile);
    }

    public async Task<TutorProfileDto> VerifyAsync(TutorProfileVerificationDto request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var spec = new TutorProfileByUserIdSpec(request.UserId);
        var profile = await _repository.FirstOrDefaultAsync(spec, cancellationToken)
            ?? throw new AvanciraNotFoundException($"Tutor profile for user '{request.UserId}' not found.");

        if (request.Approve)
            profile.Verify();
        else
            profile.Reject(request.AdminComment);

        await _repository.UpdateAsync(profile, cancellationToken);
        return MapToDto(profile);
    }


    private static void UpdateAvailabilities(TutorProfile profile, IReadOnlyCollection<TutorAvailabilityUpsertDto> availabilities)
    {
        var existing = profile.Availabilities.ToDictionary(a => a.Id);

        // Remove deleted ones
        foreach (var availability in profile.Availabilities.ToList())
        {
            if (availabilities.All(dto => dto.Id != availability.Id))
                profile.Availabilities.Remove(availability);
        }

        // Add or update existing
        foreach (var dto in availabilities)
        {
            if (dto.Id.HasValue && existing.TryGetValue(dto.Id.Value, out var current))
            {
                current.Update(dto.DayOfWeek, dto.StartTime, dto.EndTime);
            }
            else
            {
                profile.Availabilities.Add(
                    TutorAvailability.Create(profile.UserId, dto.DayOfWeek, dto.StartTime, dto.EndTime));
            }
        }
    }

    private static TutorProfileDto MapToDto(TutorProfile profile)
    {
        var dto = profile.Adapt<TutorProfileDto>();

        dto.Listings = profile.Listings.Select(listing => new ListingDto
        {
            Id = listing.Id,
            TutorId = listing.TutorId,
            SubjectId = listing.SubjectId,
            SubjectName = listing.Subject.Name,
            SubjectDescription = listing.Subject.Description,
            HourlyRate = listing.HourlyRate,
            IsActive = listing.IsActive,
            IsApproved = listing.IsApproved,
            IsVisible = listing.IsVisible,
            IsFeatured = listing.IsFeatured,
            SortOrder = listing.SortOrder,
            AverageRating = listing.AverageRating,
            TotalReviews = listing.TotalReviews,
            CreatedOnUtc = listing.CreatedOnUtc
        }).ToList();

        dto.Availabilities = profile.Availabilities.Select(a => new TutorAvailabilityDto
        {
            Id = a.Id,
            DayOfWeek = a.DayOfWeek,
            StartTime = a.StartTime,
            EndTime = a.EndTime
        }).ToList();

        return dto;
    }
}
