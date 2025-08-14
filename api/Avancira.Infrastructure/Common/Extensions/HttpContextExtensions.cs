using Microsoft.AspNetCore.Http;
using System.Security.Cryptography;
using System.Text;

namespace Avancira.Infrastructure.Common.Extensions;

public static class HttpContextExtensions
{
    // Extension method for HttpContext to retrieve IP address
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
        var userAgent = context.Request.Headers["User-Agent"].FirstOrDefault() ?? string.Empty;
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(userAgent);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }
}
