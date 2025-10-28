using Avancira.Domain.Common;
using Avancira.Domain.Common.Contracts;

namespace Avancira.Domain.Users;

/// <summary>
/// Represents per-user preference data (starting with ActiveProfile only).
/// </summary>
public class UserPreference : BaseEntity<string>, IAggregateRoot
{
    private UserPreference() { }

    private UserPreference(string userId, string activeProfile)
    {
        Id = Guid.NewGuid().ToString();
        UserId = userId;
        ActiveProfile = activeProfile;
        UpdatedOnUtc = DateTime.UtcNow;
    }

    public string UserId { get; private set; } = default!;

    /// <summary>
    /// The profile currently selected for dashboard display (student, tutor, admin).
    /// </summary>
    public string ActiveProfile { get; private set; } = "student";

    public DateTime UpdatedOnUtc { get; private set; }

    public static UserPreference Create(string userId, string defaultProfile = "student")
        => new(userId, defaultProfile);

    public void SwitchProfile(string newProfile)
    {
        ActiveProfile = newProfile;
        UpdatedOnUtc = DateTime.UtcNow;
    }
}
