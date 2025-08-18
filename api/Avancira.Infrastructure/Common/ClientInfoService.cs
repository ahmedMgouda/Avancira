using Avancira.Application.Common;
using Avancira.Application.Catalog;
using Microsoft.AspNetCore.Http;
using UAParser;

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

        string ip = "N/A";
        if (context.Request.Headers.TryGetValue("X-Forwarded-For", out var ipList))
        {
            ip = ipList.FirstOrDefault() ?? "N/A";
        }
        else if (context.Connection.RemoteIpAddress != null)
        {
            ip = context.Connection.RemoteIpAddress.MapToIPv4().ToString();
        }

        string userAgent = context.Request.Headers["User-Agent"].FirstOrDefault() ?? "N/A";
        var client = Parser.GetDefault().Parse(userAgent);
        string operatingSystem = client.OS.Family ?? "Unknown";

        var (country, city) = await _geolocationService.GetLocationFromIpAsync(ip);

        return new ClientInfo(ip, userAgent, operatingSystem, country, city);
    }
}
