using Avancira.Application.Common;
using Avancira.Application.Catalog;
using Avancira.Infrastructure.Common.Extensions;
using Microsoft.AspNetCore.Http;
using UAParser;
using ClientInfo = Avancira.Application.Common.ClientInfo;

namespace Avancira.Infrastructure.Common;

public class ClientInfoService : IClientInfoService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IGeolocationService _geolocationService;
    private readonly Parser _parser;

    public ClientInfoService(IHttpContextAccessor httpContextAccessor, IGeolocationService geolocationService, Parser parser)
    {
        _httpContextAccessor = httpContextAccessor;
        _geolocationService = geolocationService;
        _parser = parser;
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
        var client = _parser.Parse(context.GetUserAgent());
        var (country, city) = await _geolocationService.GetLocationFromIpAsync(ip);

        return new ClientInfo
        {
            DeviceId = deviceId,
            IpAddress = ip,
            UserAgent = $"{client.UA.Family} {client.UA.Major}".Trim(),
            OperatingSystem = client.ToString(),
            Country = country,
            City = city
        };
    }
}
