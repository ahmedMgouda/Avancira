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
            "api" // Add your API scope
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

    /// <summary>
    /// Authorization endpoint - GET method
    /// User must be authenticated via Identity cookies before reaching this endpoint
    /// </summary>
    [HttpGet("authorize")]
    [HttpPost("authorize")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Authorize()
    {
        var request = HttpContext.GetOpenIddictServerRequest()
            ?? throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

        // ⭐ Check if user is authenticated via Identity cookies
        var result = await HttpContext.AuthenticateAsync(IdentityConstants.ApplicationScheme);
        
        if (!result.Succeeded || result.Principal == null)
        {
            // User not authenticated - redirect to login page
            var returnUrl = Request.Path + Request.QueryString;
            _logger.LogDebug("User not authenticated, redirecting to login: {ReturnUrl}", returnUrl);
            
            return Challenge(
                authenticationSchemes: IdentityConstants.ApplicationScheme,
                properties: new AuthenticationProperties
                {
                    RedirectUri = returnUrl
                });
        }

        // Get the authenticated user
        var user = await _signInManager.UserManager.GetUserAsync(result.Principal);
        if (user == null)
        {
            _logger.LogWarning("User principal exists but user not found in database");
            return Challenge(IdentityConstants.ApplicationScheme);
        }

        // Ensure user can still sign in (not locked out, etc.)
        if (!await _signInManager.CanSignInAsync(user))
        {
            _logger.LogWarning("User {UserId} cannot sign in", user.Id);
            
            return Forbid(
                authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                properties: new AuthenticationProperties(new Dictionary<string, string?>
                {
                    [OpenIddictServerAspNetCoreConstants.Properties.Error] = OpenIddictConstants.Errors.InvalidGrant,
                    [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "The user is not allowed to sign in."
                }));
        }

        // Create principal with OpenIddict-compatible claims
        var principal = await _signInManager.CreateUserPrincipalAsync(user);
        
        // Get or create device ID for session tracking
        _networkContextService.GetOrCreateDeviceId();

        // Validate and set requested scopes
        var requestedScopes = request.GetScopes();
        var allowedRequestedScopes = requestedScopes.Where(scope => AllowedScopes.Contains(scope)).ToList();
        
        if (!allowedRequestedScopes.Any())
        {
            _logger.LogWarning("No valid scopes requested. Requested: {Scopes}", string.Join(", ", requestedScopes));
            allowedRequestedScopes = new List<string> { OpenIddictConstants.Scopes.OpenId };
        }

        principal.SetScopes(allowedRequestedScopes);

        // Set destinations for claims
        AttachDestinations(principal);

        _logger.LogInformation(
            "Authorizing user {UserId} with scopes: {Scopes}",
            user.Id,
            string.Join(", ", allowedRequestedScopes));

        // Sign in with OpenIddict - this generates the authorization code
        return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    /// <summary>
    /// Token endpoint - exchanges authorization code for access/refresh tokens
    /// </summary>
    [HttpPost("token")]
    [IgnoreAntiforgeryToken]
    [Produces("application/json")]
    public async Task<IActionResult> Exchange()
    {
        var request = HttpContext.GetOpenIddictServerRequest()
            ?? throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

        // ⭐ Authorization Code Flow
        if (request.IsAuthorizationCodeGrantType())
        {
            // Retrieve the claims principal from the authorization code
            var result = await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            var principal = result.Principal
                ?? throw new InvalidOperationException("The authorization code principal cannot be retrieved.");

            // Validate the user still exists and can sign in
            var userId = principal.GetClaim(OpenIddictConstants.Claims.Subject);
            var user = await _signInManager.UserManager.FindByIdAsync(userId!);
            
            if (user == null || !await _signInManager.CanSignInAsync(user))
            {
                _logger.LogWarning("User {UserId} not found or cannot sign in during token exchange", userId);
                
                return Forbid(
                    authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                    properties: new AuthenticationProperties(new Dictionary<string, string?>
                    {
                        [OpenIddictServerAspNetCoreConstants.Properties.Error] = OpenIddictConstants.Errors.InvalidGrant,
                        [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "The user is no longer allowed to sign in."
                    }));
            }

            // Create or retrieve session
            await EnsureSessionCreatedAsync(principal, HttpContext.RequestAborted);
            
            // Set destinations for claims
            AttachDestinations(principal);

            _logger.LogInformation("Exchanging authorization code for tokens for user {UserId}", userId);

            return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        // ⭐ Refresh Token Flow
        if (request.IsRefreshTokenGrantType())
        {
            // Retrieve the claims principal from the refresh token
            var result = await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            var principal = result.Principal
                ?? throw new InvalidOperationException("The refresh token principal cannot be retrieved.");

            // Validate the user still exists and can sign in
            var userId = principal.GetClaim(OpenIddictConstants.Claims.Subject);
            var user = await _signInManager.UserManager.FindByIdAsync(userId!);
            
            if (user == null || !await _signInManager.CanSignInAsync(user))
            {
                _logger.LogWarning("User {UserId} not found or cannot sign in during token refresh", userId);
                
                return Forbid(
                    authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                    properties: new AuthenticationProperties(new Dictionary<string, string?>
                    {
                        [OpenIddictServerAspNetCoreConstants.Properties.Error] = OpenIddictConstants.Errors.InvalidGrant,
                        [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "The token is no longer valid."
                    }));
            }

            // Update session activity
            await UpdateSessionActivityAsync(principal, HttpContext.RequestAborted);
            
            // Set destinations for claims
            AttachDestinations(principal);

            _logger.LogInformation("Refreshing tokens for user {UserId}", userId);

            return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        _logger.LogWarning("Unsupported grant type received: {GrantType}", request.GrantType);
        
        return Forbid(
            authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
            properties: new AuthenticationProperties(new Dictionary<string, string?>
            {
                [OpenIddictServerAspNetCoreConstants.Properties.Error] = OpenIddictConstants.Errors.UnsupportedGrantType,
                [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "The specified grant type is not supported."
            }));
    }

    /// <summary>
    /// Revocation endpoint - revokes access and refresh tokens
    /// </summary>
    [HttpPost("revoke")]
    [HttpPost("revocation")] // Support both endpoints
    [IgnoreAntiforgeryToken]
    [Produces("application/json")]
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

        // Find the token by reference ID
        var token = await _tokenManager.FindByReferenceIdAsync(request.Token, cancellationToken);
        if (token is null)
        {
            _logger.LogDebug("Token not found for revocation: {Token}", request.Token);
            // Return success even if token not found (per RFC 7009)
            return Ok();
        }

        // Extract principal from token to get session info
        ClaimsPrincipal? principal = null;
        try
        {
            principal = await _tokenManager.GetPrincipalAsync(token, cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Failed to extract principal from token being revoked");
        }

        // Revoke the token
        await _tokenManager.TryRevokeAsync(token, cancellationToken);
        _logger.LogInformation("Token revoked successfully");

        // Revoke associated session if exists
        if (principal is not null)
        {
            var sessionIdClaim = principal.GetClaim(AuthConstants.Claims.SessionId);
            if (Guid.TryParse(sessionIdClaim, out var sessionId))
            {
                try
                {
                    await _sessionService.RevokeAsync(sessionId, "Token revoked by user", cancellationToken);
                    _logger.LogInformation("Session {SessionId} revoked", sessionId);
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, "Failed to revoke session {SessionId}", sessionId);
                }
            }
        }

        return Ok();
    }

    /// <summary>
    /// UserInfo endpoint - returns user information
    /// </summary>
    [Authorize(AuthenticationSchemes = OpenIddictServerAspNetCoreDefaults.AuthenticationScheme)]
    [HttpGet("userinfo")]
    [HttpPost("userinfo")]
    [Produces("application/json")]
    public async Task<IActionResult> Userinfo()
    {
        var userId = User.GetClaim(OpenIddictConstants.Claims.Subject);
        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning("No subject claim found in userinfo request");
            return Challenge(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        var user = await _signInManager.UserManager.FindByIdAsync(userId);
        if (user == null)
        {
            _logger.LogWarning("User {UserId} not found for userinfo request", userId);
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

        // Add roles if needed
        var roles = await _signInManager.UserManager.GetRolesAsync(user);
        if (roles.Any())
        {
            claims[OpenIddictConstants.Claims.Role] = roles;
        }

        return Ok(claims);
    }

    /// <summary>
    /// Creates a session for the user on first token exchange
    /// </summary>
    private async Task EnsureSessionCreatedAsync(ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        var userId = principal.GetClaim(OpenIddictConstants.Claims.Subject);
        var authorizationId = principal.GetAuthorizationId();

        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(authorizationId))
        {
            _logger.LogDebug("Skipping session creation: missing user or authorization");
            return;
        }

        if (!Guid.TryParse(authorizationId, out var authorizationGuid))
        {
            _logger.LogWarning("Authorization ID {AuthorizationId} is not a valid GUID", authorizationId);
            return;
        }

        // Check if session already exists
        var existingSessionId = principal.GetClaim(AuthConstants.Claims.SessionId);
        if (Guid.TryParse(existingSessionId, out _))
        {
            _logger.LogDebug("Session already exists for authorization {AuthorizationId}", authorizationId);
            return;
        }

        try
        {
            var session = await _sessionService.CreateAsync(
                new CreateUserSessionDto(userId, authorizationGuid),
                cancellationToken);

            principal.SetClaim(AuthConstants.Claims.SessionId, session.Id.ToString());
            _logger.LogInformation("Created session {SessionId} for user {UserId}", session.Id, userId);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to create session for user {UserId}", userId);
        }
    }

    /// <summary>
    /// Updates session activity on token refresh
    /// </summary>
    private async Task UpdateSessionActivityAsync(ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        var sessionIdClaim = principal.GetClaim(AuthConstants.Claims.SessionId);
        if (!Guid.TryParse(sessionIdClaim, out var sessionId))
        {
            _logger.LogDebug("No valid session ID found in refresh token");
            return;
        }

        try
        {
            await _sessionService.UpdateActivityAsync(sessionId, cancellationToken);
            _logger.LogDebug("Updated activity for session {SessionId}", sessionId);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to update activity for session {SessionId}", sessionId);
        }
    }

    /// <summary>
    /// Sets claim destinations (which tokens they appear in)
    /// </summary>
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

    /// <summary>
    /// Determines which tokens each claim should appear in
    /// </summary>
    private static IEnumerable<string> GetDestinations(Claim claim, ClaimsPrincipal principal)
    {
        switch (claim.Type)
        {
            // Subject always goes to access token
            case OpenIddictConstants.Claims.Subject:
                yield return OpenIddictConstants.Destinations.AccessToken;
                yield break;

            // Name claims go to access token and optionally id_token
            case OpenIddictConstants.Claims.Name:
            case OpenIddictConstants.Claims.GivenName:
            case OpenIddictConstants.Claims.FamilyName:
                yield return OpenIddictConstants.Destinations.AccessToken;
                if (principal.HasScope(OpenIddictConstants.Scopes.Profile))
                {
                    yield return OpenIddictConstants.Destinations.IdentityToken;
                }
                yield break;

            // Email claims go to access token and optionally id_token
            case OpenIddictConstants.Claims.Email:
            case OpenIddictConstants.Claims.EmailVerified:
                yield return OpenIddictConstants.Destinations.AccessToken;
                if (principal.HasScope(OpenIddictConstants.Scopes.Email))
                {
                    yield return OpenIddictConstants.Destinations.IdentityToken;
                }
                yield break;

            // Session ID goes to access token and refresh token
            case AuthConstants.Claims.SessionId:
                yield return OpenIddictConstants.Destinations.AccessToken;
                yield return OpenIddictConstants.Destinations.RefreshToken;
                yield break;

            // Roles only in access token
            case OpenIddictConstants.Claims.Role:
                yield return OpenIddictConstants.Destinations.AccessToken;
                yield break;

            // Audience always in access token
            case OpenIddictConstants.Claims.Audience:
                yield return OpenIddictConstants.Destinations.AccessToken;
                yield break;
        }

        // Default: include in access token if openid scope is present
        if (principal.HasScope(OpenIddictConstants.Scopes.OpenId))
        {
            yield return OpenIddictConstants.Destinations.AccessToken;
        }
    }
}
