using Avancira.Application.Persistence;
using Avancira.Application.UserSessions.Dtos;
using Avancira.Domain.Common.Exceptions;
using Avancira.Domain.UserSessions;
using Mapster;
using Microsoft.Extensions.Logging;
using OpenIddict.Abstractions;

namespace Avancira.Application.UserSessions;

public class UserSessionService : IUserSessionService
{
    private readonly IRepository<UserSession> _sessionRepository;
    private readonly ISessionMetadataCollectionService _metadataService;
    private readonly IOpenIddictAuthorizationManager _authorizationManager;
    private readonly IOpenIddictTokenManager _tokenManager;
    private readonly ILogger<UserSessionService> _logger;

    // Absolute session lifetime (e.g. 90 days)
    private static readonly TimeSpan AbsoluteSessionLifetime = TimeSpan.FromDays(90);

    public UserSessionService(
        IRepository<UserSession> sessionRepository,
        ISessionMetadataCollectionService metadataService,
        IOpenIddictAuthorizationManager authorizationManager,
        IOpenIddictTokenManager tokenManager,
        ILogger<UserSessionService> logger)
    {
        _sessionRepository = sessionRepository;
        _metadataService = metadataService;
        _authorizationManager = authorizationManager;
        _tokenManager = tokenManager;
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

        if (!await TryDisableAuthorizationAsync(session.AuthorizationId, cancellationToken))
        {
            _logger.LogWarning(
                "Skipping revocation for session {SessionId} because its authorization could not be disabled.",
                sessionId);
            return false;
        }

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

            if (!await TryDisableAuthorizationAsync(session.AuthorizationId, cancellationToken))
            {
                _logger.LogWarning(
                    "Failed to disable authorization for session {SessionId}; leaving it active.",
                    session.Id);
                continue;
            }

            session.Revoke("Bulk revocation");
            await _sessionRepository.UpdateAsync(session, cancellationToken);
            revokedCount++;
        }

        return revokedCount;
    }

    private async Task<bool> TryDisableAuthorizationAsync(Guid authorizationId, CancellationToken cancellationToken)
    {
        var identifier = authorizationId.ToString();

        try
        {
            await foreach (var token in _tokenManager.FindByAuthorizationIdAsync(identifier, cancellationToken))
            {
                try
                {
                    var revoked = await _tokenManager.TryRevokeAsync(token, cancellationToken);
                    if (!revoked)
                    {
                        _logger.LogDebug(
                            "Token linked to authorization {AuthorizationId} was already revoked.",
                            authorizationId);
                    }

                    await _tokenManager.DeleteAsync(token, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Failed to revoke token linked to authorization {AuthorizationId}.",
                        authorizationId);
                    return false;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enumerate tokens for authorization {AuthorizationId}.", authorizationId);
            return false;
        }

        object? authorization;
        try
        {
            authorization = await _authorizationManager.FindByIdAsync(identifier, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load authorization {AuthorizationId}.", authorizationId);
            return false;
        }

        if (authorization is null)
        {
            _logger.LogDebug(
                "No authorization found for identifier {AuthorizationId}; assuming it is already removed.",
                authorizationId);
            return true;
        }

        try
        {
            if (!await _authorizationManager.TryRevokeAsync(authorization, cancellationToken))
            {
                _logger.LogDebug(
                    "Authorization {AuthorizationId} was already revoked.",
                    authorizationId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to revoke authorization {AuthorizationId}.", authorizationId);
            return false;
        }

        try
        {
            await _authorizationManager.DeleteAsync(authorization, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete authorization {AuthorizationId}.", authorizationId);
            return false;
        }

        return true;
    }
}
