namespace Avancira.BFF.Controllers;

using System.Security.Claims;
using Avancira.BFF.Configuration;
using Duende.AccessTokenManagement;
using Duende.AccessTokenManagement.OpenIdConnect;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("bff/auth")]
public class BffAuthenticationController : ControllerBase
{
    private readonly ILogger<BffAuthenticationController> _logger;
    private readonly IUserTokenManager _tokenManager;
    private readonly BffSettings _settings;

    public BffAuthenticationController(
        ILogger<BffAuthenticationController> logger,
        IUserTokenManager tokenManager,
        BffSettings settings)
    {
        _logger = logger;
        _tokenManager = tokenManager;
        _settings = settings;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // GET /bff/auth/login
    // Initiates login flow
    // ═══════════════════════════════════════════════════════════════════════
    [HttpGet("login")]
    [AllowAnonymous]
    public IActionResult Login([FromQuery] string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            _logger.LogDebug("User already authenticated, redirecting to {ReturnUrl}",
                returnUrl ?? "/");
            return Redirect(GetSafeReturnUrl(returnUrl));
        }

        var props = new AuthenticationProperties
        {
            RedirectUri = GetSafeReturnUrl(returnUrl),
            IsPersistent = true
        };

        _logger.LogInformation("Login flow initiated");
        return Challenge(props, OpenIdConnectDefaults.AuthenticationScheme);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // GET /bff/auth/user
    // Returns current user info (if authenticated)
    //
    // NOTE: Only returns sub and token expiry.
    // Full user data (name, email, roles) is in the JWT access token
    // which the API receives automatically.
    // ═══════════════════════════════════════════════════════════════════════
    [HttpGet("user")]
    [AllowAnonymous]
    public async Task<IActionResult> GetUser(CancellationToken ct = default)
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return Ok(new { isAuthenticated = false });
        }

        var userId = User.FindFirstValue("sub");
        if (string.IsNullOrEmpty(userId))
        {
            return Ok(new { isAuthenticated = false });
        }

        try
        {
            // Get access token result from Duende (server-side)
            var tokenResult = await _tokenManager.GetAccessTokenAsync(User);

            // New pattern: WasSuccessful(out token, out failure)
            if (!tokenResult.WasSuccessful(out var token, out var failure))
            {
                _logger.LogWarning(
                    "Token retrieval failed for {UserId}: {Error} - {Description}",
                    userId,
                    failure?.Error ?? "unknown",
                    failure?.ErrorDescription ?? "no description"
                );

                return Ok(new { isAuthenticated = false });
            }

            // Compute expiration (avoid negative)
            var expiresIn = Math.Max(0,
                (int)(token.Expiration - DateTimeOffset.UtcNow).TotalSeconds);

            return Ok(new
            {
                isAuthenticated = true,
                sub = userId,
                tokenExpiresIn = expiresIn
                // NOTE: name, email, roles are in JWT (available in API)
                // BFF doesn't need them in cookie
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user info for {UserId}", userId);
            return Ok(new { isAuthenticated = false });
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // POST /bff/auth/logout
    // Logs out current user
    //
    // PROCESS:
    // 1. Revoke refresh token (prevents token refresh)
    // 2. Sign out from cookie scheme (clears BFF cookie)
    // 3. Sign out from OIDC scheme (notifies auth server)
    // ═══════════════════════════════════════════════════════════════════════
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout(CancellationToken ct = default)
    {
        var userId = User.FindFirstValue("sub");
        var sessionId = User.FindFirstValue("sid");

        _logger.LogInformation(
            "Logout initiated - UserId: {UserId}, SessionId: {SessionId}",
            userId,
            sessionId);

        try
        {
            // Step 1: Revoke refresh token (safe, non-critical failure)
            try
            {
                await _tokenManager.RevokeRefreshTokenAsync(User);
                _logger.LogInformation("Refresh token revoked for {UserId}", userId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Failed to revoke refresh token for {UserId}, SessionId: {SessionId}",
                    userId,
                    sessionId);
            }

            // Step 2: Clear BFF cookie
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            // Step 3: Notify OpenID Connect provider
            var props = new AuthenticationProperties
            {
                RedirectUri = _settings.Auth.DefaultRedirectUrl
            };
            await HttpContext.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme, props);

            _logger.LogInformation(
                "User logged out - UserId: {UserId}, SessionId: {SessionId}",
                userId,
                sessionId);

            return Ok(new { success = true, redirectUri = "/" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Logout failed for {UserId}, SessionId: {SessionId}",
                userId,
                sessionId);

            return StatusCode(500, new { success = false, message = "Logout failed" });
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // GET /bff/auth/claims
    // Returns all claims (development only)
    // ═══════════════════════════════════════════════════════════════════════
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
            authenticationType = User.Identity?.AuthenticationType,
            claims
        });
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Validates and sanitizes return URLs
    // Prevents open redirect vulnerabilities
    // ═══════════════════════════════════════════════════════════════════════
    private string GetSafeReturnUrl(string? returnUrl)
    {
        if (string.IsNullOrWhiteSpace(returnUrl))
            return _settings.Auth.DefaultRedirectUrl;

        // Allow local URLs
        if (Url.IsLocalUrl(returnUrl))
            return returnUrl;

        // Check if URL matches allowed origins
        if (Uri.TryCreate(returnUrl, UriKind.Absolute, out var uri))
        {
            var origin = $"{uri.Scheme}://{uri.Authority}";
            if (_settings.Cors.AllowedOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase))
                return returnUrl;
        }

        _logger.LogWarning("Invalid return URL rejected: {ReturnUrl}", returnUrl);
        return _settings.Auth.DefaultRedirectUrl;
    }
}
