using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Avancira.Application.UserSessions;
using Avancira.Application.UserSessions.Dtos;
using Avancira.Application.UserSessions.Services;
using Avancira.Infrastructure.Auth;
using IdentityUser = Avancira.Infrastructure.Identity.Users.User;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;

namespace Avancira.API.Controllers;

[AllowAnonymous]
[Route("connect")]
public sealed class OpenIddictController : Controller
{
    private static readonly HashSet<string> AllowedScopes =
        new(StringComparer.Ordinal)
        {
            OpenIddictConstants.Scopes.OpenId,
            OpenIddictConstants.Scopes.Profile,
            OpenIddictConstants.Scopes.Email,
            OpenIddictConstants.Scopes.OfflineAccess
        };

    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly IUserSessionService _sessionService;
    private readonly INetworkContextService _networkContextService;
    private readonly IOpenIddictTokenManager _tokenManager;
    private readonly ILogger<OpenIddictController> _logger;

    public OpenIddictController(
        SignInManager<IdentityUser> signInManager,
        IUserSessionService sessionService,
        INetworkContextService networkContextService,
        IOpenIddictTokenManager tokenManager,
        ILogger<OpenIddictController> logger)
    {
        _signInManager = signInManager;
        _sessionService = sessionService;
        _networkContextService = networkContextService;
        _tokenManager = tokenManager;
        _logger = logger;
    }

    [HttpGet("authorize")]
    [Authorize(AuthenticationSchemes = IdentityConstants.ApplicationScheme)]
    public Task<IActionResult> AuthorizeGet() => HandleAuthorizeRequestAsync();

    [HttpPost("authorize")]
    [Authorize(AuthenticationSchemes = IdentityConstants.ApplicationScheme)]
    [IgnoreAntiforgeryToken]
    public Task<IActionResult> AuthorizePost() => HandleAuthorizeRequestAsync();

    [HttpPost("token")]
    public async Task<IActionResult> Exchange()
    {
        var request = HttpContext.GetOpenIddictServerRequest()
            ?? throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

        if (request.IsAuthorizationCodeGrantType())
        {
            var result = await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            var principal = result.Principal
                ?? throw new InvalidOperationException("The authorization code principal cannot be retrieved.");

            await EnsureSessionCreatedAsync(principal, HttpContext.RequestAborted);
            AttachDestinations(principal);

            return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        if (request.IsRefreshTokenGrantType())
        {
            var result = await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            var principal = result.Principal
                ?? throw new InvalidOperationException("The refresh token principal cannot be retrieved.");

            await UpdateSessionActivityAsync(principal, HttpContext.RequestAborted);
            AttachDestinations(principal);

            return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        _logger.LogWarning("Unsupported grant type received: {GrantType}", request.GrantType);
        return Forbid(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    [HttpPost("revocation")]
    public async Task<IActionResult> Revoke(CancellationToken cancellationToken)
    {
        var request = HttpContext.GetOpenIddictServerRequest()
            ?? throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

        if (string.IsNullOrEmpty(request.Token))
        {
            return BadRequest(new
            {
                error = OpenIddictConstants.Errors.InvalidRequest,
                error_description = "The token to revoke must be provided."
            });
        }

        var token = await _tokenManager.FindByReferenceIdAsync(request.Token, cancellationToken);
        if (token is null)
        {
            return Ok();
        }

        ClaimsPrincipal? principal = null;
        try
        {
            principal = await _tokenManager.GetPrincipalAsync(token, cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Failed to extract principal from token being revoked.");
        }

        await _tokenManager.TryRevokeAsync(token, cancellationToken);

        if (principal is not null)
        {
            var sessionIdClaim = principal.GetClaim(AuthConstants.Claims.SessionId);
            if (Guid.TryParse(sessionIdClaim, out var sessionId))
            {
                try
                {
                    await _sessionService.RevokeAsync(sessionId, "Token revoked", cancellationToken);
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, "Failed to revoke session {SessionId} associated with token revocation.", sessionId);
                }
            }
        }

        return Ok();
    }

    private async Task<IActionResult> HandleAuthorizeRequestAsync()
    {
        var request = HttpContext.GetOpenIddictServerRequest()
            ?? throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

        var user = await _signInManager.UserManager.GetUserAsync(User)
            ?? throw new InvalidOperationException("The current user cannot be resolved.");

        var principal = await _signInManager.CreateUserPrincipalAsync(user);
        _networkContextService.GetOrCreateDeviceId();

        principal.SetScopes(request.GetScopes().Where(scope => AllowedScopes.Contains(scope)));
        AttachDestinations(principal);

        return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    private async Task EnsureSessionCreatedAsync(ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        var userId = principal.GetClaim(OpenIddictConstants.Claims.Subject);
        var authorizationId = principal.GetAuthorizationId();

        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(authorizationId))
        {
            _logger.LogDebug("Skipping session creation because user or authorization is missing.");
            return;
        }

        if (!Guid.TryParse(authorizationId, out var authorizationGuid))
        {
            _logger.LogWarning("Authorization identifier {AuthorizationId} is not a valid GUID.", authorizationId);
            return;
        }

        var existingSessionId = principal.GetClaim(AuthConstants.Claims.SessionId);
        if (Guid.TryParse(existingSessionId, out _))
        {
            _logger.LogDebug("Session already associated with authorization {AuthorizationId}.", authorizationId);
            return;
        }

        try
        {
            var session = await _sessionService.CreateAsync(
                new CreateUserSessionDto(userId, authorizationGuid),
                cancellationToken);

            principal.SetClaim(AuthConstants.Claims.SessionId, session.Id.ToString());
            _logger.LogInformation("Created session {SessionId} for user {UserId}.", session.Id, userId);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to create session for user {UserId}.", userId);
        }
    }

    private async Task UpdateSessionActivityAsync(ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        var sessionIdClaim = principal.GetClaim(AuthConstants.Claims.SessionId);
        if (!Guid.TryParse(sessionIdClaim, out var sessionId))
        {
            _logger.LogDebug("Refresh request did not contain a valid session identifier.");
            return;
        }

        try
        {
            await _sessionService.UpdateActivityAsync(sessionId, cancellationToken);
            _logger.LogInformation("Updated activity timestamp for session {SessionId}.", sessionId);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to update activity for session {SessionId}.", sessionId);
        }
    }

    private static void AttachDestinations(ClaimsPrincipal principal)
    {
        foreach (var claim in principal.Claims)
        {
            var destinations = GetDestinations(claim, principal);
            if (destinations.Any())
            {
                claim.SetDestinations(destinations);
            }
        }
    }

    private static IEnumerable<string> GetDestinations(Claim claim, ClaimsPrincipal principal)
    {
        switch (claim.Type)
        {
            case OpenIddictConstants.Claims.Name:
            case OpenIddictConstants.Claims.GivenName:
            case OpenIddictConstants.Claims.FamilyName:
                yield return OpenIddictConstants.Destinations.AccessToken;

                if (principal.HasScope(OpenIddictConstants.Scopes.Profile))
                {
                    yield return OpenIddictConstants.Destinations.IdentityToken;
                }

                yield break;

            case OpenIddictConstants.Claims.Email:
                yield return OpenIddictConstants.Destinations.AccessToken;

                if (principal.HasScope(OpenIddictConstants.Scopes.Email))
                {
                    yield return OpenIddictConstants.Destinations.IdentityToken;
                }

                yield break;

            case AuthConstants.Claims.SessionId:
                yield return OpenIddictConstants.Destinations.AccessToken;
                yield return OpenIddictConstants.Destinations.RefreshToken;
                yield break;
        }

        if (claim.Type is OpenIddictConstants.Claims.Subject or OpenIddictConstants.Claims.Audience)
        {
            yield return OpenIddictConstants.Destinations.AccessToken;
            yield break;
        }

        if (principal.HasScope(OpenIddictConstants.Scopes.OpenId))
        {
            yield return OpenIddictConstants.Destinations.AccessToken;
        }
    }
}
