using Avancira.Application.Persistence;
using Avancira.Domain.UserSessions;
using Avancira.Domain.Common.Exceptions;
using Microsoft.Extensions.Logging;
using Avancira.Application.UserSessions.Dtos;
using Mapster;

namespace Avancira.Application.UserSessions.Services;

/// <summary>
/// Application-layer service for user session management.
/// Single source of truth for all session operations.
/// NO caching here - cache belongs in infrastructure/BFF layer.
/// </summary>
public class UserSessionService : IUserSessionService
{
    private readonly IRepository<UserSession> _sessionRepository;
    private readonly ILogger<UserSessionService> _logger;

    public UserSessionService(
        IRepository<UserSession> sessionRepository,
        ILogger<UserSessionService> logger)
    {
        _sessionRepository = sessionRepository;
        _logger = logger;
    }

    public async Task<UserSession> CreateAsync(UserSession session, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentException.ThrowIfNullOrWhiteSpace(session.UserId, nameof(session.UserId));
        ArgumentException.ThrowIfNullOrWhiteSpace(session.DeviceId, nameof(session.DeviceId));

        // Initialize only if not set
        session.Id = session.Id == Guid.Empty ? Guid.NewGuid() : session.Id;
        var now = DateTimeOffset.UtcNow;
        session.CreatedAt = session.CreatedAt == default ? now : session.CreatedAt;
        session.LastActivityAt = session.LastActivityAt == default ? now : session.LastActivityAt;
        session.Status = session.Status == default ? SessionStatus.Active : session.Status;

        await _sessionRepository.AddAsync(session, cancellationToken);

        _logger.LogInformation(
            "Session created: {SessionId} for user {UserId} on device {DeviceId}",
            session.Id, session.UserId, session.DeviceId);

        return session;
    }

    public async Task<UserSession?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Session ID cannot be empty", nameof(id));

        return await _sessionRepository.GetByIdAsync(id, cancellationToken);
    }

    public async Task<IEnumerable<UserSession>> GetUserSessionsAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId, nameof(userId));
        return await _sessionRepository.ListAsync(new ActiveUserSessionsSpec(userId), cancellationToken);
    }

    public async Task UpdateAsync(UserSession session, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);

        if (session.Id == Guid.Empty)
            throw new ArgumentException("Session ID cannot be empty", nameof(session));

        var existing = await _sessionRepository.GetByIdAsync(session.Id, cancellationToken)
            ?? throw new AvanciraNotFoundException($"Session '{session.Id}' not found");

        await _sessionRepository.UpdateAsync(session, cancellationToken);
        _logger.LogDebug("Session updated: {SessionId} for user {UserId}", session.Id, session.UserId);
    }

    public async Task<bool> RevokeAsync(
        Guid sessionId,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        if (sessionId == Guid.Empty)
            throw new ArgumentException("Session ID cannot be empty", nameof(sessionId));

        var session = await _sessionRepository.GetByIdAsync(sessionId, cancellationToken);
        if (session is null)
        {
            _logger.LogWarning("Cannot revoke: session {SessionId} not found", sessionId);
            return false;
        }

        // Already in terminal state
        if (session.Status is SessionStatus.Revoked or SessionStatus.RevokedBySecurityEvent
            or SessionStatus.RevokedByTokenInvalidation or SessionStatus.Expired)
        {
            _logger.LogDebug("Session {SessionId} already revoked/expired", sessionId);
            return false;
        }

        session.Status = SessionStatus.Revoked;
        session.RevokedAt = DateTimeOffset.UtcNow;
        session.RevocationReason = reason ?? "User logout";

        await _sessionRepository.UpdateAsync(session, cancellationToken);
        _logger.LogInformation("Session revoked: {SessionId}, reason: {Reason}", sessionId, reason);

        return true;
    }

    public async Task<int> RevokeAllAsync(
        string userId,
        Guid? excludeSessionId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId, nameof(userId));

        var sessions = await _sessionRepository.ListAsync(
            new ActiveUserSessionsSpec(userId),
            cancellationToken);

        var revokedCount = 0;
        var now = DateTimeOffset.UtcNow;

        foreach (var session in sessions)
        {
            if (excludeSessionId.HasValue && session.Id == excludeSessionId.Value)
                continue;

            if (session.Status != SessionStatus.Active)
                continue;

            session.Status = SessionStatus.Revoked;
            session.RevokedAt = now;
            session.RevocationReason = "Bulk revocation";

            await _sessionRepository.UpdateAsync(session, cancellationToken);
            revokedCount++;
        }

        if (revokedCount > 0)
            _logger.LogInformation("Revoked {Count} sessions for user {UserId}", revokedCount, userId);

        return revokedCount;
    }

    public async Task<IEnumerable<UserSession>> GetExpiredSessionsAsync(
        DateTimeOffset beforeDate,
        CancellationToken cancellationToken = default)
    {
        return await _sessionRepository.ListAsync(
            new ExpiredSessionsSpec(beforeDate),
            cancellationToken);
    }

    public async Task<IReadOnlyList<DeviceSessionsDto>> GetActiveByUserGroupedByDeviceAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId, nameof(userId));

        var sessions = await GetUserSessionsAsync(userId, cancellationToken);
        var sessionDtos = sessions.Adapt<List<UserSessionDto>>();

        var grouped = sessionDtos
            .GroupBy(s => s.DeviceId)
            .Select(group =>
            {
                var ordered = group.OrderByDescending(s => s.LastActivityAt).ToList();
                return new DeviceSessionsDto(
                    ordered.First().DeviceId,
                    ordered.First().DeviceName,
                    ordered.First().UserAgent,
                    ordered.First().LastActivityAt,
                    ordered);
            })
            .OrderByDescending(g => g.LastActivityAt)
            .ToList();

        return grouped;
    }

    public async Task<IEnumerable<UserSessionDto>> GetActiveByUserAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId, nameof(userId));

        var sessions = await GetUserSessionsAsync(userId, cancellationToken);
        return sessions.Adapt<List<UserSessionDto>>();
    }

    /// <summary>
    /// Update session activity. This is called frequently and WILL hit the database.
    /// BFF layer should implement lazy-write pattern via caching.
    /// </summary>
    public async Task UpdateActivityAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        if (sessionId == Guid.Empty)
            throw new ArgumentException("Session ID cannot be empty", nameof(sessionId));

        var session = await _sessionRepository.GetByIdAsync(sessionId, cancellationToken);
        if (session is null)
            return;

        session.LastActivityAt = DateTimeOffset.UtcNow;
        await _sessionRepository.UpdateAsync(session, cancellationToken);
    }

    /// <summary>
    /// Mark all active sessions for a user as revoked
    /// </summary>
    public async Task RevokeAllUserSessionsAsync(
        string userId,
        string reason = "Security event",
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId, nameof(userId));
        await RevokeAllAsync(userId, excludeSessionId: null, cancellationToken);
    }
}