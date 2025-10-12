using System;
using Avancira.Domain.UserSessions;

namespace Avancira.Infrastructure.Caching.SessionCache;

/// <summary>
/// Lightweight session info cached in memory for fast access
/// This is synced from database periodically and on-demand
/// </summary>
public class SessionTokenInfo
{
    /// <summary>Session ID</summary>
    public Guid SessionId { get; set; }

    /// <summary>User ID</summary>
    public string UserId { get; set; } = null!;

    /// <summary>Current session status</summary>
    public SessionStatus Status { get; set; } = SessionStatus.Active;

    /// <summary>Refresh token reference ID at auth server</summary>
    public string? RefreshTokenReferenceId { get; set; }

    /// <summary>When refresh token expires</summary>
    public DateTimeOffset? RefreshTokenExpiresAt { get; set; }

    /// <summary>Last activity time</summary>
    public DateTimeOffset LastActivityAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>Session creation time</summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>Check if session is still valid</summary>
    public bool IsValid => Status == SessionStatus.Active
        && RefreshTokenExpiresAt > DateTimeOffset.UtcNow;

    /// <summary>Check if session is stale and needs activity update</summary>
    public bool IsStale(TimeSpan staleDuration = default)
    {
        if (staleDuration == default)
            staleDuration = TimeSpan.FromMinutes(30);

        return DateTimeOffset.UtcNow - LastActivityAt > staleDuration;
    }
}
