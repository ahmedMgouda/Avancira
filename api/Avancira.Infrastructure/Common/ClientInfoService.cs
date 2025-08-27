using Avancira.Application.Common;
using Avancira.Application.Catalog;
using Avancira.Infrastructure.Common.Extensions;
using Avancira.Infrastructure.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using UAParser;
using ClientInfo = Avancira.Application.Common.ClientInfo;

namespace Avancira.Infrastructure.Common;

public class ClientInfoService : IClientInfoService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IGeolocationService _geolocationService;
    private readonly Parser _parser;
    private readonly IHostEnvironment _environment;

    public ClientInfoService(
        IHttpContextAccessor httpContextAccessor,
        IGeolocationService geolocationService,
        Parser parser,
        IHostEnvironment environment)
    {
        _httpContextAccessor = httpContextAccessor;
        _geolocationService = geolocationService;
        _parser = parser;
        _environment = environment;
    }

    public async Task<ClientInfo> GetClientInfoAsync()
    {
        var context = _httpContextAccessor.HttpContext ?? throw new InvalidOperationException("No HttpContext available");

        var deviceId = context.Request.Cookies[AuthConstants.Claims.DeviceId];
        if (string.IsNullOrEmpty(deviceId))
        {
            deviceId = Guid.NewGuid().ToString();
            context.Response.Cookies.Append(AuthConstants.Claims.DeviceId, deviceId, new CookieOptions
            {
                HttpOnly = true,
                Secure = !_environment.IsDevelopment(),
                SameSite = SameSiteMode.None,
                Expires = DateTime.UtcNow.AddYears(1),
                Path = "/"
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
