namespace Avancira.Application.TutorProfiles.Dtos;

public class TutorProfileUpdateDto
{
    public string UserId { get; set; } = default!;
    public string Headline { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int YearsOfExperience { get; set; }
    public string? TeachingPhilosophy { get; set; }
    public string? Specializations { get; set; }
    public string? Languages { get; set; }
    public int MinSessionDurationMinutes { get; set; }
    public int MaxSessionDurationMinutes { get; set; }
    public bool OffersTrialLesson { get; set; }
    public decimal? TrialLessonRate { get; set; }
    public int? TrialLessonDurationMinutes { get; set; }
    public bool AllowsInstantBooking { get; set; }
    public string? IntroVideoUrl { get; set; }
    public int? IntroVideoDurationSeconds { get; set; }
    public IReadOnlyCollection<TutorAvailabilityUpsertDto> Availabilities { get; set; } = Array.Empty<TutorAvailabilityUpsertDto>();
}

public class TutorAvailabilityUpsertDto
{
    public int? Id { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
}
