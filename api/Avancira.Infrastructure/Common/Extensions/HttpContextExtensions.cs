using Microsoft.AspNetCore.Http;

namespace Avancira.Infrastructure.Common.Extensions;

public static class HttpContextExtensions
{
    public static string GetDeviceIdentifier(this HttpContext context)
    {
        var deviceId = context.Request.Cookies["device_id"];
        if (string.IsNullOrEmpty(deviceId))
        {
            deviceId = Guid.NewGuid().ToString();
            context.Response.Cookies.Append("device_id", deviceId, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Expires = DateTime.UtcNow.AddYears(1),
                Path = "/api/auth"
            });
        }

        return deviceId;
    }
}

