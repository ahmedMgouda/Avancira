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
    private readonly ISessionCacheService _sessionCacheService;
    private readonly ITokenManagementService _tokenManagementService;
    private readonly INetworkContextService _networkContextService;

    public BffAuthenticationController(
        ILogger<BffAuthenticationController> logger,
        IUserTokenManager userTokenManager,
        ISessionCacheService sessionCacheService,
        ITokenManagementService tokenManagementService,
        INetworkContextService networkContextService)
    {
        _logger = logger;
        _userTokenManager = userTokenManager;
        _sessionCacheService = sessionCacheService;
        _tokenManagementService = tokenManagementService;
        _networkContextService = networkContextService;
    }

    /// <summary>
    /// Initiates login flow with OpenID Connect
    /// </summary>
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

    /// <summary>
    /// Callback after successful authentication at auth server
    /// FIXED: Properly caches access token and validates token expiry
    /// </summary>
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
            {
                _logger.LogError("Login callback: No subject claim found");
                return Redirect(GetSafeReturnUrl(null));
            }

            // Get session ID from token (created at auth server)
            var sessionIdClaim = User.FindFirstValue(AuthConstants.Claims.SessionId);
            if (string.IsNullOrEmpty(sessionIdClaim) || !Guid.TryParse(sessionIdClaim, out var sessionId))
            {
                _logger.LogError(
                    "Login callback: No valid SessionId claim in token for user {UserId}",
                    userId);
                return Redirect(GetSafeReturnUrl(null));
            }

            // Get device info for tracking
            var deviceId = _networkContextService.GetOrCreateDeviceId();
            var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            // Create session in BFF database (mirrors auth server session)
            var session = await _sessionCacheService.CreateSessionAsync(
                userId,
                deviceId,
                deviceName: null,
                userAgent: userAgent,
                ipAddress: ipAddress,
                ct);

            _logger.LogInformation(
                "BFF session created for user {UserId}: {SessionId}, auth session: {AuthSessionId}",
                userId,
                session.Id,
                sessionId);

            // Get access token from auth server context
            var accessToken = await HttpContext.GetTokenAsync("access_token");
            var expiresAtStr = await HttpContext.GetTokenAsync("expires_at");
            var refreshToken = await HttpContext.GetTokenAsync("refresh_token");

            // FIXED: Validate token was received
            if (string.IsNullOrWhiteSpace(accessToken))
            {
                _logger.LogError(
                    "Login callback: No access token received from auth server for user {UserId}",
                    userId);
                return Redirect(GetSafeReturnUrl(null));
            }

            // FIXED: Parse and validate token expiry
            if (!DateTimeOffset.TryParse(expiresAtStr, out var accessTokenExpiresAt))
            {
                _logger.LogError(
                    "Login callback: Invalid token expiration for user {UserId}",
                    userId);
                return Redirect(GetSafeReturnUrl(null));
            }

            // Validate token is not already expired
            if (accessTokenExpiresAt <= DateTimeOffset.UtcNow)
            {
                _logger.LogError(
                    "Login callback: Received already-expired token for user {UserId}",
                    userId);
                return Redirect(GetSafeReturnUrl(null));
            }

            // Cache the access token immediately
            await _tokenManagementService.CacheAccessTokenAsync(
                userId,
                session.Id,
                accessToken,
                accessTokenExpiresAt,
                ct);

            _logger.LogInformation(
                "Access token cached for user {UserId}, expires at {ExpiresAt}",
                userId,
                accessTokenExpiresAt);

            // FIXED: Store refresh token reference ID if present
            // This links the session to the auth server's refresh token
            if (!string.IsNullOrWhiteSpace(refreshToken))
            {
                // Calculate refresh token expiry (usually 14 days for offline_access scope)
                // For standard OpenIddict: 14 days default
                var refreshTokenExpiresAt = DateTimeOffset.UtcNow.AddDays(14);

                try
                {
                    await _sessionCacheService.UpdateRefreshTokenAsync(
                        session.Id,
                        refreshToken,  // Store the reference ID
                        refreshTokenExpiresAt,
                        accessTokenExpiresAt,
                        ct);

                    _logger.LogInformation(
                        "Refresh token reference stored for session {SessionId}",
                        session.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to store refresh token reference for session {SessionId}", session.Id);
                    // Don't fail login if this fails - token caching already succeeded
                }
            }

            // Store session ID in HttpContext items for this request
            // (will be available in any downstream middleware/handlers)
            HttpContext.Items["SessionId"] = session.Id.ToString();

            _logger.LogInformation(
                "Login successful for user {UserId}, session {SessionId}",
                userId,
                session.Id);

            return Redirect(GetSafeReturnUrl(returnUrl));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in login callback");
            return Redirect(GetSafeReturnUrl(null));
        }
    }

    /// <summary>
    /// Logout endpoint - revokes session and clears authentication
    /// FIXED: Revokes refresh token at auth server AND local session
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout(CancellationToken ct = default)
    {
        var userId = User.FindFirstValue("sub");
        var sessionIdClaim = User.FindFirstValue(AuthConstants.Claims.SessionId);

        _logger.LogInformation("Logout for user {UserId}, session {SessionId}", userId, sessionIdClaim);

        try
        {
            // FIXED: Revoke session locally
            if (Guid.TryParse(sessionIdClaim, out var sessionId))
            {
                await _sessionCacheService.RevokeSessionAsync(
                    sessionId,
                    "User logout",
                    ct);

                _logger.LogInformation("Session {SessionId} revoked", sessionId);
            }

            // FIXED: Revoke refresh token at auth server
            // This prevents token reuse even if attacker has the token
            try
            {
                await _userTokenManager.RevokeRefreshTokenAsync(User, ct: ct);
                _logger.LogInformation("Refresh token revoked at auth server for user {UserId}", userId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to revoke refresh token at auth server");
                // Continue with logout even if revocation fails
            }

            // Clear authentication cookies
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme);

            _logger.LogInformation("User {UserId} logged out successfully", userId);

            return Ok(new { success = true, message = "Logged out successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout for user {UserId}", userId);
            return StatusCode(500, new { success = false, message = "Error during logout" });
        }
    }

    /// <summary>
    /// Get current user information
    /// FIXED: Validates session is active before returning user info
    /// </summary>
    [HttpGet("user")]
    [Authorize]
    public async Task<IActionResult> GetUser(CancellationToken ct = default)
    {
        var userId = User.FindFirstValue("sub");
        var sessionIdClaim = User.FindFirstValue(AuthConstants.Claims.SessionId);

        if (string.IsNullOrWhiteSpace(userId))
        {
            _logger.LogWarning("GetUser: No user ID in claims");
            return Unauthorized(new { message = "No user ID in claims" });
        }

        // FIXED: Parse session ID and validate it exists
        if (!Guid.TryParse(sessionIdClaim, out var sessionId))
        {
            _logger.LogWarning("GetUser: Invalid session ID for user {UserId}", userId);
            return Unauthorized(new { message = "Invalid session" });
        }

        try
        {
            // FIXED: Verify session is still active (not revoked/expired)
            var isSessionValid = await _sessionCacheService.IsSessionValidAsync(sessionId, ct);
            if (!isSessionValid)
            {
                _logger.LogWarning(
                    "GetUser: Session {SessionId} is no longer valid for user {UserId}",
                    sessionId,
                    userId);

                return Unauthorized(new
                {
                    message = "Session has been revoked or expired",
                    needsReauth = true
                });
            }

            // Update session activity (lazy - only updates DB if stale)
            await _sessionCacheService.UpdateActivityLazyAsync(sessionId, ct);

            // Get access token from cache
            var accessToken = await _tokenManagementService.GetAccessTokenAsync(
                userId,
                sessionId,
                ct);

            // FIXED: Validate token is available and valid
            if (!accessToken.IsSuccess)
            {
                _logger.LogWarning(
                    "GetUser: No valid access token for user {UserId}: {Error}",
                    userId,
                    accessToken.Error);

                return Unauthorized(new
                {
                    message = accessToken.Error ?? "Access token not available",
                    needsRefresh = true
                });
            }

            var roles = User.FindAll("role");

            return Ok(new
            {
                isAuthenticated = true,
                sub = userId,
                name = User.FindFirstValue("name"),
                givenName = User.FindFirstValue("given_name"),
                familyName = User.FindFirstValue("family_name"),
                email = User.FindFirstValue("email"),
                emailVerified = bool.TryParse(User.FindFirstValue("email_verified"), out var verified) && verified,
                roles = roles.Select(r => r.Value),
                tokenExpiresAt = accessToken.ExpiresAt.ToString("o"),
                tokenExpiresIn = (int)accessToken.ExpiresIn.TotalSeconds,
                sessionId = sessionIdClaim,
                needsRefresh = accessToken.NeedsRefresh
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user info for user {UserId}", userId);
            return StatusCode(500, new { message = "Error retrieving user information" });
        }
    }

    /// <summary>
    /// Refresh access token
    /// FIXED: Validates session before refresh and updates token cache
    /// </summary>
    [HttpPost("refresh")]
    [Authorize]
    public async Task<IActionResult> RefreshToken(CancellationToken ct = default)
    {
        var userId = User.FindFirstValue("sub");
        var sessionIdClaim = User.FindFirstValue(AuthConstants.Claims.SessionId);

        _logger.LogDebug("Refresh token requested for user {UserId}", userId);

        try
        {
            // FIXED: Validate session ID exists
            if (!Guid.TryParse(sessionIdClaim, out var sessionId))
            {
                _logger.LogWarning("Refresh: Invalid session ID for user {UserId}", userId);
                return Unauthorized(new
                {
                    success = false,
                    error = "invalid_session",
                    message = "Session information is missing or invalid"
                });
            }

            // FIXED: Verify session is still active
            var isSessionValid = await _sessionCacheService.IsSessionValidAsync(sessionId, ct);
            if (!isSessionValid)
            {
                _logger.LogWarning("Refresh: Session {SessionId} no longer valid for user {UserId}", sessionId, userId);
                return Unauthorized(new
                {
                    success = false,
                    error = "session_revoked",
                    message = "Session has been revoked. Please login again."
                });
            }

            // Request new tokens from auth server
            var tokenResult = await _userTokenManager.GetAccessTokenAsync(User, ct: ct);

            if (!tokenResult.WasSuccessful(out var token, out var failure))
            {
                _logger.LogWarning(
                    "Refresh failed for user {UserId}: {Error}",
                    userId,
                    failure?.Error ?? "unknown_error");

                return Unauthorized(new
                {
                    success = false,
                    error = failure?.Error ?? "unknown_error",
                    message = "Failed to refresh token. Please login again."
                });
            }

            // FIXED: Validate we got a token back
            if (string.IsNullOrWhiteSpace(token?.AccessToken))
            {
                _logger.LogError("Refresh: No access token returned from auth server for user {UserId}", userId);
                return Unauthorized(new
                {
                    success = false,
                    error = "no_token",
                    message = "Failed to obtain access token"
                });
            }

            // FIXED: Cache new token immediately
            await _tokenManagementService.CacheAccessTokenAsync(
                userId,
                sessionId,
                token.AccessToken,
                token.Expiration,
                ct);

            // Update session activity
            await _sessionCacheService.UpdateActivityLazyAsync(sessionId, ct);

            var expiresIn = (int)(token.Expiration - DateTimeOffset.UtcNow).TotalSeconds;

            _logger.LogInformation(
                "Token refreshed successfully for user {UserId}, expires in {Seconds} seconds",
                userId,
                expiresIn);

            return Ok(new
            {
                success = true,
                message = "Token refreshed successfully",
                accessToken = token.AccessToken,
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

    /// <summary>
    /// Check authentication status (health check endpoint)
    /// </summary>
    [HttpGet("check")]
    public IActionResult CheckAuthentication()
    {
        return User.Identity?.IsAuthenticated == true
            ? Ok(new { isAuthenticated = true })
            : Unauthorized(new { isAuthenticated = false });
    }

    /// <summary>
    /// Get list of all active sessions for current user
    /// Used for device management UI
    /// </summary>
    [HttpGet("sessions")]
    [Authorize]
    public async Task<IActionResult> GetSessions(CancellationToken ct = default)
    {
        var userId = User.FindFirstValue("sub");
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        try
        {
            var sessions = await _sessionCacheService.GetUserSessionsAsync(userId, ct);

            return Ok(sessions.Select(s => new
            {
                id = s.Id,
                deviceName = s.DeviceName,
                deviceId = s.DeviceId,
                userAgent = s.UserAgent,
                ipAddress = s.IpAddress,
                status = s.Status.ToString(),
                createdAt = s.CreatedAt,
                lastActivityAt = s.LastActivityAt,
                refreshTokenExpiresAt = s.RefreshTokenExpiresAt,
                isCurrent = s.Id.ToString() == User.FindFirstValue(AuthConstants.Claims.SessionId)
            }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user sessions for user {UserId}", userId);
            return StatusCode(500, new { message = "Error retrieving sessions" });
        }
    }

    /// <summary>
    /// Revoke a specific session (logout from device)
    /// </summary>
    [HttpPost("sessions/{sessionId}/revoke")]
    [Authorize]
    public async Task<IActionResult> RevokeSession(
        [FromRoute] Guid sessionId,
        CancellationToken ct = default)
    {
        var userId = User.FindFirstValue("sub");
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        try
        {
            var session = await _sessionCacheService.GetSessionAsync(sessionId, ct);
            if (session?.UserId != userId)
            {
                _logger.LogWarning(
                    "User {UserId} attempted to revoke session {SessionId} belonging to different user",
                    userId,
                    sessionId);
                return Forbid();
            }

            await _sessionCacheService.RevokeSessionAsync(
                sessionId,
                "User revoked session",
                ct);

            _logger.LogInformation("Session {SessionId} revoked by user {UserId}", sessionId, userId);

            return Ok(new { success = true, message = "Session revoked successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking session {SessionId}", sessionId);
            return StatusCode(500, new { message = "Error revoking session" });
        }
    }

    /// <summary>
    /// Logout from all other devices (keep current session active)
    /// </summary>
    [HttpPost("logout-other-devices")]
    [Authorize]
    public async Task<IActionResult> LogoutOtherDevices(CancellationToken ct = default)
    {
        var userId = User.FindFirstValue("sub");
        var sessionIdClaim = User.FindFirstValue(AuthConstants.Claims.SessionId);

        if (string.IsNullOrWhiteSpace(userId) || !Guid.TryParse(sessionIdClaim, out var currentSessionId))
        {
            return Unauthorized();
        }

        try
        {
            await _sessionCacheService.RevokeOtherSessionsAsync(
                userId,
                currentSessionId,
                "User logged out from other devices",
                ct);

            _logger.LogInformation(
                "All other sessions revoked for user {UserId}, kept session {SessionId}",
                userId,
                currentSessionId);

            return Ok(new { success = true, message = "Logged out from all other devices" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging out other devices for user {UserId}", userId);
            return StatusCode(500, new { message = "Error logging out other devices" });
        }
    }

    /// <summary>
    /// Validate return URL is safe (local only, prevents open redirects)
    /// </summary>
    private string GetSafeReturnUrl(string? returnUrl)
    {
        const string defaultUrl = "/";

        if (string.IsNullOrWhiteSpace(returnUrl))
            return defaultUrl;

        if (!Url.IsLocalUrl(returnUrl))
        {
            _logger.LogWarning("Blocked non-local return URL: {ReturnUrl}", returnUrl);
            return defaultUrl;
        }

        return returnUrl;
    }
}