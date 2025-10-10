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
            OpenIddictConstants.Scopes.OfflineAccess,
            AuthConstants.Scopes.OfflineAccess
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
    [IgnoreAntiforgeryToken]
    public Task<IActionResult> AuthorizeGet() => HandleAuthorizeRequestAsync();

    [HttpPost("authorize")]
    [IgnoreAntiforgeryToken]
    public Task<IActionResult> AuthorizePost() => HandleAuthorizeRequestAsync();

    [HttpPost("token")]
    [IgnoreAntiforgeryToken]
    [Produces("application/json")]
    public async Task<IActionResult> Exchange()
    {
        var request = HttpContext.GetOpenIddictServerRequest()
            ?? throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

        if (request.IsAuthorizationCodeGrantType())
        {
            var result = await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            var principal = result.Principal
                ?? throw new InvalidOperationException("The authorization code principal cannot be retrieved.");

            var validationResult = await EnsureUserStillAllowedAsync(
                principal,
                "authorization code exchange");

            if (validationResult is not null)
            {
                return validationResult;
            }

            await EnsureSessionCreatedAsync(principal, HttpContext.RequestAborted);
            AttachDestinations(principal);

            return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        if (request.IsRefreshTokenGrantType())
        {
            var result = await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            var principal = result.Principal
                ?? throw new InvalidOperationException("The refresh token principal cannot be retrieved.");

            var validationResult = await EnsureUserStillAllowedAsync(
                principal,
                "refresh token exchange");

            if (validationResult is not null)
            {
                return validationResult;
            }

            await UpdateSessionActivityAsync(principal, HttpContext.RequestAborted);
            AttachDestinations(principal);

            return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        _logger.LogWarning("Unsupported grant type received: {GrantType}", request.GrantType);
        return ForbidWithError(
            OpenIddictConstants.Errors.UnsupportedGrantType,
            "The specified grant type is not supported.");
    }

    [HttpPost("revoke")]
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
        _logger.LogInformation("Token revoked successfully.");

        if (principal is not null)
        {
            var sessionIdClaim = principal.GetClaim(AuthConstants.Claims.SessionId);
            if (Guid.TryParse(sessionIdClaim, out var sessionId))
            {
                try
                {
                    await _sessionService.RevokeAsync(sessionId, "Token revoked via revocation endpoint", cancellationToken);
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, "Failed to revoke session {SessionId} associated with token revocation.", sessionId);
                }
            }
        }

        return Ok();
    }

    [Authorize(AuthenticationSchemes = OpenIddictServerAspNetCoreDefaults.AuthenticationScheme)]
    [HttpGet("userinfo")]
    [HttpPost("userinfo")]
    [Produces("application/json")]
    public async Task<IActionResult> Userinfo()
    {
        var userId = User.GetClaim(OpenIddictConstants.Claims.Subject);
        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning("Userinfo request did not include a subject claim.");
            return Challenge(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        var user = await _signInManager.UserManager.FindByIdAsync(userId);
        if (user is null)
        {
            _logger.LogWarning("User {UserId} referenced in userinfo request no longer exists.", userId);
            return Challenge(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        var claims = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            [OpenIddictConstants.Claims.Subject] = user.Id
        };

        if (User.HasScope(OpenIddictConstants.Scopes.Profile))
        {
            claims[OpenIddictConstants.Claims.Name] = user.UserName ?? string.Empty;
            claims[OpenIddictConstants.Claims.GivenName] = user.FirstName ?? string.Empty;
            claims[OpenIddictConstants.Claims.FamilyName] = user.LastName ?? string.Empty;
        }

        if (User.HasScope(OpenIddictConstants.Scopes.Email))
        {
            claims[OpenIddictConstants.Claims.Email] = user.Email ?? string.Empty;
            claims[OpenIddictConstants.Claims.EmailVerified] = user.EmailConfirmed;
        }

        var roles = await _signInManager.UserManager.GetRolesAsync(user);
        if (roles.Any())
        {
            claims[OpenIddictConstants.Claims.Role] = roles;
        }

        return Ok(claims);
    }

    private async Task<IActionResult> HandleAuthorizeRequestAsync()
    {
        var request = HttpContext.GetOpenIddictServerRequest()
            ?? throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

        var authenticationResult = await HttpContext.AuthenticateAsync(IdentityConstants.ApplicationScheme);

        if (!authenticationResult.Succeeded || authenticationResult.Principal is null)
        {
            var redirectUri = Request.Path + Request.QueryString;
            _logger.LogDebug(
                "Authorization request requires login, redirecting to {RedirectUri}.",
                redirectUri);

            return Challenge(
                IdentityConstants.ApplicationScheme,
                new AuthenticationProperties
                {
                    RedirectUri = redirectUri
                });
        }

        var user = await _signInManager.UserManager.GetUserAsync(authenticationResult.Principal);
        if (user is null)
        {
            _logger.LogWarning("Authenticated principal could not be resolved to a user.");
            await HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);

            return Challenge(
                IdentityConstants.ApplicationScheme,
                new AuthenticationProperties
                {
                    RedirectUri = Request.Path + Request.QueryString
                });
        }

        if (!await _signInManager.CanSignInAsync(user))
        {
            _logger.LogWarning("User {UserId} is not allowed to sign in during authorization.", user.Id);
            return ForbidWithError(
                OpenIddictConstants.Errors.InvalidGrant,
                "The user is not allowed to sign in.");
        }

        var principal = await _signInManager.CreateUserPrincipalAsync(user);

        _networkContextService.GetOrCreateDeviceId();

        var requestedScopes = request.GetScopes();
        var grantedScopes = requestedScopes
            .Where(scope => AllowedScopes.Contains(scope))
            .ToHashSet(StringComparer.Ordinal);

        if (grantedScopes.Count == 0)
        {
            grantedScopes.Add(OpenIddictConstants.Scopes.OpenId);
        }

        principal.SetScopes(grantedScopes);
        AttachDestinations(principal);

        _logger.LogInformation(
            "Authorizing user {UserId} with scopes: {Scopes}.",
            user.Id,
            string.Join(", ", grantedScopes.OrderBy(scope => scope, StringComparer.Ordinal)));

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
            _logger.LogDebug("Updated activity timestamp for session {SessionId}.", sessionId);
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
            case OpenIddictConstants.Claims.Subject:
                yield return OpenIddictConstants.Destinations.AccessToken;
                yield break;

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
            case OpenIddictConstants.Claims.EmailVerified:
                yield return OpenIddictConstants.Destinations.AccessToken;

                if (principal.HasScope(OpenIddictConstants.Scopes.Email))
                {
                    yield return OpenIddictConstants.Destinations.IdentityToken;
                }

                yield break;

            case OpenIddictConstants.Claims.Role:
                yield return OpenIddictConstants.Destinations.AccessToken;
                yield break;

            case OpenIddictConstants.Claims.Audience:
                yield return OpenIddictConstants.Destinations.AccessToken;
                yield break;

            case AuthConstants.Claims.SessionId:
                yield return OpenIddictConstants.Destinations.AccessToken;
                yield return OpenIddictConstants.Destinations.RefreshToken;
                yield break;
        }

        if (principal.HasScope(OpenIddictConstants.Scopes.OpenId))
        {
            yield return OpenIddictConstants.Destinations.AccessToken;
        }
    }

    private async Task<IActionResult?> EnsureUserStillAllowedAsync(
        ClaimsPrincipal principal,
        string operation)
    {
        var userId = principal.GetClaim(OpenIddictConstants.Claims.Subject);
        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning(
                "Principal received for {Operation} did not contain a subject identifier.",
                operation);

            return ForbidWithError(
                OpenIddictConstants.Errors.InvalidGrant,
                "The token is no longer valid.");
        }

        var user = await _signInManager.UserManager.FindByIdAsync(userId);
        if (user is null)
        {
            _logger.LogWarning(
                "User {UserId} referenced by token during {Operation} no longer exists.",
                userId);

            return ForbidWithError(
                OpenIddictConstants.Errors.InvalidGrant,
                "The user is no longer allowed to sign in.");
        }

        if (!await _signInManager.CanSignInAsync(user))
        {
            _logger.LogWarning(
                "User {UserId} is not allowed to sign in during {Operation}.",
                userId);

            return ForbidWithError(
                OpenIddictConstants.Errors.InvalidGrant,
                "The user is no longer allowed to sign in.");
        }

        return null;
    }

    private ForbidResult ForbidWithError(string error, string description)
    {
        var properties = new AuthenticationProperties(new Dictionary<string, string?>
        {
            [OpenIddictServerAspNetCoreConstants.Properties.Error] = error,
            [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = description
        });

        return Forbid(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme, properties);
    }
}
