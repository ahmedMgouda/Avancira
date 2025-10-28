using Avancira.Application.Auth;
using Microsoft.AspNetCore.Http;

namespace Avancira.Infrastructure.Auth;

/// <summary>
/// Default implementation using HttpContext.
/// </summary>
public class NetworkContextService : INetworkContextService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private const string DeviceIdCookieName = ".Avancira.DeviceId";

    public NetworkContextService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string GetOrCreateDeviceId()
    {
        var context = _httpContextAccessor.HttpContext;
        if (context is null)
            return Guid.NewGuid().ToString();

        if (context.Request.Cookies.TryGetValue(DeviceIdCookieName, out var existingId) &&
            !string.IsNullOrWhiteSpace(existingId))
        {
            return existingId;
        }

        var deviceId = Guid.NewGuid().ToString();

        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None, // ✅ important for cross-site SPA/BFF
            Expires = DateTimeOffset.UtcNow.AddDays(90), // extend to 3 months
            Path = "/"
        };

        context.Response.Cookies.Append(DeviceIdCookieName, deviceId, cookieOptions);
        return deviceId;
    }

    public string GetClientIpAddress()
    {
        var context = _httpContextAccessor.HttpContext;
        if (context is null)
            return "unknown";

        // Prefer forwarded IP header (proxy-aware)
        if (context.Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor))
        {
            var ip = forwardedFor.ToString().Split(',').FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(ip))
                return ip.Trim();
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    public string GetUserAgent()
    {
        var context = _httpContextAccessor.HttpContext;
        return context?.Request.Headers["User-Agent"].ToString() ?? "unknown";
    }
}
