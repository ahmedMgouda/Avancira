using Avancira.Application.TutorSubjects.Dtos;

namespace Avancira.Application.TutorProfiles.Dtos;

public class TutorProfileDto
{
    public string UserId { get; set; } = default!;
    public string Headline { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsVerified { get; set; }
    public DateTime? VerifiedOnUtc { get; set; }
    public double AverageRating { get; set; }
    public bool IsActive { get; set; }
    public bool IsFeatured { get; set; }
    public int SortOrder { get; set; }
    public int YearsOfExperience { get; set; }
    public string? TeachingPhilosophy { get; set; }
    public string? Specializations { get; set; }
    public string? IntroVideoUrl { get; set; }
    public int? IntroVideoDurationSeconds { get; set; }
    public string? Languages { get; set; }
    public decimal AverageResponseTimeMinutes { get; set; }
    public decimal BookingAcceptanceRate { get; set; }
    public int MinSessionDurationMinutes { get; set; }
    public int MaxSessionDurationMinutes { get; set; }
    public bool OffersTrialLesson { get; set; }
    public decimal? TrialLessonRate { get; set; }
    public int? TrialLessonDurationMinutes { get; set; }
    public bool AllowsInstantBooking { get; set; }
    public DateTime? TopTutorSinceUtc { get; set; }
    public bool IsRisingTalent { get; set; }
    public string? AdminComment { get; set; }
    public DateTime CreatedOnUtc { get; set; }
    public IReadOnlyCollection<TutorSubjectDto> Subjects { get; set; } = Array.Empty<TutorSubjectDto>();
    public IReadOnlyCollection<TutorAvailabilityDto> Availabilities { get; set; } = Array.Empty<TutorAvailabilityDto>();
}

public class TutorAvailabilityDto
{
    public int Id { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
}
