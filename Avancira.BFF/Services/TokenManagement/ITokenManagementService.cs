namespace Avancira.BFF.Services;

/// <summary>
/// Manages access token caching and validation
/// Provides fast access to current tokens without hitting auth server repeatedly
/// </summary>
public interface ITokenManagementService
{
    /// <summary>
    /// Get or retrieve access token for current user session
    /// First checks in-memory cache, then fetches from auth server if expired
    /// </summary>
    Task<AccessTokenResult> GetAccessTokenAsync(
        string userId,
        Guid sessionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cache a freshly acquired access token
    /// Called after token refresh from auth server
    /// </summary>
    Task CacheAccessTokenAsync(
        string userId,
        Guid sessionId,
        string token,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove all cached tokens for a user
    /// Called on logout or password change
    /// </summary>
    Task InvalidateUserTokensAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove cached token for specific session
    /// Called when session is revoked
    /// </summary>
    Task InvalidateSessionTokenAsync(string userId, Guid sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if session is active and not revoked
    /// Validates against revoked sessions list
    /// </summary>
    Task<bool> IsSessionValidAsync(Guid sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Mark session as revoked in cache (prevents race conditions)
    /// </summary>
    Task MarkSessionAsRevokedAsync(Guid sessionId, CancellationToken cancellationToken = default);
}
