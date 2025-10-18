using System.Security.Claims;
using Avancira.Auth.Helpers;
using Avancira.Infrastructure.Identity.Users;
using Avancira.Shared.Authorization;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;

namespace Avancira.Auth.Controllers;

[AllowAnonymous]
[Route("connect")]
public sealed class OpenIddictController : Controller
{
    private readonly SignInManager<User> _signInManager;
    private readonly IOpenIddictTokenManager _tokenManager;
    private readonly ILogger<OpenIddictController> _logger;

    public OpenIddictController(
        SignInManager<User> signInManager,
        IOpenIddictTokenManager tokenManager,
        ILogger<OpenIddictController> logger)
    {
        _signInManager = signInManager;
        _tokenManager = tokenManager;
        _logger = logger;
    }

    // ══════════════════════════════════════════════════════════════════
    // AUTHORIZE ENDPOINT
    // ══════════════════════════════════════════════════════════════════
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

        // Validate client_id
        if (string.IsNullOrWhiteSpace(request.ClientId))
        {
            _logger.LogWarning("Authorization request missing client_id");
            return BadRequest(new
            {
                error = OpenIddictConstants.Errors.InvalidRequest,
                error_description = "The client_id parameter is required."
            });
        }

        // Check if user is authenticated
        var result = await HttpContext.AuthenticateAsync(IdentityConstants.ApplicationScheme);
        if (!result.Succeeded || result.Principal == null)
        {
            var returnUrl = Request.Path + Request.QueryString;
            _logger.LogDebug("User not authenticated, redirecting to login: {ReturnUrl}", returnUrl);

            return Challenge(
                authenticationSchemes: IdentityConstants.ApplicationScheme,
                properties: new AuthenticationProperties { RedirectUri = returnUrl });
        }

        // Get user from database
        var user = await _signInManager.UserManager.GetUserAsync(result.Principal);
        if (user == null)
        {
            _logger.LogWarning("User principal exists but user not found in database");
            return Challenge(IdentityConstants.ApplicationScheme);
        }

        // Validate user can sign in
        if (!await _signInManager.CanSignInAsync(user))
        {
            _logger.LogWarning("User {UserId} cannot sign in", user.Id);
            return Forbid(
                authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                properties: CreateErrorProperties(
                    OpenIddictConstants.Errors.InvalidGrant,
                    "The user is not allowed to sign in."));
        }

        // Check email confirmation if required
        if (!user.EmailConfirmed && _signInManager.Options.SignIn.RequireConfirmedEmail)
        {
            _logger.LogWarning("User {UserId} email not confirmed", user.Id);
            return Forbid(
                authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                properties: CreateErrorProperties(
                    OpenIddictConstants.Errors.InvalidGrant,
                    "Email confirmation is required."));
        }

        // ═══════════════════════════════════════════════════════════════
        // READ SESSION ID (created in AccountController.Login)
        // Do NOT create a new session ID here
        // ═══════════════════════════════════════════════════════════════
        var sessionId = result.Principal.GetClaim(OidcClaimTypes.SessionId);

        // If session ID doesn't exist (shouldn't happen), generate one as fallback
        if (string.IsNullOrEmpty(sessionId))
        {
            _logger.LogWarning("Session ID missing from principal, generating new one (this shouldn't happen)");
            sessionId = ClaimsHelper.GenerateSessionId();

            // Add it to the principal
            var identity = (ClaimsIdentity)result.Principal.Identity!;
            identity.AddClaim(new Claim(OidcClaimTypes.SessionId, sessionId));
        }

        // Add required claims (sub, email, email_verified)
        // Note: sid already exists, no need to add it again
        ClaimsHelper.EnsureRequiredClaims(result.Principal, user, sessionId);

        // Add profile claims if profile scope requested
        var requestedScopes = request.GetScopes().ToList();

        if (requestedScopes.Contains(OpenIddictConstants.Scopes.Profile))
        {
            ClaimsHelper.AddProfileClaims(result.Principal, user);
        }

        // Add roles
        await ClaimsHelper.AddRoleClaimsAsync(result.Principal, user, _signInManager.UserManager);


        result.Principal.SetScopes(requestedScopes);

        // Set resources
        if (requestedScopes.Contains("api"))
        {
            result.Principal.SetResources("api-resource");
        }

        // ═══════════════════════════════════════════════════════════════
        // CRITICAL: Attach claim destinations
        // This controls which claims go to ID token vs access token
        // ═══════════════════════════════════════════════════════════════
        ClaimsHelper.AttachDestinations(result.Principal);

        _logger.LogInformation(
            "User authorized - UserId: {UserId}, SessionId: {SessionId}, Client: {ClientId}, Scopes: {Scopes}",
            user.Id,
            sessionId,
            request.ClientId,
            string.Join(", ", requestedScopes));

        return SignIn(result.Principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    // ══════════════════════════════════════════════════════════════════
    // TOKEN ENDPOINT
    // ══════════════════════════════════════════════════════════════════
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

    // ══════════════════════════════════════════════════════════════════
    // REVOCATION ENDPOINT
    // ══════════════════════════════════════════════════════════════════
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

        var token = await _tokenManager.FindByReferenceIdAsync(request.Token, cancellationToken)
                    ?? await _tokenManager.FindByIdAsync(request.Token, cancellationToken);

        if (token == null)
        {
            // RFC 7009: Return success even if token not found
            _logger.LogDebug("Token not found for revocation (RFC 7009 compliance)");
            return Ok();
        }

        var subject = await _tokenManager.GetSubjectAsync(token, cancellationToken);
        await _tokenManager.TryRevokeAsync(token, cancellationToken);

        _logger.LogInformation("✅ Token revoked - Subject: {Subject}", subject);

        // Cleanup old tokens
        await _tokenManager.PruneAsync(DateTimeOffset.UtcNow.AddDays(-30), cancellationToken);

        return Ok();
    }

    // ══════════════════════════════════════════════════════════════════
    // USERINFO ENDPOINT
    // ══════════════════════════════════════════════════════════════════
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

        // Profile scope claims
        if (User.HasScope(OpenIddictConstants.Scopes.Profile))
        {
            claims[OpenIddictConstants.Claims.Name] = user.UserName ?? string.Empty;

            if (!string.IsNullOrEmpty(user.FirstName))
                claims[OpenIddictConstants.Claims.GivenName] = user.FirstName;

            if (!string.IsNullOrEmpty(user.LastName))
                claims[OpenIddictConstants.Claims.FamilyName] = user.LastName;
        }

        // Email scope claims
        if (User.HasScope(OpenIddictConstants.Scopes.Email))
        {
            claims[OpenIddictConstants.Claims.Email] = user.Email ?? string.Empty;
            claims[OpenIddictConstants.Claims.EmailVerified] = user.EmailConfirmed;
        }

        // Roles
        var roles = await _signInManager.UserManager.GetRolesAsync(user);
        if (roles.Count > 0)
        {
            claims[OpenIddictConstants.Claims.Role] = roles.ToArray();
        }

        _logger.LogDebug("Userinfo returned for user {UserId}", userId);
        return Ok(claims);
    }

    // ══════════════════════════════════════════════════════════════════
    // PRIVATE HELPERS
    // ══════════════════════════════════════════════════════════════════

    private async Task<IActionResult> HandleAuthorizationCodeGrantAsync(
        OpenIddictRequest request,
        CancellationToken cancellationToken)
    {
        var result = await HttpContext.AuthenticateAsync(
            OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

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

        // Refresh dynamic claims (email verification, roles)
        await ClaimsHelper.RefreshDynamicClaimsAsync(principal, user, _signInManager.UserManager);

        // Reattach destinations
        ClaimsHelper.AttachDestinations(principal);

        var sessionId = principal.GetClaim(OidcClaimTypes.SessionId);
        _logger.LogInformation(
            "Code exchanged - UserId: {UserId}, SessionId: {SessionId}, Client: {ClientId}",
            userId,
            sessionId,
            request.ClientId);

        return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    private async Task<IActionResult> HandleRefreshTokenGrantAsync(
        OpenIddictRequest request,
        CancellationToken cancellationToken)
    {
        var result = await HttpContext.AuthenticateAsync(
            OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

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

        // Refresh dynamic claims (email verification, roles)
        await ClaimsHelper.RefreshDynamicClaimsAsync(principal, user, _signInManager.UserManager);

        // Reattach destinations
        ClaimsHelper.AttachDestinations(principal);

        var sessionId = principal.GetClaim(OidcClaimTypes.SessionId);
        _logger.LogInformation(
            "Token refreshed - UserId: {UserId}, SessionId: {SessionId}, Client: {ClientId}",
            userId,
            sessionId,
            request.ClientId);

        return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    private static AuthenticationProperties CreateErrorProperties(
        string error,
        string errorDescription)
    {
        return new AuthenticationProperties(new Dictionary<string, string?>
        {
            [OpenIddictServerAspNetCoreConstants.Properties.Error] = error,
            [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = errorDescription
        });
    }
}
