using Avancira.Application.UserSessions;
using Avancira.BFF.Services.SessionManagement;
using Avancira.Domain.UserSessions;
using Avancira.Infrastructure.Caching;
using Microsoft.Extensions.Caching.Memory;

namespace Avancira.BFF.Services;

/// <summary>
/// Manages user sessions in the BFF layer with short-term caching.
/// Reduces DB lookups and coordinates with token management.
/// </summary>
public class SessionCacheService : ISessionCacheService
{
    private readonly IUserSessionService _appSessionService;
    private readonly ITokenManagementService _tokenManagement;
    private readonly IMemoryCache _cache;
    private readonly ILogger<SessionCacheService> _logger;

    private const int CacheDurationMinutes = 30;
    private const int ActivityUpdateThresholdMinutes = 30;

    public SessionCacheService(
        IUserSessionService appSessionService,
        ITokenManagementService tokenManagement,
        IMemoryCache memoryCache,
        ILogger<SessionCacheService> logger)
    {
        _appSessionService = appSessionService;
        _tokenManagement = tokenManagement;
        _cache = memoryCache;
        _logger = logger;
    }

    public async Task<UserSession> CreateSessionAsync(
        string userId,
        string deviceId,
        string? deviceName,
        string? userAgent,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentException.ThrowIfNullOrWhiteSpace(deviceId);

        var now = DateTimeOffset.UtcNow;
        var session = new UserSession
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            DeviceId = deviceId,
            DeviceName = deviceName,
            UserAgent = userAgent,
            IpAddress = ipAddress,
            Status = SessionStatus.Active,
            CreatedAt = now,
            LastActivityAt = now,
            RefreshTokenExpiresAt = now.AddDays(7)
        };

        await _appSessionService.CreateAsync(session, cancellationToken);
        CacheSession(session);

        _logger.LogInformation("Created session {SessionId} for user {UserId} ({DeviceId})", session.Id, userId, deviceId);
        return session;
    }

    public async Task<UserSession?> GetSessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        if (sessionId == Guid.Empty)
            throw new ArgumentException("Session ID cannot be empty", nameof(sessionId));

        var session = await _appSessionService.GetByIdAsync(sessionId, cancellationToken);
        if (session is not null)
            CacheSession(session);

        return session;
    }

    public SessionCacheInfo? GetCachedSessionInfo(Guid sessionId)
    {
        _cache.TryGetValue(CacheKeys.Session(sessionId), out SessionCacheInfo? info);
        return info;
    }

    public async Task UpdateActivityLazyAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        if (sessionId == Guid.Empty)
            return;

        var cacheKey = CacheKeys.Session(sessionId);
        var now = DateTimeOffset.UtcNow;

        if (_cache.TryGetValue(cacheKey, out SessionCacheInfo? cached))
        {
            var delta = now - cached!.LastActivityAt;
            if (delta.TotalMinutes >= ActivityUpdateThresholdMinutes)
            {
                await _appSessionService.UpdateActivityAsync(sessionId, cancellationToken);
                _logger.LogDebug("Activity persisted for session {SessionId}", sessionId);
            }

            cached.LastActivityAt = now;
            _cache.Set(cacheKey, cached, TimeSpan.FromMinutes(CacheDurationMinutes));
        }
        else
        {
            var session = await _appSessionService.GetByIdAsync(sessionId, cancellationToken);
            if (session is not null)
            {
                await _appSessionService.UpdateActivityAsync(sessionId, cancellationToken);
                CacheSession(session);
            }
        }
    }

    public async Task<bool> IsSessionValidAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var cached = GetCachedSessionInfo(sessionId);
        if (cached is not null)
            return cached.IsValid();

        var session = await GetSessionAsync(sessionId, cancellationToken);
        return session is not null && IsSessionValid(session);
    }

    public async Task RevokeSessionAsync(Guid sessionId, string reason = "User logout", CancellationToken cancellationToken = default)
    {
        if (sessionId == Guid.Empty)
            throw new ArgumentException("Session ID cannot be empty", nameof(sessionId));

        var session = await _appSessionService.GetByIdAsync(sessionId, cancellationToken);
        if (session is null)
            return;

        await _appSessionService.RevokeAsync(sessionId, reason, cancellationToken);
        InvalidateCache(sessionId);

        await _tokenManagement.InvalidateSessionTokenAsync(session.UserId, sessionId, cancellationToken);
        await _tokenManagement.MarkSessionAsRevokedAsync(sessionId, cancellationToken);

        _logger.LogInformation("Revoked session {SessionId}", sessionId);
    }

    public async Task<IEnumerable<UserSession>> GetUserSessionsAsync(string userId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        return await _appSessionService.GetUserSessionsAsync(userId, cancellationToken);
    }

    public async Task RevokeOtherSessionsAsync(string userId, Guid exceptSessionId, string reason = "User revoked other sessions", CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        var sessions = await _appSessionService.GetUserSessionsAsync(userId, cancellationToken);
        var toRevoke = sessions
            .Where(s => s.Id != exceptSessionId && s.Status == SessionStatus.Active)
            .ToList();

        foreach (var session in toRevoke)
            await RevokeSessionAsync(session.Id, reason, cancellationToken);

        _logger.LogInformation("Revoked {Count} other sessions for user {UserId}", toRevoke.Count, userId);
    }

    public async Task UpdateRefreshTokenAsync(Guid sessionId, string refreshTokenRef, DateTimeOffset refreshTokenExpiresAt, DateTimeOffset accessTokenExpiresAt, CancellationToken cancellationToken = default)
    {
        var session = await _appSessionService.GetByIdAsync(sessionId, cancellationToken);
        if (session is null)
            throw new InvalidOperationException($"Session {sessionId} not found");

        session.RefreshTokenReferenceId = refreshTokenRef;
        session.RefreshTokenExpiresAt = refreshTokenExpiresAt;

        await _appSessionService.UpdateAsync(session, cancellationToken);
        InvalidateCache(sessionId);

        _logger.LogInformation("Updated refresh token for session {SessionId}", sessionId);
    }

    #region Private Helpers

    private void CacheSession(UserSession session)
    {
        var info = new SessionCacheInfo
        {
            SessionId = session.Id,
            UserId = session.UserId,
            Status = session.Status,
            RefreshTokenExpiresAt = session.RefreshTokenExpiresAt,
            LastActivityAt = session.LastActivityAt,
            CreatedAt = session.CreatedAt
        };

        _cache.Set(CacheKeys.Session(session.Id), info, TimeSpan.FromMinutes(CacheDurationMinutes));
    }

    private void InvalidateCache(Guid sessionId)
    {
        _cache.Remove(CacheKeys.Session(sessionId));
        _logger.LogDebug("Invalidated cache for session {SessionId}", sessionId);
    }

    private static bool IsSessionValid(UserSession session) =>
        session.Status == SessionStatus.Active &&
        (!session.RefreshTokenExpiresAt.HasValue || session.RefreshTokenExpiresAt > DateTimeOffset.UtcNow);

    #endregion
}
