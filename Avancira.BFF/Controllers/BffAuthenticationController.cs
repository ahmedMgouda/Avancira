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

    // --------------------------------------------------------------
    // LOGIN
    // --------------------------------------------------------------
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

    // --------------------------------------------------------------
    // LOGOUT (simplified – no localOnly flag)
    // --------------------------------------------------------------
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout(CancellationToken ct = default)
    {
        var userId = User.FindFirstValue("sub");
        _logger.LogInformation("Logout initiated for user {UserId}", userId);

        try
        {
            // Try to revoke refresh token on the IdP
            try
            {
                await _tokenManager.RevokeRefreshTokenAsync(User, parameters: null, ct: ct);
                _logger.LogInformation("Refresh token revoked for user {UserId}", userId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to revoke refresh token for user {UserId}", userId);
            }

            // Clear local cookies
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            // Sign out from OIDC provider (redirect to IdP logout)
            var props = new AuthenticationProperties { RedirectUri = "/" };
            await HttpContext.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme, props);

            return Ok(new { success = true, message = "Logged out successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout for user {UserId}", userId);
            return StatusCode(500, new { success = false, message = "Error during logout" });
        }
    }

    // --------------------------------------------------------------
    // GET USER
    // --------------------------------------------------------------
    [HttpGet("user")]
    [Authorize]
    public async Task<IActionResult> GetUser(CancellationToken ct = default)
    {
        var userId = User.FindFirstValue("sub");
        if (string.IsNullOrEmpty(userId))
            return Unauthorized(new { message = "No user ID found" });

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

            _logger.LogWarning("Failed to get access token for user {UserId}: {Error}",
                userId,
                tokenResult.FailedResult?.Error ?? "unknown_error");

            return Unauthorized(new
            {
                message = "Token invalid or expired",
                error = tokenResult.FailedResult?.Error,
                needsReauth = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user info for user {UserId}", userId);
            return StatusCode(500, new { message = "Error retrieving user information" });
        }
    }

    // --------------------------------------------------------------
    // REFRESH TOKEN
    // --------------------------------------------------------------
    [HttpPost("refresh")]
    [Authorize]
    public async Task<IActionResult> RefreshToken(CancellationToken ct = default)
    {
        var userId = User.FindFirstValue("sub");
        _logger.LogDebug("Token refresh requested for user {UserId}", userId);

        try
        {
            var tokenResult = await _tokenManager.GetAccessTokenAsync(User, parameters: null, ct: ct);

            if (!tokenResult.Succeeded || tokenResult.Token is null)
            {
                return Unauthorized(new
                {
                    success = false,
                    error = tokenResult.FailedResult?.Error ?? "refresh_failed",
                    message = "Failed to refresh token. Please login again."
                });
            }

            var token = tokenResult.Token;
            var expiresIn = (int)(token.Expiration - DateTimeOffset.UtcNow).TotalSeconds;

            return Ok(new
            {
                success = true,
                message = "Token refreshed successfully",
                expiresAt = token.Expiration.ToString("o"),
                expiresIn = expiresIn
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing token for user {UserId}", userId);
            return StatusCode(500, new
            {
                success = false,
                error = "server_error",
                message = "An error occurred while refreshing the token"
            });
        }
    }

    // --------------------------------------------------------------
    // CHECK AUTH
    // --------------------------------------------------------------
    [HttpGet("check")]
    [AllowAnonymous]
    public IActionResult CheckAuthentication()
    {
        return Ok(new
        {
            isAuthenticated = User.Identity?.IsAuthenticated == true,
            userId = User.FindFirstValue("sub")
        });
    }

    // --------------------------------------------------------------
    // CLAIMS (DEV)
    // --------------------------------------------------------------
    [HttpGet("claims")]
    [Authorize]
    public IActionResult GetClaims()
    {
        if (!HttpContext.RequestServices
            .GetRequiredService<IWebHostEnvironment>()
            .IsDevelopment())
        {
            return NotFound();
        }

        var claims = User.Claims.Select(c => new { c.Type, c.Value }).ToList();
        return Ok(new
        {
            identity = User.Identity?.Name,
            isAuthenticated = User.Identity?.IsAuthenticated,
            claims
        });
    }

    // --------------------------------------------------------------
    // HELPERS
    // --------------------------------------------------------------
    private string GetSafeReturnUrl(string? returnUrl)
    {
        const string defaultUrl = "/";
        if (string.IsNullOrWhiteSpace(returnUrl) || !Url.IsLocalUrl(returnUrl))
            return defaultUrl;
        return returnUrl;
    }
}
