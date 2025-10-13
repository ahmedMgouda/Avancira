using System.Security.Claims;
using Avancira.Application.Auth;
using Avancira.BFF.Services;
using Avancira.Infrastructure.Auth;
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
    private readonly IUserTokenManager _userTokenManager;
    private readonly ISessionManagementService _sessionManagementService;
    private readonly ITokenManagementService _tokenManagementService;
    private readonly INetworkContextService _networkContextService;

    public BffAuthenticationController(
        ILogger<BffAuthenticationController> logger,
        IUserTokenManager userTokenManager,
        ISessionManagementService sessionManagementService,
        ITokenManagementService tokenManagementService,
        INetworkContextService networkContextService)
    {
        _logger = logger;
        _userTokenManager = userTokenManager;
        _sessionManagementService = sessionManagementService;
        _tokenManagementService = tokenManagementService;
        _networkContextService = networkContextService;
    }

    // ------------------------------------------------------------
    // LOGIN
    // ------------------------------------------------------------
    [HttpGet("login")]
    [AllowAnonymous]
    public IActionResult Login([FromQuery] string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return Redirect(GetSafeReturnUrl(returnUrl));

        var props = new AuthenticationProperties
        {
            RedirectUri = Url.Action(nameof(LoginCallback), new { returnUrl }) ?? "/"
        };

        _logger.LogInformation("Starting login with returnUrl={ReturnUrl}", returnUrl ?? "/");
        return Challenge(props, OpenIdConnectDefaults.AuthenticationScheme);
    }

    // ------------------------------------------------------------
    // LOGIN CALLBACK
    // ------------------------------------------------------------
    [HttpGet("login-callback")]
    [AllowAnonymous]
    public async Task<IActionResult> LoginCallback(
        [FromQuery] string? returnUrl = null,
        CancellationToken ct = default)
    {
        try
        {
            var userId = User.FindFirstValue("sub");
            if (string.IsNullOrEmpty(userId))
                return Redirect(GetSafeReturnUrl(null));

            var deviceId = _networkContextService.GetOrCreateDeviceId();
            var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

            var session = await _sessionManagementService.CreateSessionAsync(
                userId, deviceId, null, userAgent, ip, ct);

            var accessToken = await HttpContext.GetTokenAsync("access_token");
            var refreshToken = await HttpContext.GetTokenAsync("refresh_token");
            var expiresAtStr = await HttpContext.GetTokenAsync("expires_at");
            DateTimeOffset.TryParse(expiresAtStr, out var expiresAt);

            if (string.IsNullOrWhiteSpace(accessToken))
                return Redirect(GetSafeReturnUrl(null));

            await _tokenManagementService.CacheAccessTokenAsync(userId, session.Id, accessToken, expiresAt, ct);

            if (!string.IsNullOrWhiteSpace(refreshToken))
            {
                var refreshTokenId = User.FindFirstValue("refresh_token_id") ?? refreshToken;
                await _sessionManagementService.UpdateSessionRefreshTokenAsync(session.Id, refreshTokenId, expiresAt, ct);
            }

            HttpContext.Items["SessionId"] = session.Id.ToString();

            _logger.LogInformation("Login successful for {UserId}, session {SessionId}", userId, session.Id);
            return Redirect(GetSafeReturnUrl(returnUrl));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login callback error");
            return Redirect(GetSafeReturnUrl(null));
        }
    }

    // ------------------------------------------------------------
    // LOGOUT
    // ------------------------------------------------------------
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout(CancellationToken ct = default)
    {
        var userId = User.FindFirstValue("sub");
        var sessionIdClaim = User.FindFirstValue(AuthConstants.Claims.SessionId);

        _logger.LogInformation("Logout for user {UserId}, session {SessionId}", userId, sessionIdClaim);

        try
        {
            if (Guid.TryParse(sessionIdClaim, out var sessionId))
                await _sessionManagementService.RevokeSessionAsync(sessionId, "User logout", false, ct);

            await _userTokenManager.RevokeRefreshTokenAsync(User, ct: ct);

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme);

            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
            return StatusCode(500, new { success = false, message = "Logout failed" });
        }
    }

    // ------------------------------------------------------------
    // USER INFO
    // ------------------------------------------------------------
    [HttpGet("user")]
    [Authorize]
    public async Task<IActionResult> GetUser(CancellationToken ct = default)
    {
        var userId = User.FindFirstValue("sub");
        var sessionIdClaim = User.FindFirstValue(AuthConstants.Claims.SessionId);

        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        if (Guid.TryParse(sessionIdClaim, out var sessionId))
        {
            if (!await _sessionManagementService.IsSessionValidAsync(sessionId, ct))
                return Unauthorized(new { message = "Session revoked" });

            await _sessionManagementService.UpdateSessionActivityAsync(sessionId, ct);
        }

        var access = await _tokenManagementService.GetAccessTokenAsync(userId, sessionId, ct);

        return Ok(new
        {
            isAuthenticated = true,
            sub = userId,
            name = User.FindFirstValue("name"),
            email = User.FindFirstValue("email"),
            roles = User.FindAll("role").Select(r => r.Value),
            tokenExpiresAt = access.ExpiresAt,
            tokenExpiresIn = (int)access.ExpiresIn.TotalSeconds,
            sessionId = sessionIdClaim
        });
    }

    // ------------------------------------------------------------
    // REFRESH TOKEN
    // ------------------------------------------------------------
    [HttpPost("refresh")]
    [Authorize]
    public async Task<IActionResult> RefreshToken(CancellationToken ct = default)
    {
        var userId = User.FindFirstValue("sub");
        var sessionIdClaim = User.FindFirstValue(AuthConstants.Claims.SessionId);

        if (!Guid.TryParse(sessionIdClaim, out var sessionId))
            return Unauthorized(new { error = "invalid_session" });

        if (!await _sessionManagementService.IsSessionValidAsync(sessionId, ct))
            return Unauthorized(new { error = "session_revoked" });

        var tokenResult = await _userTokenManager.GetAccessTokenAsync(User, ct: ct);

        if (!tokenResult.WasSuccessful(out var token, out var failure))
        {
            _logger.LogWarning("Refresh failed for {UserId}: {Error}", userId, failure?.Error ?? "unknown_error");
            return Unauthorized(new { error = failure?.Error ?? "unknown_error" });
        }

        await _tokenManagementService.CacheAccessTokenAsync(
            userId, sessionId, token.AccessToken, token.Expiration, ct);

        await _sessionManagementService.UpdateSessionActivityAsync(sessionId, ct);

        return Ok(new
        {
            success = true,
            accessToken = token.AccessToken,
            expiresAt = token.Expiration.ToString("o"),
            expiresIn = (int)(token.Expiration - DateTimeOffset.UtcNow).TotalSeconds
        });
    }

    // ------------------------------------------------------------
    // CHECK AUTH
    // ------------------------------------------------------------
    [HttpGet("check")]
    public IActionResult CheckAuthentication()
    {
        return User.Identity?.IsAuthenticated == true
            ? Ok(new { isAuthenticated = true })
            : Unauthorized(new { isAuthenticated = false });
    }

    // ------------------------------------------------------------
    // HELPERS
    // ------------------------------------------------------------
    private string GetSafeReturnUrl(string? returnUrl)
    {
        const string defaultUrl = "/";
        if (string.IsNullOrWhiteSpace(returnUrl) || !Url.IsLocalUrl(returnUrl))
            return defaultUrl;

        return returnUrl;
    }
}
