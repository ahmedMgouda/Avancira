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
public sealed class AuthorizationController : Controller
{
    private readonly SignInManager<User> _signInManager;
    private readonly IOpenIddictTokenManager _tokenManager;
    private readonly IOpenIddictScopeManager _scopeManager;
    private readonly ILogger<OpenIddictController> _logger;

    public AuthorizationController(
        SignInManager<User> signInManager,
        IOpenIddictTokenManager tokenManager,
        IOpenIddictScopeManager scopeManager,
        ILogger<OpenIddictController> logger)
    {
        _signInManager = signInManager;
        _tokenManager = tokenManager;
        _scopeManager = scopeManager;
        _logger = logger;
    }

    // ══════════════════════════════════════════════════════════════════
    // AUTHORIZE ENDPOINT (Aligned with OpenIddict official sample)
    // ══════════════════════════════════════════════════════════════════
    [HttpGet("authorize")]
    [HttpPost("authorize")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Authorize(CancellationToken cancellationToken = default)
    {
        var request = HttpContext.GetOpenIddictServerRequest();
        if (request is null)
        {
            _logger.LogError("OpenID Connect request cannot be retrieved");
            return BadRequest(new
            {
                error = OpenIddictConstants.Errors.InvalidRequest,
                error_description = "The OpenID Connect request cannot be retrieved."
            });
        }

        // ───────────────────────────────────────────────────────────────
        // 1. Validate client_id
        // ───────────────────────────────────────────────────────────────
        if (string.IsNullOrWhiteSpace(request.ClientId))
        {
            _logger.LogWarning("Authorization request missing client_id");
            return BadRequest(new
            {
                error = OpenIddictConstants.Errors.InvalidRequest,
                error_description = "The client_id parameter is required."
            });
        }

        // ───────────────────────────────────────────────────────────────
        // 2. Authenticate user session (cookie)
        // ───────────────────────────────────────────────────────────────
        var result = await HttpContext.AuthenticateAsync(IdentityConstants.ApplicationScheme);
        if (!result.Succeeded || result.Principal is null)
        {
            var returnUrl = Request.Path + Request.QueryString;
            _logger.LogDebug("User not authenticated, redirecting to login: {ReturnUrl}", returnUrl);

            return Challenge(
                authenticationSchemes: IdentityConstants.ApplicationScheme,
                properties: new AuthenticationProperties { RedirectUri = returnUrl });
        }

        // ───────────────────────────────────────────────────────────────
        // 3. Validate user from database
        // ───────────────────────────────────────────────────────────────
        var user = await _signInManager.UserManager.GetUserAsync(result.Principal);
        if (user is null)
        {
            _logger.LogWarning("User principal exists but user not found in database");
            return Challenge(IdentityConstants.ApplicationScheme);
        }

        if (!await _signInManager.CanSignInAsync(user))
        {
            _logger.LogWarning("User {UserId} cannot sign in", user.Id);
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

        // ───────────────────────────────────────────────────────────────
        // 4. Ensure session ID exists
        // ───────────────────────────────────────────────────────────────
        var sessionId = result.Principal.GetClaim(OidcClaimTypes.SessionId);
        if (string.IsNullOrEmpty(sessionId))
        {
            _logger.LogWarning("Session ID missing from principal, generating new one (this shouldn't happen)");
            sessionId = ClaimsHelper.GenerateSessionId();

            var identity = (ClaimsIdentity)result.Principal.Identity!;
            identity.AddClaim(new Claim(OidcClaimTypes.SessionId, sessionId));
        }

        // ───────────────────────────────────────────────────────────────
        // 5. Add user and profile claims
        // ───────────────────────────────────────────────────────────────
        ClaimsHelper.EnsureRequiredClaims(result.Principal, user, sessionId);

        var requestedScopes = request.GetScopes().ToList();

        if (requestedScopes.Contains(OpenIddictConstants.Scopes.Profile))
        {
            ClaimsHelper.AddProfileClaims(result.Principal, user);
        }

        await ClaimsHelper.AddRoleClaimsAsync(result.Principal, user, _signInManager.UserManager);

        result.Principal.SetScopes(requestedScopes);

        // ───────────────────────────────────────────────────────────────
        // 6. Dynamically resolve resources (scopes → resources)
        // ───────────────────────────────────────────────────────────────
   
        result.Principal.SetResources(
            await _scopeManager.ListResourcesAsync(result.Principal.GetScopes(), cancellationToken).ToListAsync());
 
        // ───────────────────────────────────────────────────────────────
        // 7. Attach claim destinations (ID token vs Access token)
        // ───────────────────────────────────────────────────────────────
        ClaimsHelper.AttachDestinations(result.Principal);

        // ───────────────────────────────────────────────────────────────
        // 8. Return authorization response (token issuance)
        // ───────────────────────────────────────────────────────────────
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


    //[HttpGet("logout")]
    //[HttpPost("logout")]
    //[IgnoreAntiforgeryToken]
    //public async Task<IActionResult> Logout()
    //{
    //    var request = HttpContext.GetOpenIddictServerRequest();

    //    _logger.LogInformation(
    //        "Logout endpoint called - PostLogoutRedirectUri: {RedirectUri}",
    //        request?.PostLogoutRedirectUri);

    //    // Step 1: Get the authenticated user (if any)
    //    var result = await HttpContext.AuthenticateAsync(IdentityConstants.ApplicationScheme);

    //    if (result?.Succeeded == true && result.Principal != null)
    //    {
    //        var userId = result.Principal.FindFirstValue(ClaimTypes.NameIdentifier);
    //        var sessionId = result.Principal.FindFirstValue("sid");

    //        _logger.LogInformation(
    //            "Logging out user - UserId: {UserId}, SessionId: {SessionId}",
    //            userId,
    //            sessionId);

    //        // Step 2: Sign out from Identity (clears the cookie)
    //        await _signInManager.SignOutAsync();
    //    }
    //    else
    //    {
    //        _logger.LogDebug("No authenticated user found during logout");
    //    }

    //    // Step 3: Determine where to redirect after logout
    //    var redirectUri = request?.PostLogoutRedirectUri;

    //    // If no redirect URI provided, use default
    //    if (string.IsNullOrWhiteSpace(redirectUri))
    //    {
    //        redirectUri = "/";
    //        _logger.LogDebug("No post_logout_redirect_uri provided, using default: {RedirectUri}", redirectUri);
    //    }

    //    return Redirect(redirectUri);
    //}

    [HttpGet("logout")]
    [HttpPost("logout")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Logout()
    {
        var request = HttpContext.GetOpenIddictServerRequest();
        var redirectUri = request?.PostLogoutRedirectUri ?? "/";

        _logger.LogInformation("Logout endpoint called - Redirect: {RedirectUri}", redirectUri);

        await HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
        await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

        return SignOut(
            new AuthenticationProperties { RedirectUri = redirectUri },
            OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
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
