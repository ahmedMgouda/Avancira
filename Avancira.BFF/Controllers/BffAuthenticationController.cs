using System.Security.Claims;
using Avancira.BFF.Services;
using IdentityModel.AspNetCore.AccessTokenManagement;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Avancira.BFF.Controllers;

[ApiController]
[Route("bff/auth")]
public class BffAuthenticationController : ControllerBase
{
    private readonly ILogger<BffAuthenticationController> _logger;
    private readonly IUserAccessTokenManagementService _userTokenManagementService;
    private readonly ITokenManagementService _tokenSnapshotService;

    public BffAuthenticationController(
        ILogger<BffAuthenticationController> logger,
        IUserAccessTokenManagementService userTokenManagementService,
        ITokenManagementService tokenSnapshotService)
    {
        _logger = logger;
        _userTokenManagementService = userTokenManagementService;
        _tokenSnapshotService = tokenSnapshotService;
    }

    [HttpGet("login")]
    public IActionResult Login([FromQuery] string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            var safeUrl = GetSafeReturnUrl(returnUrl);
            _logger.LogDebug("User already authenticated, redirecting to {ReturnUrl}", safeUrl);
            return Redirect(safeUrl);
        }

        var properties = new AuthenticationProperties
        {
            RedirectUri = Url.Action(nameof(LoginCallback), new { returnUrl }) ?? "/"
        };

        properties.Items["scheme"] = OpenIdConnectDefaults.AuthenticationScheme;

        _logger.LogInformation("Starting login flow, returnUrl={ReturnUrl}", returnUrl ?? "/");

        return Challenge(properties, OpenIdConnectDefaults.AuthenticationScheme);
    }

    [HttpGet("login-callback")]
    public async Task<IActionResult> LoginCallback([FromQuery] string? returnUrl = null)
    {
        var snapshot = await _tokenSnapshotService.CaptureAsync(HttpContext);
        _tokenSnapshotService.StoreSessionSnapshot(HttpContext, snapshot);

        _logger.LogInformation(
            "User {UserId} logged in, token expires at {ExpiresAt}",
            User.FindFirstValue("sub"),
            snapshot.ExpiresAt);

        return Redirect(GetSafeReturnUrl(returnUrl));
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        var userId = User.FindFirstValue("sub");
        _logger.LogInformation("User {UserId} logging out", userId);

        _tokenSnapshotService.ClearSession(HttpContext);

        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme);

        return Ok(new { success = true, message = "Logged out successfully" });
    }

    [HttpGet("user")]
    public async Task<IActionResult> GetUser()
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return Ok(new { isAuthenticated = false });
        }

        var snapshot = await _tokenSnapshotService.CaptureAsync(HttpContext);
        _tokenSnapshotService.StoreSessionSnapshot(HttpContext, snapshot);

        var roles = User.FindAll("role").Select(claim => claim.Value).ToArray();

        return Ok(new
        {
            isAuthenticated = true,
            sub = User.FindFirstValue("sub"),
            name = User.FindFirstValue("name"),
            givenName = User.FindFirstValue("given_name"),
            familyName = User.FindFirstValue("family_name"),
            email = User.FindFirstValue("email"),
            emailVerified = bool.TryParse(User.FindFirstValue("email_verified"), out var verified) && verified,
            roles,
            tokenExpiry = snapshot.ExpiresAt?.ToString("o"),
            tokenExpiresIn = snapshot.ExpiresIn?.TotalSeconds
        });
    }

    [HttpGet("session")]
    public IActionResult CheckSession()
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return Unauthorized(new
            {
                isAuthenticated = false,
                message = "Not authenticated"
            });
        }

        var expiresAt = HttpContext.Session.GetString("ExpiresAt");

        return Ok(new
        {
            isAuthenticated = true,
            userId = User.FindFirstValue("sub"),
            expiresAt
        });
    }

    [HttpPost("refresh")]
    [Authorize]
    public async Task<IActionResult> RefreshToken()
    {
        try
        {
            var tokenResult = await _userTokenManagementService.GetUserAccessTokenAsync(User);
            if (!string.IsNullOrEmpty(tokenResult.Error))
            {
                _logger.LogWarning("Token refresh failed for {UserId}: {Error}", User.FindFirstValue("sub"), tokenResult.Error);
                return Unauthorized(new
                {
                    success = false,
                    error = tokenResult.Error,
                    message = "Failed to refresh token. Please login again."
                });
            }

            var snapshot = await _tokenSnapshotService.CaptureAsync(HttpContext);
            _tokenSnapshotService.StoreSessionSnapshot(HttpContext, snapshot);

            return Ok(new
            {
                success = true,
                message = "Token refreshed successfully",
                expiresAt = snapshot.ExpiresAt?.ToString("o"),
                expiresIn = snapshot.ExpiresIn?.TotalSeconds
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing token for user {UserId}", User.FindFirstValue("sub"));
            return StatusCode(500, new
            {
                success = false,
                message = "An error occurred while refreshing the token"
            });
        }
    }

    [HttpGet("check")]
    public IActionResult CheckAuthentication()
    {
        return User.Identity?.IsAuthenticated == true ? Ok() : Unauthorized();
    }

    private string GetSafeReturnUrl(string? returnUrl)
    {
        const string defaultReturnUrl = "/";

        if (string.IsNullOrWhiteSpace(returnUrl))
        {
            return defaultReturnUrl;
        }

        if (!Url.IsLocalUrl(returnUrl))
        {
            _logger.LogWarning("Blocked non-local return URL {ReturnUrl}", returnUrl);
            return defaultReturnUrl;
        }

        return returnUrl;
    }
}
