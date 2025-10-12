using System;

namespace Avancira.Infrastructure.Caching.TokenCache;

/// <summary>
/// Cached access token in memory - provides fast lookup without hitting auth server
/// NOT persisted - just a performance optimization layer
/// </summary>
public class CachedAccessToken
{
    /// <summary>The JWT access token itself</summary>
    public string AccessToken { get; set; } = null!;

    /// <summary>When this access token expires</summary>
    public DateTimeOffset ExpiresAt { get; set; }

    /// <summary>The session this token belongs to</summary>
    public Guid SessionId { get; set; }

    /// <summary>User ID for quick lookups</summary>
    public string UserId { get; set; } = null!;

    /// <summary>When this cache entry was populated</summary>
    public DateTimeOffset CachedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>Check if token is still valid</summary>
    public bool IsValid => ExpiresAt > DateTimeOffset.UtcNow.AddSeconds(5); // 5 second buffer

    /// <summary>Check if token is expired or close to expiring</summary>
    public bool IsExpiredOrExpiring(TimeSpan refreshThreshold = default)
    {
        if (refreshThreshold == default)
            refreshThreshold = TimeSpan.FromMinutes(5);

        return ExpiresAt <= DateTimeOffset.UtcNow.Add(refreshThreshold);
    }
}
