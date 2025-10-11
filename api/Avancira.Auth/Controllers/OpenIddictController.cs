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
using Microsoft.AspNetCore;

namespace Avancira.Auth.Controllers;

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

    /// <summary>
    /// Authorization endpoint - GET and POST methods
    /// User must be authenticated via Identity cookies before reaching this endpoint
    /// </summary>
    [HttpGet("authorize")]
    [HttpPost("authorize")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Authorize()
    {
        var request = HttpContext.GetOpenIddictServerRequest();
        if (request == null)
        {
            _logger.LogError("OpenID Connect request cannot be retrieved");
            return BadRequest(new
            {
                error = OpenIddictConstants.Errors.InvalidRequest,
                error_description = "The OpenID Connect request cannot be retrieved."
            });
        }

        // Validate required client_id parameter
        if (string.IsNullOrWhiteSpace(request.ClientId))
        {
            _logger.LogWarning("Authorization request missing client_id");
            return BadRequest(new
            {
                error = OpenIddictConstants.Errors.InvalidRequest,
                error_description = "The client_id parameter is required."
            });
        }

        // Check if user is authenticated via Identity cookies
        var result = await HttpContext.AuthenticateAsync(IdentityConstants.ApplicationScheme);

        if (!result.Succeeded || result.Principal == null)
        {
            var returnUrl = Request.Path + Request.QueryString;
            _logger.LogDebug("User not authenticated, redirecting to login: {ReturnUrl}", returnUrl);

            return Challenge(
                authenticationSchemes: IdentityConstants.ApplicationScheme,
                properties: new AuthenticationProperties { RedirectUri = returnUrl });
        }

        // Get the authenticated user
        var user = await _signInManager.UserManager.GetUserAsync(result.Principal);
        if (user == null)
        {
            _logger.LogWarning("User principal exists but user not found in database");
            return Challenge(IdentityConstants.ApplicationScheme);
        }

        // Verify user can still sign in
        if (!await _signInManager.CanSignInAsync(user))
        {
            _logger.LogWarning("User {UserId} cannot sign in (locked out or disabled)", user.Id);
            return Forbid(
                authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                properties: CreateErrorProperties(
                    OpenIddictConstants.Errors.InvalidGrant,
                    "The user is not allowed to sign in."));
        }

        // Validate email confirmation if required
        if (!user.EmailConfirmed && _signInManager.Options.SignIn.RequireConfirmedEmail)
        {
            _logger.LogWarning("User {UserId} email not confirmed", user.Id);
            return Forbid(
                authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                properties: CreateErrorProperties(
                    OpenIddictConstants.Errors.InvalidGrant,
                    "Email confirmation is required."));
        }

        // Create principal with OpenIddict-compatible claims
        var principal = await _signInManager.CreateUserPrincipalAsync(user);
        EnsureRequiredClaims(principal, user);

        // Get or create device ID for session tracking
        _networkContextService.GetOrCreateDeviceId();

        // Validate and set requested scopes
        var requestedScopes = request.GetScopes().ToList();
        var allowedScopes = requestedScopes
            .Where(scope => AllowedScopes.Contains(scope))
            .ToList();

        // Default to openid scope if no valid scopes requested
        if (allowedScopes.Count == 0)
        {
            _logger.LogWarning(
                "No valid scopes requested by client {ClientId}. Requested: {RequestedScopes}",
                request.ClientId,
                string.Join(", ", requestedScopes));
            allowedScopes.Add(OpenIddictConstants.Scopes.OpenId);
        }

        principal.SetScopes(allowedScopes);

        // Set resources for API access
        if (allowedScopes.Contains("api"))
        {
            principal.SetResources("api-resource");
        }

        AttachDestinations(principal);

        _logger.LogInformation(
            "Authorizing user {UserId} for client {ClientId} with scopes: {Scopes}",
            user.Id,
            request.ClientId,
            string.Join(", ", allowedScopes));

        // SESSION LOGIC - COMMENTED OUT
        // Uncomment when session tracking is ready
        // _networkContextService.GetOrCreateDeviceId();
        // await EnsureSessionCreatedAsync(principal, HttpContext.RequestAborted);

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
        var request = HttpContext.GetOpenIddictServerRequest();
        if (request == null)
        {
            _logger.LogError("OpenID Connect request cannot be retrieved in token endpoint");
            return BadRequest(new
            {
                error = OpenIddictConstants.Errors.InvalidRequest,
                error_description = "The OpenID Connect request cannot be retrieved."
            });
        }

        if (request.IsAuthorizationCodeGrantType())
        {
            return await HandleAuthorizationCodeGrantAsync(request);
        }

        if (request.IsRefreshTokenGrantType())
        {
            return await HandleRefreshTokenGrantAsync(request);
        }

        _logger.LogWarning("Unsupported grant type: {GrantType}", request.GrantType);
        return Forbid(
            authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
            properties: CreateErrorProperties(
                OpenIddictConstants.Errors.UnsupportedGrantType,
                "The specified grant type is not supported."));
    }

    /// <summary>
    /// Revocation endpoint - revokes tokens
    /// </summary>
    [HttpPost("revoke")]
    [HttpPost("revocation")]
    [IgnoreAntiforgeryToken]
    [Produces("application/json")]
    public async Task<IActionResult> Revoke(CancellationToken cancellationToken)
    {
        var request = HttpContext.GetOpenIddictServerRequest();
        if (request == null)
        {
            _logger.LogError("OpenID Connect request cannot be retrieved in revocation endpoint");
            return BadRequest(new
            {
                error = OpenIddictConstants.Errors.InvalidRequest,
                error_description = "The OpenID Connect request cannot be retrieved."
            });
        }

        if (string.IsNullOrWhiteSpace(request.Token))
        {
            _logger.LogWarning("Revocation request missing token parameter");
            return BadRequest(new
            {
                error = OpenIddictConstants.Errors.InvalidRequest,
                error_description = "The token parameter is required."
            });
        }

        // Try to find token by reference ID first, then by ID
        var token = await _tokenManager.FindByReferenceIdAsync(request.Token, cancellationToken);
        token ??= await _tokenManager.FindByIdAsync(request.Token, cancellationToken);

        if (token == null)
        {
            _logger.LogDebug("Token not found for revocation (per RFC 7009, returning success)");
            return Ok();
        }

        // Attempt to extract principal from token for session cleanup
        //ClaimsPrincipal? principal = null;
        //try
        //{
        //    principal = await _tokenManager.GetPrincipalAsync(token, cancellationToken);
        //}
        //catch (Exception ex)
        //{
        //    _logger.LogWarning(ex, "Failed to extract principal from token during revocation");
        //}

        // Revoke the token
        await _tokenManager.TryRevokeAsync(token, cancellationToken);
        _logger.LogInformation("Token revoked successfully");

        // SESSION LOGIC - COMMENTED OUT
        // Uncomment when session tracking is ready
        // if (principal is not null)
        // {
        //     await RevokeSessionIfExistsAsync(principal, cancellationToken);
        // }

        return Ok();
    }

    /// <summary>
    /// UserInfo endpoint - returns user information based on granted scopes
    /// </summary>
    [Authorize(AuthenticationSchemes = OpenIddictServerAspNetCoreDefaults.AuthenticationScheme)]
    [HttpGet("userinfo")]
    [HttpPost("userinfo")]
    [Produces("application/json")]
    public async Task<IActionResult> Userinfo()
    {
        var userId = User.GetClaim(OpenIddictConstants.Claims.Subject);
        if (string.IsNullOrWhiteSpace(userId))
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

        // Only include profile claims if scope granted
        if (User.HasScope(OpenIddictConstants.Scopes.Profile))
        {
            claims[OpenIddictConstants.Claims.Name] = user.UserName ?? string.Empty;

            if (!string.IsNullOrEmpty(user.FirstName))
                claims[OpenIddictConstants.Claims.GivenName] = user.FirstName;

            if (!string.IsNullOrEmpty(user.LastName))
                claims[OpenIddictConstants.Claims.FamilyName] = user.LastName;
        }

        // Only include email claims if scope granted
        if (User.HasScope(OpenIddictConstants.Scopes.Email))
        {
            claims[OpenIddictConstants.Claims.Email] = user.Email ?? string.Empty;
            claims[OpenIddictConstants.Claims.EmailVerified] = user.EmailConfirmed;
        }

        // Add user roles
        var roles = await _signInManager.UserManager.GetRolesAsync(user);
        if (roles.Count > 0)
        {
            claims[OpenIddictConstants.Claims.Role] = roles.ToArray();
        }

        _logger.LogDebug("Userinfo returned for user {UserId}", userId);
        return Ok(claims);
    }

    #region Private Helper Methods

    /// <summary>
    /// Handles authorization code exchange for tokens
    /// </summary>
    private async Task<IActionResult> HandleAuthorizationCodeGrantAsync(OpenIddictRequest request)
    {
        var result = await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        var principal = result.Principal;

        if (principal == null)
        {
            _logger.LogError("Authorization code principal cannot be retrieved");
            return Forbid(
                authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                properties: CreateErrorProperties(
                    OpenIddictConstants.Errors.InvalidGrant,
                    "The authorization code is invalid."));
        }

        var userId = principal.GetClaim(OpenIddictConstants.Claims.Subject);
        var user = await _signInManager.UserManager.FindByIdAsync(userId!);

        if (user == null || !await _signInManager.CanSignInAsync(user))
        {
            _logger.LogWarning("User {UserId} not found or cannot sign in during code exchange", userId);
            return Forbid(
                authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                properties: CreateErrorProperties(
                    OpenIddictConstants.Errors.InvalidGrant,
                    "The user is no longer allowed to sign in."));
        }

        // SESSION LOGIC - COMMENTED OUT
        // Uncomment when session tracking is ready
        // await EnsureSessionCreatedAsync(principal, HttpContext.RequestAborted);

        await RefreshUserClaimsAsync(principal, user);
        AttachDestinations(principal);

        _logger.LogInformation("Code exchanged for tokens - User: {UserId}, Client: {ClientId}", userId, request.ClientId);
        return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    /// <summary>
    /// Handles refresh token grant
    /// </summary>
    private async Task<IActionResult> HandleRefreshTokenGrantAsync(OpenIddictRequest request)
    {
        var result = await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        var principal = result.Principal;

        if (principal == null)
        {
            _logger.LogError("Refresh token principal cannot be retrieved");
            return Forbid(
                authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                properties: CreateErrorProperties(
                    OpenIddictConstants.Errors.InvalidGrant,
                    "The refresh token is invalid."));
        }

        var userId = principal.GetClaim(OpenIddictConstants.Claims.Subject);
        var user = await _signInManager.UserManager.FindByIdAsync(userId!);

        if (user == null || !await _signInManager.CanSignInAsync(user))
        {
            _logger.LogWarning("User {UserId} not found or cannot sign in during refresh", userId);
            return Forbid(
                authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                properties: CreateErrorProperties(
                    OpenIddictConstants.Errors.InvalidGrant,
                    "The token is no longer valid."));
        }

        // SESSION LOGIC - COMMENTED OUT
        // Uncomment when session tracking is ready
        // var sessionIdClaim = principal.GetClaim(AuthConstants.Claims.SessionId);
        // if (Guid.TryParse(sessionIdClaim, out var sessionId))
        // {
        //     try
        //     {
        //         var session = await _sessionService.GetByIdAsync(sessionId, HttpContext.RequestAborted);
        //         if (session is null || !session.IsActive)
        //         {
        //             _logger.LogWarning("Session {SessionId} is invalid or inactive", sessionId);
        //             return Forbid(
        //                 authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
        //                 properties: CreateErrorProperties(
        //                     OpenIddictConstants.Errors.InvalidGrant,
        //                     "The session is no longer valid."));
        //         }
        //         await UpdateSessionActivityAsync(principal, HttpContext.RequestAborted);
        //     }
        //     catch (Exception ex)
        //     {
        //         _logger.LogError(ex, "Error validating session {SessionId}", sessionId);
        //     }
        // }

        await RefreshUserClaimsAsync(principal, user);
        AttachDestinations(principal);

        _logger.LogInformation("Tokens refreshed - User: {UserId}, Client: {ClientId}", userId, request.ClientId);
        return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    /// <summary>
    /// Ensures all required OpenIddict claims are present
    /// </summary>
    private void EnsureRequiredClaims(ClaimsPrincipal principal, IdentityUser user)
    {
        var identity = (ClaimsIdentity)principal.Identity!;

        if (!identity.HasClaim(c => c.Type == OpenIddictConstants.Claims.Subject))
        {
            identity.AddClaim(new Claim(OpenIddictConstants.Claims.Subject, user.Id));
        }

        if (!identity.HasClaim(c => c.Type == OpenIddictConstants.Claims.Email) && !string.IsNullOrEmpty(user.Email))
        {
            identity.AddClaim(new Claim(OpenIddictConstants.Claims.Email, user.Email));
        }

        if (!identity.HasClaim(c => c.Type == OpenIddictConstants.Claims.EmailVerified))
        {
            identity.AddClaim(new Claim(
                OpenIddictConstants.Claims.EmailVerified,
                user.EmailConfirmed.ToString().ToLowerInvariant(),
                ClaimValueTypes.Boolean));
        }
    }

    /// <summary>
    /// Refreshes user claims from database to ensure they're current
    /// </summary>
    private async Task RefreshUserClaimsAsync(ClaimsPrincipal principal, IdentityUser user)
    {
        var identity = (ClaimsIdentity)principal.Identity!;

        // Update email verified status
        var emailVerifiedClaim = identity.FindFirst(OpenIddictConstants.Claims.EmailVerified);
        if (emailVerifiedClaim != null)
        {
            identity.RemoveClaim(emailVerifiedClaim);
            identity.AddClaim(new Claim(
                OpenIddictConstants.Claims.EmailVerified,
                user.EmailConfirmed.ToString().ToLowerInvariant(),
                ClaimValueTypes.Boolean));
        }

        // Update roles
        var roleClaims = identity.FindAll(OpenIddictConstants.Claims.Role).ToList();
        foreach (var roleClaim in roleClaims)
        {
            identity.RemoveClaim(roleClaim);
        }

        var currentRoles = await _signInManager.UserManager.GetRolesAsync(user);
        foreach (var role in currentRoles)
        {
            identity.AddClaim(new Claim(OpenIddictConstants.Claims.Role, role));
        }
    }

    /// <summary>
    /// SESSION LOGIC - COMMENTED OUT
    /// Uncomment when session tracking is implemented
    /// </summary>
    // private async Task EnsureSessionCreatedAsync(ClaimsPrincipal principal, CancellationToken cancellationToken)
    // {
    //     var userId = principal.GetClaim(OpenIddictConstants.Claims.Subject);
    //     var authorizationId = principal.GetAuthorizationId();
    //
    //     if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(authorizationId))
    //     {
    //         _logger.LogDebug("Skipping session creation: missing user or authorization ID");
    //         return;
    //     }
    //
    //     if (!Guid.TryParse(authorizationId, out var authorizationGuid))
    //     {
    //         _logger.LogWarning("Authorization ID {AuthorizationId} is not a valid GUID", authorizationId);
    //         return;
    //     }
    //
    //     var existingSessionId = principal.GetClaim(AuthConstants.Claims.SessionId);
    //     if (Guid.TryParse(existingSessionId, out _))
    //     {
    //         _logger.LogDebug("Session already exists for authorization {AuthorizationId}", authorizationId);
    //         return;
    //     }
    //
    //     try
    //     {
    //         var session = await _sessionService.CreateAsync(
    //             new CreateUserSessionDto(userId, authorizationGuid),
    //             cancellationToken);
    //
    //         principal.SetClaim(AuthConstants.Claims.SessionId, session.Id.ToString());
    //         _logger.LogInformation("Created session {SessionId} for user {UserId}", session.Id, userId);
    //     }
    //     catch (Exception ex)
    //     {
    //         _logger.LogError(ex, "Failed to create session for user {UserId}", userId);
    //     }
    // }

    /// <summary>
    /// SESSION LOGIC - COMMENTED OUT
    /// Uncomment when session tracking is implemented
    /// </summary>
    // private async Task UpdateSessionActivityAsync(ClaimsPrincipal principal, CancellationToken cancellationToken)
    // {
    //     var sessionIdClaim = principal.GetClaim(AuthConstants.Claims.SessionId);
    //     if (!Guid.TryParse(sessionIdClaim, out var sessionId))
    //     {
    //         _logger.LogDebug("No valid session ID found in refresh token");
    //         return;
    //     }
    //
    //     try
    //     {
    //         await _sessionService.UpdateActivityAsync(sessionId, cancellationToken);
    //         _logger.LogDebug("Updated activity for session {SessionId}", sessionId);
    //     }
    //     catch (Exception ex)
    //     {
    //         _logger.LogError(ex, "Failed to update activity for session {SessionId}", sessionId);
    //     }
    // }

    /// <summary>
    /// SESSION LOGIC - COMMENTED OUT
    /// Uncomment when session tracking is implemented
    /// </summary>
    // private async Task RevokeSessionIfExistsAsync(ClaimsPrincipal principal, CancellationToken cancellationToken)
    // {
    //     var sessionIdClaim = principal.GetClaim(AuthConstants.Claims.SessionId);
    //     if (Guid.TryParse(sessionIdClaim, out var sessionId))
    //     {
    //         try
    //         {
    //             await _sessionService.RevokeAsync(sessionId, "Token revoked by user", cancellationToken);
    //             _logger.LogInformation("Session {SessionId} revoked", sessionId);
    //         }
    //         catch (Exception ex)
    //         {
    //             _logger.LogError(ex, "Failed to revoke session {SessionId}", sessionId);
    //         }
    //     }
    // }

    /// <summary>
    /// Sets claim destinations for inclusion in specific token types
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
    /// Determines which token types each claim should appear in
    /// </summary>
    private static IEnumerable<string> GetDestinations(Claim claim, ClaimsPrincipal principal)
    {
        return claim.Type switch
        {
            // Subject claim - always in access token and ID token if OpenId scope
            OpenIddictConstants.Claims.Subject =>
                principal.HasScope(OpenIddictConstants.Scopes.OpenId)
                    ? new[] { OpenIddictConstants.Destinations.AccessToken, OpenIddictConstants.Destinations.IdentityToken }
                    : new[] { OpenIddictConstants.Destinations.AccessToken },

            // Name claims - access token + ID token if Profile scope
            OpenIddictConstants.Claims.Name or
            OpenIddictConstants.Claims.GivenName or
            OpenIddictConstants.Claims.FamilyName =>
                principal.HasScope(OpenIddictConstants.Scopes.Profile)
                    ? new[] { OpenIddictConstants.Destinations.AccessToken, OpenIddictConstants.Destinations.IdentityToken }
                    : new[] { OpenIddictConstants.Destinations.AccessToken },

            // Email claims - access token + ID token if Email scope
            OpenIddictConstants.Claims.Email or
            OpenIddictConstants.Claims.EmailVerified =>
                principal.HasScope(OpenIddictConstants.Scopes.Email)
                    ? new[] { OpenIddictConstants.Destinations.AccessToken, OpenIddictConstants.Destinations.IdentityToken }
                    : new[] { OpenIddictConstants.Destinations.AccessToken },

            // Roles - access token only (not part of OIDC spec for ID token)
            OpenIddictConstants.Claims.Role => new[] { OpenIddictConstants.Destinations.AccessToken },

            // Audience - access token only
            OpenIddictConstants.Claims.Audience => new[] { OpenIddictConstants.Destinations.AccessToken },

            // Default: access token only
            _ => new[] { OpenIddictConstants.Destinations.AccessToken }
        };
    }

    /// <summary>
    /// Helper method to create standardized error properties
    /// </summary>
    private static AuthenticationProperties CreateErrorProperties(string error, string errorDescription)
    {
        return new AuthenticationProperties(new Dictionary<string, string?>
        {
            [OpenIddictServerAspNetCoreConstants.Properties.Error] = error,
            [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = errorDescription
        });
    }

    #endregion
}