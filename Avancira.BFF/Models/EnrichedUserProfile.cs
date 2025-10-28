namespace Avancira.BFF.Models;

public record EnrichedUserProfile
{
    public string UserId { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string? ProfileImageUrl { get; init; }
    public string[] Roles { get; init; } = Array.Empty<string>();
    public string ActiveProfile { get; init; } = "student";
    public bool HasAdminAccess { get; init; }
    public TutorProfile? TutorProfile { get; init; }
    public StudentProfile? StudentProfile { get; init; }
}

public record TutorProfile
{
    public bool IsActive { get; init; }
    public bool IsVerified { get; init; }
    public bool IsComplete { get; init; }
    public bool ShowReminder { get; init; }
}

public record StudentProfile
{
    public bool CanBook { get; init; }
    public string SubscriptionStatus { get; init; } = "Inactive";
    public DateTime? SubscriptionEndsOnUtc { get; init; }
    public bool IsComplete { get; init; }
    public bool ShowReminder { get; init; }
}