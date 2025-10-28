namespace Avancira.Application.Identity.Users.Dtos;

/// <summary>
/// Enriched user profile returned to BFF after authentication.
/// Contains all data needed by the SPA to render the user interface.
/// </summary>
public record EnrichedUserProfileDto
{
    /// <summary>
    /// Unique user identifier
    /// </summary>
    public string UserId { get; init; } = string.Empty;

    /// <summary>
    /// User's first name
    /// </summary>
    public string FirstName { get; init; } = string.Empty;

    /// <summary>
    /// User's last name
    /// </summary>
    public string LastName { get; init; } = string.Empty;

    /// <summary>
    /// Full name (first + last)
    /// </summary>
    public string FullName { get; init; } = string.Empty;

    /// <summary>
    /// URL to user's profile image
    /// </summary>
    public string? ProfileImageUrl { get; init; }

    /// <summary>
    /// User's assigned roles (e.g., ["Tutor", "Student", "Admin"])
    /// </summary>
    public string[] Roles { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Currently active profile: "tutor" or "student"
    /// </summary>
    public string ActiveProfile { get; init; } = "student";

    /// <summary>
    /// Whether user has admin/super-admin privileges
    /// </summary>
    public bool HasAdminAccess { get; init; }

    /// <summary>
    /// Tutor-specific profile data (null if user is not a tutor)
    /// </summary>
    public TutorProfileSummaryDto? TutorProfile { get; init; }

    /// <summary>
    /// Student-specific profile data (null if user is not a student)
    /// </summary>
    public StudentProfileSummaryDto? StudentProfile { get; init; }
}

/// <summary>
/// Tutor-specific profile information
/// </summary>
public record TutorProfileSummaryDto
{
    /// <summary>
    /// Whether tutor profile is active and can accept bookings
    /// </summary>
    public bool IsActive { get; init; }

    /// <summary>
    /// Whether tutor has been verified by administrators
    /// </summary>
    public bool IsVerified { get; init; }

    /// <summary>
    /// Whether tutor has completed all required profile fields
    /// </summary>
    public bool IsComplete { get; init; }

    /// <summary>
    /// Whether to show profile completion reminder in UI
    /// </summary>
    public bool ShowReminder { get; init; }
}

/// <summary>
/// Student-specific profile information
/// </summary>
public record StudentProfileSummaryDto
{
    /// <summary>
    /// Whether student can book sessions
    /// </summary>
    public bool CanBook { get; init; }

    /// <summary>
    /// Current subscription status: "Active", "Inactive", "Expired", "Cancelled"
    /// </summary>
    public string SubscriptionStatus { get; init; } = "Inactive";

    /// <summary>
    /// When the subscription expires (null if no active subscription)
    /// </summary>
    public DateTime? SubscriptionEndsOnUtc { get; init; }

    /// <summary>
    /// Whether student has completed all required profile fields
    /// </summary>
    public bool IsComplete { get; init; }

    /// <summary>
    /// Whether to show profile completion reminder in UI
    /// </summary>
    public bool ShowReminder { get; init; }
}