using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avancira.Application.UserSessions;
using Avancira.BFF.Services.TokenManagement;
using Avancira.Domain.UserSessions;
using Avancira.Infrastructure.Caching;
using Avancira.Infrastructure.Caching.SessionCache;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Avancira.BFF.Services;

/// <summary>
/// Production-grade session management with database persistence and in-memory caching.
/// Thread-safe and optimized for concurrent requests.
/// </summary>
public class SessionManagementService : ISessionManagementService
{
    private readonly IUserSessionService _dbSessionService;
    private readonly IMemoryCache _memoryCache;
    private readonly ITokenManagementService _tokenManagement;
    private readonly ILogger<SessionManagementService> _logger;

    private const int SessionCacheDurationMinutes = 30;

    public SessionManagementService(
        IUserSessionService dbSessionService,
        IMemoryCache memoryCache,
        ITokenManagementService tokenManagement,
        ILogger<SessionManagementService> logger)
    {
        _dbSessionService = dbSessionService;
        _memoryCache = memoryCache;
        _tokenManagement = tokenManagement;
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

        var session = new UserSession
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            DeviceId = deviceId,
            DeviceName = deviceName,
            UserAgent = userAgent,
            IpAddress = ipAddress,
            Status = SessionStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow,
            LastActivityAt = DateTimeOffset.UtcNow
        };

        await _dbSessionService.CreateAsync(session, cancellationToken);

        _logger.LogInformation(
            "Session created: {SessionId} for user {UserId} on device {DeviceId}",
            session.Id,
            userId,
            deviceId);

        await CacheSessionAsync(session);
        return session;
    }

    public async Task UpdateSessionRefreshTokenAsync(
        Guid sessionId,
        string refreshTokenReferenceId,
        DateTimeOffset tokenExpiresAt,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(refreshTokenReferenceId);

        var session = await _dbSessionService.GetByIdAsync(sessionId, cancellationToken);
        if (session is null)
        {
            _logger.LogWarning("Cannot update refresh token: session {SessionId} not found", sessionId);
            throw new InvalidOperationException($"Session {sessionId} not found");
        }

        session.RefreshTokenReferenceId = refreshTokenReferenceId;
        session.TokenExpiresAt = tokenExpiresAt;

        await _dbSessionService.UpdateAsync(session, cancellationToken);

        _logger.LogInformation(
            "Refresh token updated for session {SessionId}, expires at {ExpiresAt}",
            sessionId,
            tokenExpiresAt);

        InvalidateSessionCache(sessionId);
    }

    public async Task<UserSession?> GetSessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var cacheKey = CacheKeys.Session(sessionId);

        if (_memoryCache.TryGetValue(cacheKey, out SessionTokenInfo? cachedInfo))
        {
            _logger.LogDebug("Session cache hit for {SessionId}", sessionId);
            return await _dbSessionService.GetByIdAsync(sessionId, cancellationToken);
        }

        var session = await _dbSessionService.GetByIdAsync(sessionId, cancellationToken);
        if (session is not null)
            await CacheSessionAsync(session);

        return session;
    }

    public async Task<IEnumerable<UserSession>> GetUserSessionsAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        return await _dbSessionService.GetUserSessionsAsync(userId, cancellationToken);
    }

    public async Task UpdateSessionActivityAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var cacheKey = CacheKeys.Session(sessionId);

        if (_memoryCache.TryGetValue(cacheKey, out SessionTokenInfo? cachedInfo))
        {
            cachedInfo!.LastActivityAt = DateTimeOffset.UtcNow;

            if (cachedInfo.IsStale(TimeSpan.FromMinutes(30)))
            {
                var dbSession = await _dbSessionService.GetByIdAsync(sessionId, cancellationToken);
                if (dbSession is not null)
                {
                    dbSession.LastActivityAt = DateTimeOffset.UtcNow;
                    await _dbSessionService.UpdateAsync(dbSession, cancellationToken);
                    _logger.LogDebug("Session activity persisted: {SessionId}", sessionId);
                }

                cachedInfo.LastActivityAt = DateTimeOffset.UtcNow;
            }

            _memoryCache.Set(cacheKey, cachedInfo, TimeSpan.FromMinutes(SessionCacheDurationMinutes));
        }
        else
        {
            var dbSession = await _dbSessionService.GetByIdAsync(sessionId, cancellationToken);
            if (dbSession is not null)
            {
                dbSession.LastActivityAt = DateTimeOffset.UtcNow;
                await _dbSessionService.UpdateAsync(dbSession, cancellationToken);
                await CacheSessionAsync(dbSession);
            }
        }
    }

    public async Task RevokeSessionAsync(
        Guid sessionId,
        string reason = "User logout",
        bool notifyUser = false,
        CancellationToken cancellationToken = default)
    {
        var session = await _dbSessionService.GetByIdAsync(sessionId, cancellationToken);
        if (session is null)
        {
            _logger.LogWarning("Cannot revoke: session {SessionId} not found", sessionId);
            return;
        }

        session.Status = SessionStatus.Revoked;
        session.RevokedAt = DateTimeOffset.UtcNow;
        session.RevocationReason = reason;
        session.RequiresUserNotification = notifyUser;

        await _dbSessionService.UpdateAsync(session, cancellationToken);

        _logger.LogInformation(
            "Session revoked: {SessionId} for user {UserId}, reason: {Reason}",
            sessionId, session.UserId, reason);

        InvalidateSessionCache(sessionId);
        await _tokenManagement.InvalidateSessionTokenAsync(session.UserId, sessionId, cancellationToken);
        await _tokenManagement.MarkSessionAsRevokedAsync(sessionId, cancellationToken);

        if (notifyUser)
            _logger.LogInformation("Session revocation notification required for user {UserId}", session.UserId);
    }

    public async Task RevokeAllUserSessionsAsync(
        string userId,
        string reason = "Security event",
        bool notifyUser = true,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        var sessions = await _dbSessionService.GetUserSessionsAsync(userId, cancellationToken);
        foreach (var session in sessions.Where(s => s.Status == SessionStatus.Active))
            await RevokeSessionAsync(session.Id, reason, notifyUser, cancellationToken);

        _logger.LogWarning(
            "All sessions revoked for user {UserId} ({Count} sessions), reason: {Reason}",
            userId, sessions.Count(), reason);
    }

    public async Task RevokeOtherSessionsAsync(
        string userId,
        Guid exceptSessionId,
        string reason = "User revoked other sessions",
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        var sessions = await _dbSessionService.GetUserSessionsAsync(userId, cancellationToken);
        foreach (var session in sessions.Where(s => s.Id != exceptSessionId && s.Status == SessionStatus.Active))
            await RevokeSessionAsync(session.Id, reason, false, cancellationToken);

        _logger.LogInformation(
            "Other sessions revoked for user {UserId} (kept {KeptSessionId}), total affected: {Count}",
            userId, exceptSessionId, sessions.Count() - 1);
    }

    public async Task<bool> IsSessionValidAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var session = await GetSessionAsync(sessionId, cancellationToken);
        if (session is null || session.Status != SessionStatus.Active)
            return false;

        if (session.TokenExpiresAt.HasValue && session.TokenExpiresAt <= DateTimeOffset.UtcNow)
            return false;

        return true;
    }

    public async Task CleanupExpiredSessionsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting session cleanup");

        var expiredSessions = await _dbSessionService.GetExpiredSessionsAsync(
            DateTimeOffset.UtcNow.AddDays(-30),
            cancellationToken);

        var count = 0;
        foreach (var session in expiredSessions)
        {
            if (session.Status != SessionStatus.Revoked && session.Status != SessionStatus.Expired)
            {
                session.Status = SessionStatus.Expired;
                session.RevocationReason = "Automatic cleanup - token expired";
                await _dbSessionService.UpdateAsync(session, cancellationToken);
                count++;
            }

            InvalidateSessionCache(session.Id);
        }

        _logger.LogInformation("Session cleanup completed: {Count} sessions marked as expired", count);
    }

    private Task CacheSessionAsync(UserSession session)
    {
        var sessionInfo = new SessionTokenInfo
        {
            SessionId = session.Id,
            UserId = session.UserId,
            Status = session.Status,
            RefreshTokenReferenceId = session.RefreshTokenReferenceId,
            RefreshTokenExpiresAt = session.TokenExpiresAt,
            LastActivityAt = session.LastActivityAt,
            CreatedAt = session.CreatedAt
        };

        var cacheKey = CacheKeys.Session(session.Id);
        _memoryCache.Set(cacheKey, sessionInfo, TimeSpan.FromMinutes(SessionCacheDurationMinutes));
        return Task.CompletedTask;
    }

    private void InvalidateSessionCache(Guid sessionId)
    {
        var cacheKey = CacheKeys.Session(sessionId);
        _memoryCache.Remove(cacheKey);
    }
}
