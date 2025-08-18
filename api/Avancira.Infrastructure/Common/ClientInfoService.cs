using Avancira.Application.Common;
using Avancira.Application.Catalog;
using Avancira.Infrastructure.Common.Extensions;
using Microsoft.AspNetCore.Http;

namespace Avancira.Infrastructure.Common;

public class ClientInfoService : IClientInfoService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IGeolocationService _geolocationService;

    public ClientInfoService(IHttpContextAccessor httpContextAccessor, IGeolocationService geolocationService)
    {
        _httpContextAccessor = httpContextAccessor;
        _geolocationService = geolocationService;
    }

    public async Task<ClientInfo> GetClientInfoAsync()
    {
        var context = _httpContextAccessor.HttpContext ?? throw new InvalidOperationException("No HttpContext available");

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

        var ip = context.GetIpAddress();
        var userAgent = context.GetUserAgent();
        var operatingSystem = context.GetOperatingSystem();

        var (country, city) = await _geolocationService.GetLocationFromIpAsync(ip);

        return new ClientInfo
        {
            DeviceId = deviceId,
            IpAddress = ip,
            UserAgent = userAgent,
            OperatingSystem = operatingSystem,
            Country = country,
            City = city
        };
    }
}
