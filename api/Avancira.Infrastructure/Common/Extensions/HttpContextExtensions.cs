using Microsoft.AspNetCore.Http;

namespace Avancira.Infrastructure.Common.Extensions;

public static class HttpContextExtensions
{
    public static string GetIpAddress(this HttpContext context)
    {
        string ip = "N/A";
        if (context.Request.Headers.TryGetValue("X-Forwarded-For", out var ipList))
        {
            ip = ipList.FirstOrDefault() ?? "N/A";
        }
        else if (context.Connection.RemoteIpAddress != null)
        {
            ip = context.Connection.RemoteIpAddress.MapToIPv4().ToString();
        }
        return ip;
    }

    public static string GetDeviceIdentifier(this HttpContext context)
    {
        var deviceId = context.Request.Headers["Device-Id"].FirstOrDefault();

        if (string.IsNullOrEmpty(deviceId))
        {
            deviceId = Guid.NewGuid().ToString();
        }

        return deviceId;
    }

    public static string GetUserAgent(this HttpContext context) =>
        context.Request.Headers["User-Agent"].FirstOrDefault() ?? "N/A";

    public static string GetOperatingSystem(this HttpContext context)
    {
        var userAgent = context.GetUserAgent();

        if (userAgent.Contains("Windows", StringComparison.OrdinalIgnoreCase)) return "Windows";
        if (userAgent.Contains("Android", StringComparison.OrdinalIgnoreCase)) return "Android";
        if (userAgent.Contains("iPhone", StringComparison.OrdinalIgnoreCase) || userAgent.Contains("iPad", StringComparison.OrdinalIgnoreCase)) return "iOS";
        if (userAgent.Contains("Mac OS", StringComparison.OrdinalIgnoreCase) || userAgent.Contains("Macintosh", StringComparison.OrdinalIgnoreCase)) return "macOS";
        if (userAgent.Contains("Linux", StringComparison.OrdinalIgnoreCase)) return "Linux";

        return "Unknown";
    }
}