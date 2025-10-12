using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avancira.Application.Persistence;
using Avancira.Application.UserSessions.Dtos;
using Avancira.Application.UserSessions.Services;
using Avancira.Domain.Common.Exceptions;
using Avancira.Domain.UserSessions;
using Mapster;
using Microsoft.Extensions.Logging;

namespace Avancira.Application.UserSessions;

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

    public async Task<UserSessionDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var session = await _sessionRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new AvanciraNotFoundException($"Session with ID '{id}' not found.");

        return session.Adapt<UserSessionDto>();
    }

    public async Task<IEnumerable<UserSessionDto>> GetActiveByUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        var sessions = await _sessionRepository.ListAsync(new ActiveUserSessionsSpec(userId), cancellationToken);
        return sessions.Adapt<List<UserSessionDto>>();
    }

    public async Task<IReadOnlyList<DeviceSessionsDto>> GetActiveByUserGroupedByDeviceAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var sessions = await _sessionRepository.ListAsync(new ActiveUserSessionsSpec(userId), cancellationToken);
        var sessionDtos = sessions.Adapt<List<UserSessionDto>>();

        var groupedSessions = sessionDtos
            .GroupBy(session => session.DeviceId)
            .Select(group =>
            {
                var ordered = group
                    .OrderByDescending(s => s.LastActivityAt)
                    .ToList();

                var first = ordered[0];

                return new DeviceSessionsDto(
                    first.DeviceId,
                    first.DeviceName,
                    first.UserAgent,
                    first.LastActivityAt,
                    ordered);
            })
            .OrderByDescending(group => group.LastActivityAt)
            .ToList();

        return groupedSessions;
    }

    public async Task<UserSessionDto> CreateAsync(CreateUserSessionDto dto, CancellationToken cancellationToken = default)
    {
        if (dto is null)
        {
            throw new ArgumentNullException(nameof(dto));
        }

        var session = new UserSession
        {
            Id = Guid.NewGuid(),
            UserId = dto.UserId,
            DeviceId = string.IsNullOrWhiteSpace(dto.DeviceId)
                ? _networkContextService.GetOrCreateDeviceId()
                : dto.DeviceId!,
            DeviceName = dto.DeviceName,
            UserAgent = dto.UserAgent,
            IpAddress = dto.IpAddress ?? _networkContextService.GetIpAddress(),
            RefreshTokenReferenceId = dto.RefreshTokenReferenceId,
            TokenExpiresAt = dto.TokenExpiresAt,
            CreatedAt = DateTimeOffset.UtcNow,
            LastActivityAt = DateTimeOffset.UtcNow,
            Status = SessionStatus.Active
        };

        await _sessionRepository.AddAsync(session, cancellationToken);

        _logger.LogInformation(
            "Created session {SessionId} for user {UserId} on device {DeviceId}",
            session.Id,
            session.UserId,
            session.DeviceId);

        return session.Adapt<UserSessionDto>();
    }

    public async Task<UserSessionDto> UpdateActivityAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var session = await _sessionRepository.GetByIdAsync(sessionId, cancellationToken)
            ?? throw new AvanciraNotFoundException($"Session with ID '{sessionId}' not found.");

        session.LastActivityAt = DateTimeOffset.UtcNow;

        await _sessionRepository.UpdateAsync(session, cancellationToken);

        return session.Adapt<UserSessionDto>();
    }

    public async Task<bool> RevokeAsync(Guid sessionId, string? reason = null, CancellationToken cancellationToken = default)
    {
        var session = await _sessionRepository.GetByIdAsync(sessionId, cancellationToken)
            ?? throw new AvanciraNotFoundException($"Session with ID '{sessionId}' not found.");

        if (session.Status is SessionStatus.Revoked or SessionStatus.RevokedBySecurityEvent or SessionStatus.RevokedByTokenInvalidation)
        {
            _logger.LogDebug("Session {SessionId} is already revoked.", sessionId);
            return false;
        }

        session.Status = SessionStatus.Revoked;
        session.RevokedAt = DateTimeOffset.UtcNow;
        session.RevocationReason = reason;

        await _sessionRepository.UpdateAsync(session, cancellationToken);

        _logger.LogInformation("Revoked session {SessionId} for user {UserId}", sessionId, session.UserId);

        return true;
    }

    public async Task<int> RevokeAllAsync(string userId, Guid? excludeSessionId = null, CancellationToken cancellationToken = default)
    {
        var sessions = await _sessionRepository.ListAsync(new ActiveUserSessionsSpec(userId), cancellationToken);

        int revokedCount = 0;
        foreach (var session in sessions)
        {
            if (excludeSessionId.HasValue && session.Id == excludeSessionId.Value)
            {
                continue;
            }

            session.Status = SessionStatus.Revoked;
            session.RevokedAt = DateTimeOffset.UtcNow;
            session.RevocationReason = "Bulk revocation";

            await _sessionRepository.UpdateAsync(session, cancellationToken);
            revokedCount++;
        }

        if (revokedCount > 0)
        {
            _logger.LogInformation("Revoked {Count} sessions for user {UserId}", revokedCount, userId);
        }

        return revokedCount;
    }
}
