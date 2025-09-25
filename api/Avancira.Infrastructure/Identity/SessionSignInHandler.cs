using Avancira.Application.UserSessions;
using Avancira.Application.UserSessions.Dtos;
using Avancira.Application.UserSessions.Services;
using Avancira.Infrastructure.Auth;
using Microsoft.Extensions.Logging;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using static OpenIddict.Server.OpenIddictServerEvents;

namespace Avancira.Infrastructure.UserSessions.Handlers;

/// <summary>
/// Creates a persistent UserSession record whenever a sign-in is processed.
/// </summary>
public sealed class SessionSignInHandler : IOpenIddictServerHandler<ProcessSignInContext>
{
    private readonly IUserSessionService _sessionService;
    private readonly INetworkContextService _networkService;
    private readonly ILogger<SessionSignInHandler> _logger;

    public SessionSignInHandler(
        IUserSessionService sessionService,
        INetworkContextService networkService,
        ILogger<SessionSignInHandler> logger)
    {
        _sessionService = sessionService;
        _networkService = networkService;
        _logger = logger;
    }

    public async ValueTask HandleAsync(ProcessSignInContext context)
    {
        if (context.Principal is null)
            return;

        var userId = context.Principal.GetClaim(OpenIddictConstants.Claims.Subject);
        var authorizationId = context.Principal.GetAuthorizationId();

        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(authorizationId))
            return;

        try
        {
            var grantType = context.Request?.GrantType;

            if (string.IsNullOrEmpty(grantType))
            {
                // Ensure device_id cookie exists at interactive login
                _networkService.GetOrCreateDeviceId();
                return;
            }

            if (grantType == OpenIddictConstants.GrantTypes.AuthorizationCode)
            {
                // First token exchange → create session
                var dto = new CreateUserSessionDto(userId, Guid.Parse(authorizationId));
                var session = await _sessionService.CreateAsync(dto, context.CancellationToken);

                context.Principal.SetClaim(AuthConstants.Claims.SessionId, session.Id.ToString());
                _logger.LogInformation("Created new session {SessionId} for user {UserId}", session.Id, userId);
            }
            else if (grantType == OpenIddictConstants.GrantTypes.RefreshToken)
            {
                // Refresh token → update session activity
                var sid = context.Principal.GetClaim(AuthConstants.Claims.SessionId);
                if (Guid.TryParse(sid, out var sessionId))
                {
                    await _sessionService.UpdateActivityAsync(sessionId, context.CancellationToken);
                    _logger.LogInformation("Updated activity for session {SessionId} (user {UserId})", sessionId, userId);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle session for user {UserId}", userId);
        }
    }
}

