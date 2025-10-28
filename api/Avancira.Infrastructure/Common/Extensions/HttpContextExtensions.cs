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

    public static string GetUserAgent(this HttpContext context) =>
        context.Request.Headers["User-Agent"].FirstOrDefault() ?? "N/A";
  
}

