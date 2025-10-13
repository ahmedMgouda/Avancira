using Avancira.Domain.UserSessions;

namespace Avancira.BFF.Services;

/// <summary>
/// Manages user sessions across devices.
/// Handles creation, validation, activity tracking, and revocation.
/// Integrates database persistence with in-memory cache.
/// </summary>
public interface ISessionManagementService
{
    /// <summary>
    /// Create a new session after successful login.
    /// Returns session ID to include in claims.
    /// </summary>
    Task<UserSession> CreateSessionAsync(
        string userId,
        string deviceId,
        string? deviceName,
        string? userAgent,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Store refresh token reference ID after successful token exchange.
    /// Links session to auth server's refresh token.
    /// </summary>
    Task UpdateSessionRefreshTokenAsync(
        Guid sessionId,
        string refreshTokenReferenceId,
        DateTimeOffset tokenExpiresAt,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get session by ID with cache.
    /// </summary>
    Task<UserSession?> GetSessionAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all active sessions for a user.
    /// </summary>
    Task<IEnumerable<UserSession>> GetUserSessionsAsync(
        string userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Update last activity time.
    /// Called on each API request through reverse proxy.
    /// </summary>
    Task UpdateSessionActivityAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Revoke a single session (logout from device).
    /// </summary>
    Task RevokeSessionAsync(
        Guid sessionId,
        string reason = "User logout",
        bool notifyUser = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Revoke all sessions for a user (logout everywhere).
    /// Used on password change or security event.
    /// </summary>
    Task RevokeAllUserSessionsAsync(
        string userId,
        string reason = "Security event",
        bool notifyUser = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Revoke sessions based on security criteria.
    /// E.g., logout all sessions except current device.
    /// </summary>
    Task RevokeOtherSessionsAsync(
        string userId,
        Guid exceptSessionId,
        string reason = "User revoked other sessions",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if session is valid and active.
    /// Returns false if revoked, expired, or user disabled.
    /// </summary>
    Task<bool> IsSessionValidAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Clean up expired sessions (background job).
    /// Should run daily.
    /// </summary>
    Task CleanupExpiredSessionsAsync(
        CancellationToken cancellationToken = default);
}
