namespace Avancira.BFF.Controllers;

using System.Security.Claims;
using Avancira.BFF.Configuration;
using Avancira.BFF.Services;
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
    private readonly AuthServerClient _authClient;
    private readonly BffSettings _settings;
    private readonly IHttpClientFactory _factory;
    public BffAuthenticationController(
        ILogger<BffAuthenticationController> logger,
        IUserTokenManager tokenManager,
        AuthServerClient authClient,
        IHttpClientFactory factory,
        BffSettings settings)
    {
        _logger = logger;
        _tokenManager = tokenManager;
        _authClient = authClient;
        _factory = factory;
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


    [HttpGet("validate")]
    [AllowAnonymous]
    [Produces("application/json")]
    public IActionResult ValidateSession()
    {
        var isAuthenticated = User.Identity?.IsAuthenticated ?? false;
        if (!isAuthenticated)
        {
            _logger.LogDebug("ValidateSession: user not authenticated.");
            return Ok(new { isAuthenticated = false });
        }

        return Ok(new { isAuthenticated = true });
    }


    [HttpGet("user")]
    [Authorize]
    public async Task<IActionResult> GetUser(CancellationToken ct)
    {
        var user = await _authClient.GetUserInfoAsync(ct);
        if (user is null)
        {
            _logger.LogWarning("Failed to fetch user info for {Sub}", User.FindFirstValue("sub"));
            return Ok(new { isAuthenticated = true, user = (object?)null });
        }

        return Ok(new
        {
            isAuthenticated = true,
            user
        });
    }

    // Add this to test endpoint connectivity
    [HttpGet("test-auth")]
    [AllowAnonymous]
    public async Task<IActionResult> TestAuthConnection()
    {
        try
        {
            var client = _factory.CreateClient("auth-client");
            var response = await client.GetAsync(".well-known/openid-configuration",
                HttpCompletionOption.ResponseHeadersRead);
            return Ok(new
            {
                status = response.StatusCode,
                baseAddress = client.BaseAddress,
                reachable = response.IsSuccessStatusCode
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
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
