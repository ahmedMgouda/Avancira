using System.Security.Claims;
using IdentityUser = Avancira.Infrastructure.Identity.Users.User;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
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
            OpenIddictConstants.Scopes.OfflineAccess,
            "api"
        };

    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly IOpenIddictTokenManager _tokenManager;
    private readonly ILogger<OpenIddictController> _logger;

    public OpenIddictController(
        SignInManager<IdentityUser> signInManager,
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
            _logger.LogWarning("User {UserId} cannot sign in", user.Id);
            return Forbid(
                properties: CreateErrorProperties(OpenIddictConstants.Errors.InvalidGrant,
                    "The user is not allowed to sign in."),
                authenticationSchemes: new[] { OpenIddictServerAspNetCoreDefaults.AuthenticationScheme });
        }

        if (!user.EmailConfirmed && _signInManager.Options.SignIn.RequireConfirmedEmail)
        {
            _logger.LogWarning("User {UserId} email not confirmed", user.Id);
            return Forbid(
                properties: CreateErrorProperties(OpenIddictConstants.Errors.InvalidGrant,
                    "Email confirmation is required."),
                authenticationSchemes: new[] { OpenIddictServerAspNetCoreDefaults.AuthenticationScheme });
        }

        var principal = await _signInManager.CreateUserPrincipalAsync(user);
        EnsureRequiredClaims(principal, user);

        var requestedScopes = request.GetScopes().ToList();
        var allowedScopes = requestedScopes
            .Where(scope => AllowedScopes.Contains(scope))
            .ToList();

        if (!allowedScopes.Contains(OpenIddictConstants.Scopes.OpenId))
            allowedScopes.Add(OpenIddictConstants.Scopes.OpenId);

        principal.SetScopes(allowedScopes);

        if (allowedScopes.Contains("api"))
            principal.SetResources("api-resource");

        AttachDestinations(principal);

        _logger.LogInformation("Authorizing user {UserId} for client {ClientId} with scopes: {Scopes}",
            user.Id, request.ClientId, string.Join(", ", allowedScopes));

        return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
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
            return await HandleAuthorizationCodeGrantAsync(request, cancellationToken);

        if (request.IsRefreshTokenGrantType())
            return await HandleRefreshTokenGrantAsync(request, cancellationToken);

        _logger.LogWarning("Unsupported grant type: {GrantType}", request.GrantType);
        return Forbid(
            properties: CreateErrorProperties(OpenIddictConstants.Errors.UnsupportedGrantType,
                "The specified grant type is not supported."),
            authenticationSchemes: new[] { OpenIddictServerAspNetCoreDefaults.AuthenticationScheme });
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
            _logger.LogDebug("Token not found for revocation (RFC 7009 compliance, returning success)");
            return Ok();
        }

        await _tokenManager.TryRevokeAsync(token, cancellationToken);
        _logger.LogInformation("Token revoked successfully");


        // Prune old/invalid tokens older than 30 days
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
            claims[OpenIddictConstants.Claims.Role] = roles.ToArray();

        _logger.LogDebug("Userinfo returned for user {UserId}", userId);
        return Ok(claims);
    }

    // ══════════════════════════════════════════════════════════════════
    // PRIVATE HELPERS
    // ══════════════════════════════════════════════════════════════════
    private async Task<IActionResult> HandleAuthorizationCodeGrantAsync(OpenIddictRequest request, CancellationToken cancellationToken)
    {
        var result = await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        var principal = result.Principal;

        if (principal == null)
        {
            _logger.LogError("Authorization code principal cannot be retrieved");
            return Forbid(
                properties: CreateErrorProperties(OpenIddictConstants.Errors.InvalidGrant,
                    "The authorization code is invalid."),
                authenticationSchemes: new[] { OpenIddictServerAspNetCoreDefaults.AuthenticationScheme });
        }

        var userId = principal.GetClaim(OpenIddictConstants.Claims.Subject);
        var user = await _signInManager.UserManager.FindByIdAsync(userId!);

        if (user == null || !await _signInManager.CanSignInAsync(user))
        {
            _logger.LogWarning("User {UserId} not found or cannot sign in during code exchange", userId);
            return Forbid(
                properties: CreateErrorProperties(OpenIddictConstants.Errors.InvalidGrant,
                    "The user is no longer allowed to sign in."),
                authenticationSchemes: new[] { OpenIddictServerAspNetCoreDefaults.AuthenticationScheme });
        }

        await RefreshUserClaimsAsync(principal, user);
        AttachDestinations(principal);

        _logger.LogInformation("Code exchanged for tokens - User: {UserId}, Client: {ClientId}", userId, request.ClientId);
        return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    private async Task<IActionResult> HandleRefreshTokenGrantAsync(OpenIddictRequest request, CancellationToken cancellationToken)
    {
        var result = await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        var principal = result.Principal;

        if (principal == null)
        {
            _logger.LogError("Refresh token principal cannot be retrieved");
            return Forbid(
                properties: CreateErrorProperties(OpenIddictConstants.Errors.InvalidGrant,
                    "The refresh token is invalid."),
                authenticationSchemes: new[] { OpenIddictServerAspNetCoreDefaults.AuthenticationScheme });
        }

        var userId = principal.GetClaim(OpenIddictConstants.Claims.Subject);
        var user = await _signInManager.UserManager.FindByIdAsync(userId!);

        if (user == null || !await _signInManager.CanSignInAsync(user))
        {
            _logger.LogWarning("User {UserId} not found or cannot sign in during refresh", userId);
            return Forbid(
                properties: CreateErrorProperties(OpenIddictConstants.Errors.InvalidGrant,
                    "The user is no longer allowed to sign in."),
                authenticationSchemes: new[] { OpenIddictServerAspNetCoreDefaults.AuthenticationScheme });
        }

        await RefreshUserClaimsAsync(principal, user);
        AttachDestinations(principal);

        _logger.LogInformation("Tokens refreshed - User: {UserId}, Client: {ClientId}", userId, request.ClientId);
        return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    private void EnsureRequiredClaims(ClaimsPrincipal principal, IdentityUser user)
    {
        var identity = (ClaimsIdentity)principal.Identity!;

        if (!identity.HasClaim(c => c.Type == OpenIddictConstants.Claims.Subject))
            identity.AddClaim(new Claim(OpenIddictConstants.Claims.Subject, user.Id));

        if (!identity.HasClaim(c => c.Type == OpenIddictConstants.Claims.Email) && !string.IsNullOrEmpty(user.Email))
            identity.AddClaim(new Claim(OpenIddictConstants.Claims.Email, user.Email));

        if (!identity.HasClaim(c => c.Type == OpenIddictConstants.Claims.EmailVerified))
            identity.AddClaim(new Claim(OpenIddictConstants.Claims.EmailVerified,
                user.EmailConfirmed.ToString().ToLowerInvariant(), ClaimValueTypes.Boolean));
    }

    private async Task RefreshUserClaimsAsync(ClaimsPrincipal principal, IdentityUser user)
    {
        var identity = (ClaimsIdentity)principal.Identity!;
        var emailVerifiedClaim = identity.FindFirst(OpenIddictConstants.Claims.EmailVerified);
        if (emailVerifiedClaim != null)
        {
            identity.RemoveClaim(emailVerifiedClaim);
            identity.AddClaim(new Claim(OpenIddictConstants.Claims.EmailVerified,
                user.EmailConfirmed.ToString().ToLowerInvariant(), ClaimValueTypes.Boolean));
        }

        var roleClaims = identity.FindAll(OpenIddictConstants.Claims.Role).ToList();
        foreach (var roleClaim in roleClaims)
            identity.RemoveClaim(roleClaim);

        var currentRoles = await _signInManager.UserManager.GetRolesAsync(user);
        foreach (var role in currentRoles)
            identity.AddClaim(new Claim(OpenIddictConstants.Claims.Role, role));
    }

    private static void AttachDestinations(ClaimsPrincipal principal)
    {
        foreach (var claim in principal.Claims)
        {
            var destinations = GetDestinations(claim, principal);
            if (destinations.Any())
                claim.SetDestinations(destinations);
        }
    }

    private static IEnumerable<string> GetDestinations(Claim claim, ClaimsPrincipal principal)
    {
        return claim.Type switch
        {
            OpenIddictConstants.Claims.Subject => principal.HasScope(OpenIddictConstants.Scopes.OpenId)
                ? new[]
                {
                    OpenIddictConstants.Destinations.AccessToken,
                    OpenIddictConstants.Destinations.IdentityToken
                }
                : new[] { OpenIddictConstants.Destinations.AccessToken },

            OpenIddictConstants.Claims.Name or
            OpenIddictConstants.Claims.GivenName or
            OpenIddictConstants.Claims.FamilyName => principal.HasScope(OpenIddictConstants.Scopes.Profile)
                ? new[]
                {
                    OpenIddictConstants.Destinations.AccessToken,
                    OpenIddictConstants.Destinations.IdentityToken
                }
                : new[] { OpenIddictConstants.Destinations.AccessToken },

            OpenIddictConstants.Claims.Email or
            OpenIddictConstants.Claims.EmailVerified => principal.HasScope(OpenIddictConstants.Scopes.Email)
                ? new[]
                {
                    OpenIddictConstants.Destinations.AccessToken,
                    OpenIddictConstants.Destinations.IdentityToken
                }
                : new[] { OpenIddictConstants.Destinations.AccessToken },

            OpenIddictConstants.Claims.Role =>
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
}
