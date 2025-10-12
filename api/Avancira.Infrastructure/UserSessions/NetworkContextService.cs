using System;
using Avancira.Application.UserSessions.Services;
using Microsoft.AspNetCore.Http;

namespace Avancira.Infrastructure.UserSessions;

public sealed class NetworkContextService : INetworkContextService
{
    private const string DeviceIdCookieName = "device_id";
    private readonly IHttpContextAccessor _httpContextAccessor;

    public NetworkContextService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? GetIpAddress()
    {
        var context = _httpContextAccessor.HttpContext
            ?? throw new InvalidOperationException("HTTP context not available");

        return context.Connection.RemoteIpAddress?.ToString();
    }

    public string GetOrCreateDeviceId()
    {
        var context = _httpContextAccessor.HttpContext
            ?? throw new InvalidOperationException("HTTP context not available");

        if (context.Request.Cookies.TryGetValue(DeviceIdCookieName, out var deviceId) && !string.IsNullOrWhiteSpace(deviceId))
        {
            return deviceId;
        }

        var generated = $"gen-{Guid.NewGuid():N}";

        var options = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Path = "/",
            Expires = DateTimeOffset.UtcNow.AddYears(1)
        };

        context.Response.Cookies.Append(DeviceIdCookieName, generated, options);

        return generated;
    }
}
