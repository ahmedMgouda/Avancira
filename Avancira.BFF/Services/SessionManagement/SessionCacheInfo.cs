using Avancira.Domain.UserSessions;

namespace Avancira.BFF.Services.SessionManagement;

/// <summary>
/// Lightweight cached representation of a user session.
/// Stored in IMemoryCache (or Redis) for fast access and validation
/// without querying the database.
/// </summary>
public class SessionCacheInfo
{
    /// <summary>Unique session ID.</summary>
    public Guid SessionId { get; set; }

    /// <summary>User ID this session belongs to.</summary>
    public string UserId { get; set; } = null!;

    /// <summary>Current session status (Active, Revoked, Expired, etc.).</summary>
    public SessionStatus Status { get; set; }

    /// <summary>
    /// When the refresh token expires, representing the max lifetime of this session.
    /// Used for quick validity checks.
    /// </summary>
    public DateTimeOffset? RefreshTokenExpiresAt { get; set; }

    /// <summary>
    /// Last recorded user activity timestamp (cached).
    /// This is lazily persisted to the database every ~30 minutes.
    /// </summary>
    public DateTimeOffset LastActivityAt { get; set; }

    /// <summary>Timestamp when the session was created.</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Determines whether this cached session info is still valid.
    /// Mirrors logic from <see cref="UserSession"/> validation.
    /// </summary>
    public bool IsValid()
    {
        if (Status != SessionStatus.Active)
            return false;

        if (RefreshTokenExpiresAt.HasValue &&
            RefreshTokenExpiresAt.Value <= DateTimeOffset.UtcNow)
            return false;

        return true;
    }
}
