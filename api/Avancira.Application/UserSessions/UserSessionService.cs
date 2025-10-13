using Avancira.Application.Auth;
using Avancira.Application.Persistence;
using Avancira.Application.UserSessions.Dtos;
using Avancira.Domain.Common.Exceptions;
using Avancira.Domain.UserSessions;
using Mapster;
using Microsoft.Extensions.Logging;

namespace Avancira.Application.UserSessions.Services;

/// <summary>
/// Application-layer service responsible for user session management.
/// Handles persistence logic, business validation, and transformation.
/// </summary>
public class UserSessionService : IUserSessionService
{
    private readonly IRepository<UserSession> _sessionRepository;
    private readonly INetworkContextService _networkContextService;
    private readonly ILogger<UserSessionService> _logger;

    public UserSessionService(
        IRepository<UserSession> sessionRepository,
        INetworkContextService networkContextService,
        ILogger<UserSessionService> logger)
    {
        _sessionRepository = sessionRepository;
        _networkContextService = networkContextService;
        _logger = logger;
    }

    public async Task<UserSession> CreateAsync(UserSession session, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);

        if (string.IsNullOrWhiteSpace(session.UserId))
            throw new ArgumentException("UserId cannot be null or empty", nameof(session));

        if (string.IsNullOrWhiteSpace(session.DeviceId))
            throw new ArgumentException("DeviceId cannot be null or empty", nameof(session));

        session.Id = session.Id == Guid.Empty ? Guid.NewGuid() : session.Id;
        session.CreatedAt = session.CreatedAt == default ? DateTimeOffset.UtcNow : session.CreatedAt;
        session.LastActivityAt = session.LastActivityAt == default ? DateTimeOffset.UtcNow : session.LastActivityAt;
        session.Status = session.Status == default ? SessionStatus.Active : session.Status;

        await _sessionRepository.AddAsync(session, cancellationToken);

        _logger.LogInformation("Created session {SessionId} for user {UserId} on device {DeviceId}",
            session.Id, session.UserId, session.DeviceId);

        return session;
    }

    public async Task<UserSession?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Session ID cannot be empty", nameof(id));

        return await _sessionRepository.GetByIdAsync(id, cancellationToken);
    }

    public async Task<IEnumerable<UserSession>> GetUserSessionsAsync(string userId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("UserId cannot be null or empty", nameof(userId));

        return await _sessionRepository.ListAsync(new ActiveUserSessionsSpec(userId), cancellationToken);
    }

    public async Task UpdateAsync(UserSession session, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);

        if (session.Id == Guid.Empty)
            throw new ArgumentException("Session ID cannot be empty", nameof(session));

        var existing = await _sessionRepository.GetByIdAsync(session.Id, cancellationToken)
            ?? throw new AvanciraNotFoundException($"Session with ID '{session.Id}' not found.");

        await _sessionRepository.UpdateAsync(session, cancellationToken);
        _logger.LogDebug("Updated session {SessionId} for user {UserId}", session.Id, session.UserId);
    }

    public async Task<bool> RevokeAsync(Guid sessionId, string? reason = null, CancellationToken cancellationToken = default)
    {
        if (sessionId == Guid.Empty)
            throw new ArgumentException("Session ID cannot be empty", nameof(sessionId));

        var session = await _sessionRepository.GetByIdAsync(sessionId, cancellationToken);
        if (session is null)
        {
            _logger.LogWarning("Cannot revoke: session {SessionId} not found", sessionId);
            return false;
        }

        if (session.Status is SessionStatus.Revoked or SessionStatus.RevokedBySecurityEvent or SessionStatus.RevokedByTokenInvalidation or SessionStatus.Expired)
        {
            _logger.LogDebug("Session {SessionId} is already revoked or expired", sessionId);
            return false;
        }

        session.Status = SessionStatus.Revoked;
        session.RevokedAt = DateTimeOffset.UtcNow;
        session.RevocationReason = reason ?? "User logout";

        await _sessionRepository.UpdateAsync(session, cancellationToken);

        _logger.LogInformation("Revoked session {SessionId} for user {UserId}, reason: {Reason}",
            sessionId, session.UserId, reason);

        return true;
    }

    public async Task<int> RevokeAllAsync(string userId, Guid? excludeSessionId = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("UserId cannot be null or empty", nameof(userId));

        var sessions = await _sessionRepository.ListAsync(new ActiveUserSessionsSpec(userId), cancellationToken);

        var revokedCount = 0;
        foreach (var session in sessions)
        {
            if (excludeSessionId.HasValue && session.Id == excludeSessionId.Value)
                continue;

            if (session.Status != SessionStatus.Active)
                continue;

            session.Status = SessionStatus.Revoked;
            session.RevokedAt = DateTimeOffset.UtcNow;
            session.RevocationReason = "Bulk revocation";

            await _sessionRepository.UpdateAsync(session, cancellationToken);
            revokedCount++;
        }

        if (revokedCount > 0)
            _logger.LogInformation("Revoked {Count} sessions for user {UserId}", revokedCount, userId);

        return revokedCount;
    }

    public async Task<IEnumerable<UserSession>> GetExpiredSessionsAsync(DateTimeOffset beforeDate, CancellationToken cancellationToken = default)
    {
        return await _sessionRepository.ListAsync(new ExpiredSessionsSpec(beforeDate), cancellationToken);
    }

    public async Task<IReadOnlyList<DeviceSessionsDto>> GetActiveByUserGroupedByDeviceAsync(string userId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("UserId cannot be null or empty", nameof(userId));

        var sessions = await GetUserSessionsAsync(userId, cancellationToken);
        var sessionDtos = sessions.Adapt<List<UserSessionDto>>();

        var grouped = sessionDtos
            .GroupBy(s => s.DeviceId)
            .Select(group =>
            {
                var ordered = group.OrderByDescending(s => s.LastActivityAt).ToList();
                var first = ordered.First();

                return new DeviceSessionsDto(first.DeviceId, first.DeviceName, first.UserAgent, first.LastActivityAt, ordered);
            })
            .OrderByDescending(g => g.LastActivityAt)
            .ToList();

        return grouped;
    }

    public async Task<IEnumerable<UserSessionDto>> GetActiveByUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("UserId cannot be null or empty", nameof(userId));

        var sessions = await GetUserSessionsAsync(userId, cancellationToken);
        return sessions.Adapt<List<UserSessionDto>>();
    }
}
