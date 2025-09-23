using Avancira.Application.UserSessions;
using Avancira.Application.UserSessions.Dtos;
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
    private readonly ILogger<SessionSignInHandler> _logger;

    public SessionSignInHandler(
        IUserSessionService sessionService,
        ILogger<SessionSignInHandler> logger)
    {
        _sessionService = sessionService;
        _logger = logger;
    }

    public async ValueTask HandleAsync(ProcessSignInContext context)
    {
        if (context.Principal is null)
        {
            _logger.LogWarning("No principal found during sign-in, skipping session creation.");
            return;
        }

        var userId = context.Principal.GetClaim(OpenIddictConstants.Claims.Subject);
        var authorizationId = context.Principal.GetAuthorizationId();

        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(authorizationId))
        {
            _logger.LogWarning("Missing userId or authorizationId claim, skipping session creation.");
            return;
        }

        try
        {
            var dto = new CreateUserSessionDto(
                userId,
                Guid.Parse(authorizationId));

            var session = await _sessionService.CreateAsync(dto);

            // Attach the session ID as a claim so it flows into tokens
            context.Principal.SetClaim(AuthConstants.Claims.SessionId, session.Id.ToString());

            _logger.LogInformation("User session {SessionId} created for user {UserId}", session.Id, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create user session for user {UserId}", userId);
        }
    }
}
