using Avancira.Application.Persistence;
using Avancira.Application.UserSessions.Dtos;
using Avancira.Domain.Common.Exceptions;
using Avancira.Domain.UserSessions;
using Mapster;

namespace Avancira.Application.UserSessions;

public class UserSessionService : IUserSessionService
{
    private readonly IRepository<UserSession> _sessionRepository;
    private readonly ISessionMetadataCollectionService _metadataService;

    // Absolute session lifetime (e.g. 90 days)
    private static readonly TimeSpan AbsoluteSessionLifetime = TimeSpan.FromDays(90);

    public UserSessionService(
        IRepository<UserSession> sessionRepository,
        ISessionMetadataCollectionService metadataService)
    {
        _sessionRepository = sessionRepository;
        _metadataService = metadataService;
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
        return sessions.Adapt<IEnumerable<UserSessionDto>>();
    }

    public async Task<UserSessionDto> CreateAsync(CreateUserSessionDto dto, CancellationToken cancellationToken = default)
    {
        if (dto is null) throw new ArgumentNullException(nameof(dto));

        var metadata = await _metadataService.CollectAsync(cancellationToken);

        var session = UserSession.Create(dto.UserId, dto.AuthorizationId, metadata, AbsoluteSessionLifetime);

        await _sessionRepository.AddAsync(session, cancellationToken);

        return session.Adapt<UserSessionDto>();
    }

    public async Task<UserSessionDto> UpdateActivityAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var session = await _sessionRepository.GetByIdAsync(sessionId, cancellationToken)
            ?? throw new AvanciraNotFoundException($"Session with ID '{sessionId}' not found.");

        session.UpdateActivity();
        await _sessionRepository.UpdateAsync(session, cancellationToken);

        return session.Adapt<UserSessionDto>();
    }

    public async Task<bool> RevokeAsync(Guid sessionId, string? reason = null, CancellationToken cancellationToken = default)
    {
        var session = await _sessionRepository.GetByIdAsync(sessionId, cancellationToken)
            ?? throw new AvanciraNotFoundException($"Session with ID '{sessionId}' not found.");

        session.Revoke(reason);
        await _sessionRepository.UpdateAsync(session, cancellationToken);

        return true;
    }

    public async Task<int> RevokeAllAsync(string userId, Guid? excludeSessionId = null, CancellationToken cancellationToken = default)
    {
        var sessions = await _sessionRepository.ListAsync(new ActiveUserSessionsSpec(userId), cancellationToken);

        int revokedCount = 0;
        foreach (var session in sessions)
        {
            if (excludeSessionId.HasValue && session.Id == excludeSessionId.Value)
                continue;

            session.Revoke("Bulk revocation");
            await _sessionRepository.UpdateAsync(session, cancellationToken);
            revokedCount++;
        }

        return revokedCount;
    }
}
