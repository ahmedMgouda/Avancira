using System.Security.Claims;
using Avancira.BFF.Configuration;
using Avancira.BFF.Services;
using Duende.AccessTokenManagement.OpenIdConnect;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Avancira.BFF.Controllers;

[ApiController]
[Route("bff/auth")]
public class AuthController : ControllerBase
{
    private readonly ILogger<AuthController> _logger;
    private readonly IUserTokenManager _tokenManager;
    private readonly ApiClient _apiClient;
    private readonly BffSettings _settings;

    public AuthController(
        ILogger<AuthController> logger,
        IUserTokenManager tokenManager,
        ApiClient apiClient,
        BffSettings settings)
    {
        _logger = logger;
        _tokenManager = tokenManager;
        _apiClient = apiClient;
        _settings = settings;
    }

    // ═══════════════════════════════════════════════════════════════════
    // GET /bff/auth/login - Initiates login flow
    // ═══════════════════════════════════════════════════════════════════
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

    // ═══════════════════════════════════════════════════════════════════
    // GET /bff/auth/user - Returns user authentication status and profile
    // 👇 THIS IS THE KEY ENDPOINT CALLED BY YOUR SPA
    // ═══════════════════════════════════════════════════════════════════
    [HttpGet("user")]
    [Produces("application/json")]
    [AllowAnonymous]
    public async Task<IActionResult> GetUser(CancellationToken ct)
    {
        var isAuthenticated = User.Identity?.IsAuthenticated ?? false;

        if (!isAuthenticated)
        {
            _logger.LogDebug("User not authenticated");
            return Ok(new { isAuthenticated = false });
        }

        var userId = User.FindFirstValue("sub");
        _logger.LogDebug("Fetching profile for authenticated user {UserId}", userId);

        // 👇 Call API to get enriched profile
        var profile = await _apiClient.GetUserProfileAsync(ct);

        if (profile == null)
        {
            _logger.LogWarning("Failed to fetch profile for user {UserId}", userId);
            return Ok(new
            {
                isAuthenticated = true,
                error = "Failed to load user profile"
            });
        }

        // 👇 Return the exact format your SPA expects
        return Ok(new
        {
            isAuthenticated = true,
            userId = profile.UserId,
            firstName = profile.FirstName,
            lastName = profile.LastName,
            fullName = profile.FullName,
            profileImageUrl = profile.ProfileImageUrl,
            roles = profile.Roles,
            activeProfile = profile.ActiveProfile,
            hasAdminAccess = profile.HasAdminAccess,
            tutorProfile = profile.TutorProfile,
            studentProfile = profile.StudentProfile
        });
    }

    // ═══════════════════════════════════════════════════════════════════
    // POST /bff/auth/logout - Initiates logout flow
    // ═══════════════════════════════════════════════════════════════════
    [HttpPost("logout"), HttpGet("logout")]
    [Authorize]
    public IActionResult Logout()
    {
        var userId = User.FindFirstValue("sub");
        var sessionId = User.FindFirstValue("sid");

        _logger.LogInformation("Logout initiated - UserId: {UserId}, SessionId: {SessionId}",
            userId, sessionId);

        // Optional: revoke refresh token
        try
        {
            _tokenManager.RevokeRefreshTokenAsync(User).Wait();
            _logger.LogInformation("Refresh token revoked for {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to revoke refresh token for {UserId}", userId);
        }

        var props = new AuthenticationProperties
        {
            RedirectUri = _settings.Auth.DefaultRedirectUrl
        };

        return SignOut(
            props,
            CookieAuthenticationDefaults.AuthenticationScheme,
            OpenIdConnectDefaults.AuthenticationScheme);
    }

    // ═══════════════════════════════════════════════════════════════════
    // PRIVATE HELPER: Validates return URLs
    // ═══════════════════════════════════════════════════════════════════
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
