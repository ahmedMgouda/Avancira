using Avancira.Domain.Common;
using Avancira.Domain.Common.Contracts;

namespace Avancira.Domain.Tutors;

public class TutorProfile : BaseEntity<string>, IAggregateRoot
{
    private TutorProfile() { }

    private TutorProfile(string userId)
    {
        Id = userId;
        CreatedOnUtc = DateTime.UtcNow;

        IsActive = false;
        IsVerified = false;
        IsComplete = false;
        ShowTutorProfileReminder = true;
        BookingAcceptanceRate = 100;
        MinSessionDurationMinutes = 60;
        MaxSessionDurationMinutes = 120;
    }

    public string UserId => Id;

    public string Headline { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public int YearsOfExperience { get; private set; }
    public string? TeachingPhilosophy { get; private set; }
    public string? Specializations { get; private set; }
    public string? Languages { get; private set; }

    public bool IsVerified { get; private set; }
    public DateTime? VerifiedOnUtc { get; private set; }
    public bool IsActive { get; private set; }
    public bool IsFeatured { get; private set; }
    public int SortOrder { get; private set; }

    public double AverageRating { get; private set; }
    public decimal AverageResponseTimeMinutes { get; private set; }
    public decimal BookingAcceptanceRate { get; private set; }

    public int MinSessionDurationMinutes { get; private set; }
    public int MaxSessionDurationMinutes { get; private set; }
    public bool OffersTrialLesson { get; private set; }
    public decimal? TrialLessonRate { get; private set; }
    public int? TrialLessonDurationMinutes { get; private set; }
    public bool AllowsInstantBooking { get; private set; }

    public string? IntroVideoUrl { get; private set; }
    public int? IntroVideoDurationSeconds { get; private set; }

    public bool IsComplete { get; private set; }
    public bool ShowTutorProfileReminder { get; private set; }
    public bool IsRisingTalent { get; private set; }
    public DateTime? TopTutorSinceUtc { get; private set; }
    public string? AdminComment { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }

    public ICollection<Listing> Listings { get; private set; } = new HashSet<Listing>();
    public ICollection<TutorAvailability> Availabilities { get; private set; } = new HashSet<TutorAvailability>();

    public static TutorProfile Create(string userId) => new(userId);

    public void UpdateOverview(
        string headline,
        string description,
        int yearsOfExperience,
        string? teachingPhilosophy,
        string? languages,
        string? specializations)
    {
        Headline = headline?.Trim() ?? string.Empty;
        Description = description?.Trim() ?? string.Empty;
        YearsOfExperience = yearsOfExperience;
        TeachingPhilosophy = teachingPhilosophy?.Trim();
        Languages = languages?.Trim();
        Specializations = specializations?.Trim();

        EvaluateCompletion();
    }

    public void UpdateLessonSettings(
        int minSessionDurationMinutes,
        int maxSessionDurationMinutes,
        bool offersTrialLesson,
        decimal? trialLessonRate,
        int? trialLessonDurationMinutes,
        bool allowsInstantBooking)
    {
        MinSessionDurationMinutes = minSessionDurationMinutes;
        MaxSessionDurationMinutes = maxSessionDurationMinutes;
        OffersTrialLesson = offersTrialLesson;
        TrialLessonRate = trialLessonRate;
        TrialLessonDurationMinutes = trialLessonDurationMinutes;
        AllowsInstantBooking = allowsInstantBooking;

        EvaluateCompletion();
    }

    public void UpdateMedia(string? introVideoUrl, int? introVideoDurationSeconds)
    {
        IntroVideoUrl = introVideoUrl;
        IntroVideoDurationSeconds = introVideoDurationSeconds;
        EvaluateCompletion();
    }

    public void Verify()
    {
        IsVerified = true;
        IsActive = true;
        VerifiedOnUtc = DateTime.UtcNow;
    }

    public void Reject(string? adminComment)
    {
        IsVerified = false;
        IsActive = false;
        VerifiedOnUtc = null;
        AdminComment = adminComment;
    }

    public void Feature(bool isFeatured, int sortOrder)
    {
        IsFeatured = isFeatured;
        SortOrder = sortOrder;
    }

    public void UpdateMetrics(double avgRating, decimal avgResponse, decimal bookingRate)
    {
        AverageRating = avgRating;
        AverageResponseTimeMinutes = avgResponse;
        BookingAcceptanceRate = bookingRate;
    }

    public void HideReminder() => ShowTutorProfileReminder = false;

    private void EvaluateCompletion()
    {
        var hasBasicInfo = !string.IsNullOrWhiteSpace(Headline)
                        && !string.IsNullOrWhiteSpace(Description)
                        && YearsOfExperience > 0;

        var hasTeachingData = !string.IsNullOrWhiteSpace(TeachingPhilosophy)
                           || !string.IsNullOrWhiteSpace(Specializations);

        var hasLanguage = !string.IsNullOrWhiteSpace(Languages);

        var hasMedia = !string.IsNullOrWhiteSpace(IntroVideoUrl);

        IsComplete = hasBasicInfo && hasTeachingData && hasLanguage && hasMedia;

        if (IsComplete)
            ShowTutorProfileReminder = false;
    }
}
