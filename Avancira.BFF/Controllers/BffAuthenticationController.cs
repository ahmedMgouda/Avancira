using System.Security.Claims;
using Duende.AccessTokenManagement.OpenIdConnect;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Avancira.BFF.Controllers;

[ApiController]
[Route("bff/auth")]
public class BffAuthenticationController : ControllerBase
{
    private readonly ILogger<BffAuthenticationController> _logger;
    private readonly IUserTokenManager _tokenManager;

    public BffAuthenticationController(
        ILogger<BffAuthenticationController> logger,
        IUserTokenManager tokenManager)
    {
        _logger = logger;
        _tokenManager = tokenManager;
    }

    // ══════════════════════════════════════════════════════════════════
    // LOGIN
    // ══════════════════════════════════════════════════════════════════
    [HttpGet("login")]
    [AllowAnonymous]
    public IActionResult Login([FromQuery] string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            _logger.LogDebug("User already authenticated, redirecting to {ReturnUrl}", returnUrl ?? "/");
            return Redirect(GetSafeReturnUrl(returnUrl));
        }

        var props = new AuthenticationProperties
        {
            RedirectUri = GetSafeReturnUrl(returnUrl),
            IsPersistent = true
        };

        _logger.LogInformation("Starting login flow with returnUrl={ReturnUrl}", returnUrl ?? "/");
        return Challenge(props, OpenIdConnectDefaults.AuthenticationScheme);
    }

    // ══════════════════════════════════════════════════════════════════
    // GET USER
    // ══════════════════════════════════════════════════════════════════
    [HttpGet("user")]
    [AllowAnonymous]
    public async Task<IActionResult> GetUser(CancellationToken ct = default)
    {
        if (User.Identity?.IsAuthenticated != true)
            return Ok(new { isAuthenticated = false });

        var userId = User.FindFirstValue("sub");
        if (string.IsNullOrEmpty(userId))
            return Ok(new { isAuthenticated = false });

        try
        {
            var tokenResult = await _tokenManager.GetAccessTokenAsync(User, parameters: null, ct: ct);
            if (tokenResult.Succeeded && tokenResult.Token is not null)
            {
                var token = tokenResult.Token;
                var expiresIn = (int)(token.Expiration - DateTimeOffset.UtcNow).TotalSeconds;

                return Ok(new
                {
                    isAuthenticated = true,
                    sub = userId,
                    name = User.FindFirstValue("name"),
                    givenName = User.FindFirstValue("given_name"),
                    familyName = User.FindFirstValue("family_name"),
                    email = User.FindFirstValue("email"),
                    emailVerified = bool.TryParse(User.FindFirstValue("email_verified"), out var verified) && verified,
                    roles = User.FindAll("role").Select(c => c.Value).ToArray(),
                    scopes = User.FindAll("scope").Select(c => c.Value).ToArray(),
                    tokenExpiresAt = token.Expiration.ToString("o"),
                    tokenExpiresIn = expiresIn
                });
            }

            _logger.LogWarning("Failed to get access token for user {UserId}", userId);
            return Ok(new { isAuthenticated = false });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user info for user {UserId}", userId);
            return Ok(new { isAuthenticated = false });
        }
    }

    // ══════════════════════════════════════════════════════════════════
    // LOGOUT
    // ══════════════════════════════════════════════════════════════════
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout(CancellationToken ct = default)
    {
        var userId = User.FindFirstValue("sub");
        _logger.LogInformation("Logout initiated for user {UserId}", userId);

        try
        {
            // Try to revoke refresh token
            try
            {
                await _tokenManager.RevokeRefreshTokenAsync(User, parameters: null, ct: ct);
                _logger.LogInformation("Refresh token revoked for user {UserId}", userId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to revoke refresh token for user {UserId}", userId);
            }

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            var props = new AuthenticationProperties { RedirectUri = "/" };
            await HttpContext.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme, props);

            return Ok(new { success = true, redirectUri = "/" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout for user {UserId}", userId);
            return StatusCode(500, new { success = false, message = "Error during logout" });
        }
    }

    // ══════════════════════════════════════════════════════════════════
    // CLAIMS (DEV)
    // ══════════════════════════════════════════════════════════════════
    [HttpGet("claims")]
    [Authorize]
    public IActionResult GetClaims()
    {
        if (!HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment())
            return NotFound();

        var claims = User.Claims.Select(c => new { c.Type, c.Value }).ToList();
        return Ok(new
        {
            identity = User.Identity?.Name,
            isAuthenticated = User.Identity?.IsAuthenticated,
            claims
        });
    }

    // ══════════════════════════════════════════════════════════════════
    // HELPERS
    // ══════════════════════════════════════════════════════════════════
    private string GetSafeReturnUrl(string? returnUrl)
    {
        const string defaultUrl = "https://localhost:4200/";
        if (string.IsNullOrWhiteSpace(returnUrl) || !Url.IsLocalUrl(returnUrl))
            return defaultUrl;
        return returnUrl;
    }
}
