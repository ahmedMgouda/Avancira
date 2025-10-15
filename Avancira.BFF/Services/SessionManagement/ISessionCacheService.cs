using Avancira.BFF.Services.SessionManagement;
using Avancira.Domain.UserSessions;

namespace Avancira.BFF.Services;

/// <summary>
/// Interface for managing user sessions with caching in the BFF layer.
/// 
/// Responsibilities:
/// - Create and revoke sessions
/// - Update refresh tokens
/// - Validate active sessions
/// - Perform lazy activity updates
/// </summary>
public interface ISessionCacheService
{
    Task<UserSession> CreateSessionAsync(
        string userId,
        string deviceId,
        string? deviceName,
        string? userAgent,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task<UserSession?> GetSessionAsync(Guid sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieve session info from cache only (fast lookup).
    /// </summary>
    SessionCacheInfo? GetCachedSessionInfo(Guid sessionId);

    /// <summary>
    /// Update last activity timestamp (lazy-write mode).
    /// Persists only if last DB write was 30+ minutes ago.
    /// </summary>
    Task UpdateActivityLazyAsync(Guid sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate whether the session is active and not expired/revoked.
    /// </summary>
    Task<bool> IsSessionValidAsync(Guid sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revoke a single session.
    /// </summary>
    Task RevokeSessionAsync(Guid sessionId, string reason = "User logout", CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all active sessions for a user.
    /// </summary>
    Task<IEnumerable<UserSession>> GetUserSessionsAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revoke all user sessions except a specific one (e.g. current device).
    /// </summary>
    Task RevokeOtherSessionsAsync(string userId, Guid exceptSessionId, string reason = "User revoked other sessions", CancellationToken cancellationToken = default);

    /// <summary>
    /// Update session with new refresh token reference and expiry.
    /// </summary>
    Task UpdateRefreshTokenAsync(Guid sessionId, string refreshTokenRef, DateTimeOffset refreshTokenExpiresAt, DateTimeOffset accessTokenExpiresAt, CancellationToken cancellationToken = default);
}
