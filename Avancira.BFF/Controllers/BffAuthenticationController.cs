namespace Avancira.BFF.Controllers;

using System.Security.Claims;
using Avancira.BFF.Configuration;
using Avancira.BFF.Services;
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


    [HttpGet("user")]
    [Produces("application/json")]
    [AllowAnonymous]
    public async Task<IActionResult> GetUser(CancellationToken ct)
    {
        var isAuthenticated = User.Identity?.IsAuthenticated ?? false;

        if (!isAuthenticated)
        {
            _logger.LogDebug("User not authenticated.");
            return Ok(new { isAuthenticated = false });
        }

        var sub = User.FindFirstValue("sub");
        var user = await _authClient.GetUserInfoAsync(ct);

        if (user is null)
        {
            _logger.LogWarning("Failed to fetch user info for {Sub}", sub);
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
    // FIXED: Initiates logout flow (doesn't complete it)
    //
    // CORRECT FLOW:
    // 1. SPA calls this endpoint (POST /bff/auth/logout)
    // 2. This endpoint revokes tokens and initiates OIDC sign-out
    // 3. Browser is redirected to Auth server logout endpoint
    // 4. Auth server clears its identity cookie
    // 5. Auth server redirects back to BFF callback (/bff/signout-callback-oidc)
    // 6. BFF clears its cookie and redirects to SPA
    // ═══════════════════════════════════════════════════════════════════════
    [HttpPost("logout"), HttpGet("logout")]
    [Authorize]
    public IActionResult Logout()
    {
        var userId = User.FindFirstValue("sub");
        var sessionId = User.FindFirstValue("sid");

        _logger.LogInformation("Logout initiated - UserId: {UserId}, SessionId: {SessionId}", userId, sessionId);

        // Optional: revoke refresh token (safe to skip)
        try
        {
            _tokenManager.RevokeRefreshTokenAsync(User).Wait();
            _logger.LogInformation("Refresh token revoked for {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to revoke refresh token for {UserId}", userId);
        }

        // 👇 Return a SignOutResult instead of calling SignOutAsync
        var props = new AuthenticationProperties
        {
            RedirectUri = "https://localhost:4200/" // final SPA page after full logout
        };

        return SignOut(
            props,
            CookieAuthenticationDefaults.AuthenticationScheme,
            OpenIdConnectDefaults.AuthenticationScheme);
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
