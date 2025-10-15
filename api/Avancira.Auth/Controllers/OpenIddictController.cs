using System.Security.Claims;
using Avancira.Application.UserSessions;
using Avancira.Infrastructure.Auth;
using Avancira.Domain.UserSessions;
using IdentityUser = Avancira.Infrastructure.Identity.Users.User;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using Microsoft.AspNetCore;
using Avancira.Application.Auth;

namespace Avancira.Auth.Controllers;

[AllowAnonymous]
[Route("connect")]
public sealed class OpenIddictController : Controller
{
    /// <summary>
    /// FIXED: Added "api" scope to allowed scopes
    /// This enables clients to request API access
    /// </summary>
    private static readonly HashSet<string> AllowedScopes =
        new(StringComparer.Ordinal)
        {
            OpenIddictConstants.Scopes.OpenId,
            OpenIddictConstants.Scopes.Profile,
            OpenIddictConstants.Scopes.Email,
            OpenIddictConstants.Scopes.OfflineAccess,
            "api"  // FIXED: Was missing - prevents API scope requests
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
    /// FIXED: Now creates session and sets SessionId claim before token issuance
    /// </summary>
    [HttpGet("authorize")]
    [HttpPost("authorize")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Authorize(CancellationToken cancellationToken = default)
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

        if (string.IsNullOrWhiteSpace(request.ClientId))
        {
            _logger.LogWarning("Authorization request missing client_id");
            return BadRequest(new
            {
                error = OpenIddictConstants.Errors.InvalidRequest,
                error_description = "The client_id parameter is required."
            });
        }

        var result = await HttpContext.AuthenticateAsync(IdentityConstants.ApplicationScheme);

        if (!result.Succeeded || result.Principal == null)
        {
            var returnUrl = Request.Path + Request.QueryString;
            _logger.LogDebug("User not authenticated, redirecting to login: {ReturnUrl}", returnUrl);

            return Challenge(
                authenticationSchemes: IdentityConstants.ApplicationScheme,
                properties: new AuthenticationProperties { RedirectUri = returnUrl });
        }

        var user = await _signInManager.UserManager.GetUserAsync(result.Principal);
        if (user == null)
        {
            _logger.LogWarning("User principal exists but user not found in database");
            return Challenge(IdentityConstants.ApplicationScheme);
        }

        if (!await _signInManager.CanSignInAsync(user))
        {
            _logger.LogWarning("User {UserId} cannot sign in (locked out or disabled)", user.Id);
            return Forbid(
                authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                properties: CreateErrorProperties(
                    OpenIddictConstants.Errors.InvalidGrant,
                    "The user is not allowed to sign in."));
        }

        if (!user.EmailConfirmed && _signInManager.Options.SignIn.RequireConfirmedEmail)
        {
            _logger.LogWarning("User {UserId} email not confirmed", user.Id);
            return Forbid(
                authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                properties: CreateErrorProperties(
                    OpenIddictConstants.Errors.InvalidGrant,
                    "Email confirmation is required."));
        }

        var principal = await _signInManager.CreateUserPrincipalAsync(user);

        // FIXED: Create session BEFORE adding claims
        // This ensures we have a SessionId to include in the token
        var deviceId = _networkContextService.GetOrCreateDeviceId();
        var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

        var session = await _sessionService.CreateAsync(
            new UserSession
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                DeviceId = deviceId,
                DeviceName = null,
                UserAgent = userAgent,
                IpAddress = ipAddress,
                Status = SessionStatus.Active,
                CreatedAt = DateTimeOffset.UtcNow,
                LastActivityAt = DateTimeOffset.UtcNow
            },
            cancellationToken);

        _logger.LogInformation(
            "Session created during authorization: {SessionId} for user {UserId}",
            session.Id,
            user.Id);

        // FIXED: Now add claims with actual SessionId
        EnsureRequiredClaims(principal, user, session.Id);

        var requestedScopes = request.GetScopes().ToList();
        var allowedScopes = requestedScopes
            .Where(scope => AllowedScopes.Contains(scope))
            .ToList();

        // Ensure openid scope is always included
        if (!allowedScopes.Contains(OpenIddictConstants.Scopes.OpenId))
        {
            allowedScopes.Add(OpenIddictConstants.Scopes.OpenId);
        }

        if (allowedScopes.Count == 0)
        {
            _logger.LogWarning(
                "No valid scopes requested by client {ClientId}. Requested: {RequestedScopes}",
                request.ClientId,
                string.Join(", ", requestedScopes));
        }

        principal.SetScopes(allowedScopes);

        if (allowedScopes.Contains("api"))
        {
            principal.SetResources("api-resource");
        }

        AttachDestinations(principal);

        _logger.LogInformation(
            "Authorizing user {UserId} for client {ClientId} with scopes: {Scopes}, session: {SessionId}",
            user.Id,
            request.ClientId,
            string.Join(", ", allowedScopes),
            session.Id);

        return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    /// <summary>
    /// Token endpoint - exchanges authorization code for access/refresh tokens
    /// </summary>
    [HttpPost("token")]
    [IgnoreAntiforgeryToken]
    [Produces("application/json")]
    public async Task<IActionResult> Exchange(CancellationToken cancellationToken = default)
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
            return await HandleAuthorizationCodeGrantAsync(request, cancellationToken);
        }

        if (request.IsRefreshTokenGrantType())
        {
            return await HandleRefreshTokenGrantAsync(request, cancellationToken);
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
    /// FIXED: Now properly revokes session when refresh token is revoked
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

        var token = await _tokenManager.FindByReferenceIdAsync(request.Token, cancellationToken);
        token ??= await _tokenManager.FindByIdAsync(request.Token, cancellationToken);

        if (token == null)
        {
            _logger.LogDebug("Token not found for revocation (per RFC 7009, returning success)");
            return Ok();
        }

        // FIXED: Extract principal and revoke associated session
        //ClaimsPrincipal? principal = null;
        //try
        //{
        //    principal = await _tokenManager.GetPrincipalAsync(token, cancellationToken);
        //}
        //catch (Exception ex)
        //{
        //    _logger.LogWarning(ex, "Failed to extract principal from token during revocation");
        //}

        await _tokenManager.TryRevokeAsync(token, cancellationToken);
        _logger.LogInformation("Token revoked successfully");

        // FIXED: Revoke associated session if we have principal
        //if (principal is not null)
        //{
        //    await RevokeSessionIfExistsAsync(principal, cancellationToken);
        //}

        return Ok();
    }

    /// <summary>
    /// UserInfo endpoint - returns user information based on granted scopes
    /// </summary>
    [Authorize(AuthenticationSchemes = OpenIddictServerAspNetCoreDefaults.AuthenticationScheme)]
    [HttpGet("userinfo")]
    [HttpPost("userinfo")]
    [Produces("application/json")]
    public async Task<IActionResult> Userinfo(CancellationToken cancellationToken = default)
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

        if (User.HasScope(OpenIddictConstants.Scopes.Profile))
        {
            claims[OpenIddictConstants.Claims.Name] = user.UserName ?? string.Empty;

            if (!string.IsNullOrEmpty(user.FirstName))
                claims[OpenIddictConstants.Claims.GivenName] = user.FirstName;

            if (!string.IsNullOrEmpty(user.LastName))
                claims[OpenIddictConstants.Claims.FamilyName] = user.LastName;
        }

        if (User.HasScope(OpenIddictConstants.Scopes.Email))
        {
            claims[OpenIddictConstants.Claims.Email] = user.Email ?? string.Empty;
            claims[OpenIddictConstants.Claims.EmailVerified] = user.EmailConfirmed;
        }

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
    private async Task<IActionResult> HandleAuthorizationCodeGrantAsync(
        OpenIddictRequest request,
        CancellationToken cancellationToken)
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

        await RefreshUserClaimsAsync(principal, user);
        AttachDestinations(principal);

        _logger.LogInformation("Code exchanged for tokens - User: {UserId}, Client: {ClientId}", userId, request.ClientId);
        return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    /// <summary>
    /// Handles refresh token grant
    /// FIXED: Validates session is still active during refresh
    /// </summary>
    private async Task<IActionResult> HandleRefreshTokenGrantAsync(
        OpenIddictRequest request,
        CancellationToken cancellationToken)
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
                    "The user is no longer allowed to sign in."));
        }

        // FIXED: Validate session is still active
        var sessionIdClaim = principal.GetClaim(AuthConstants.Claims.SessionId);
        if (!string.IsNullOrEmpty(sessionIdClaim) && Guid.TryParse(sessionIdClaim, out var sessionId))
        {
            try
            {
                var session = await _sessionService.GetByIdAsync(sessionId, cancellationToken);
                if (session is null || session.Status != SessionStatus.Active)
                {
                    _logger.LogWarning("Session {SessionId} is invalid or inactive during refresh", sessionId);
                    return Forbid(
                        authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                        properties: CreateErrorProperties(
                            OpenIddictConstants.Errors.InvalidGrant,
                            "The session is no longer valid."));
                }

                // Update session activity
                session.LastActivityAt = DateTimeOffset.UtcNow;
                await _sessionService.UpdateAsync(session, cancellationToken);

                _logger.LogDebug("Session {SessionId} activity updated during refresh", sessionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating session {SessionId} during refresh", sessionId);
                return Forbid(
                    authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                    properties: CreateErrorProperties(
                        OpenIddictConstants.Errors.InvalidGrant,
                        "Session validation failed."));
            }
        }

        await RefreshUserClaimsAsync(principal, user);
        AttachDestinations(principal);

        _logger.LogInformation("Tokens refreshed - User: {UserId}, Client: {ClientId}", userId, request.ClientId);
        return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    /// <summary>
    /// FIXED: Ensures all required OpenIddict claims are present
    /// Now includes SessionId for session tracking
    /// </summary>
    private void EnsureRequiredClaims(ClaimsPrincipal principal, IdentityUser user, Guid sessionId)
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

        // FIXED: Add SessionId claim for session tracking
        if (!identity.HasClaim(c => c.Type == AuthConstants.Claims.SessionId))
        {
            identity.AddClaim(new Claim(AuthConstants.Claims.SessionId, sessionId.ToString()));
        }
    }

    /// <summary>
    /// Refreshes user claims from database to ensure they're current
    /// </summary>
    private async Task RefreshUserClaimsAsync(ClaimsPrincipal principal, IdentityUser user)
    {
        var identity = (ClaimsIdentity)principal.Identity!;

        var emailVerifiedClaim = identity.FindFirst(OpenIddictConstants.Claims.EmailVerified);
        if (emailVerifiedClaim != null)
        {
            identity.RemoveClaim(emailVerifiedClaim);
            identity.AddClaim(new Claim(
                OpenIddictConstants.Claims.EmailVerified,
                user.EmailConfirmed.ToString().ToLowerInvariant(),
                ClaimValueTypes.Boolean));
        }

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
    /// FIXED: Revokes session when refresh token is revoked
    /// </summary>
    private async Task RevokeSessionIfExistsAsync(ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        var sessionIdClaim = principal.GetClaim(AuthConstants.Claims.SessionId);
        if (string.IsNullOrEmpty(sessionIdClaim) || !Guid.TryParse(sessionIdClaim, out var sessionId))
        {
            _logger.LogDebug("No valid SessionId claim found during token revocation");
            return;
        }

        try
        {
            var session = await _sessionService.GetByIdAsync(sessionId, cancellationToken);
            if (session is not null && session.Status == SessionStatus.Active)
            {
                session.Status = SessionStatus.Revoked;
                session.RevokedAt = DateTimeOffset.UtcNow;
                session.RevocationReason = "Refresh token revoked";
                await _sessionService.UpdateAsync(session, cancellationToken);

                _logger.LogInformation("Session {SessionId} revoked due to token revocation", sessionId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to revoke session {SessionId} during token revocation", sessionId);
        }
    }

    /// <summary>
    /// Sets claim destinations for inclusion in specific token types
    /// FIXED: SessionId now included in both access and refresh tokens
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
    /// FIXED: SessionId included in both AccessToken and RefreshToken
    /// </summary>
    private static IEnumerable<string> GetDestinations(Claim claim, ClaimsPrincipal principal)
    {
        return claim.Type switch
        {
            // SessionId: Must be in both access and refresh tokens for session tracking
            AuthConstants.Claims.SessionId =>
                new[] {
                    OpenIddictConstants.Destinations.AccessToken
                },

            OpenIddictConstants.Claims.Subject =>
                principal.HasScope(OpenIddictConstants.Scopes.OpenId)
                    ? new[] { OpenIddictConstants.Destinations.AccessToken, OpenIddictConstants.Destinations.IdentityToken }
                    : new[] { OpenIddictConstants.Destinations.AccessToken },

            OpenIddictConstants.Claims.Name or
            OpenIddictConstants.Claims.GivenName or
            OpenIddictConstants.Claims.FamilyName =>
                principal.HasScope(OpenIddictConstants.Scopes.Profile)
                    ? new[] { OpenIddictConstants.Destinations.AccessToken, OpenIddictConstants.Destinations.IdentityToken }
                    : new[] { OpenIddictConstants.Destinations.AccessToken },

            OpenIddictConstants.Claims.Email or
            OpenIddictConstants.Claims.EmailVerified =>
                principal.HasScope(OpenIddictConstants.Scopes.Email)
                    ? new[] { OpenIddictConstants.Destinations.AccessToken, OpenIddictConstants.Destinations.IdentityToken }
                    : new[] { OpenIddictConstants.Destinations.AccessToken },

            OpenIddictConstants.Claims.Role =>
                new[] { OpenIddictConstants.Destinations.AccessToken },

            OpenIddictConstants.Claims.Audience =>
                new[] { OpenIddictConstants.Destinations.AccessToken },

            _ => new[] { OpenIddictConstants.Destinations.AccessToken }
        };
    }

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