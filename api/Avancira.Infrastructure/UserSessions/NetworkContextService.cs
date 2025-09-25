using Avancira.Application.UserSessions.Services;
using Avancira.Domain.UserSessions.ValueObjects;
using Microsoft.AspNetCore.Http;

namespace Avancira.Infrastructure.UserSessions
{

    public sealed class NetworkContextService : INetworkContextService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public NetworkContextService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public IpAddress GetIpAddress()
        {
            var context = _httpContextAccessor.HttpContext
                ?? throw new InvalidOperationException("HTTP context not available");

            var ip = context.Connection.RemoteIpAddress?.ToString();
            return IpAddress.Create(ip);
        }

        public DeviceIdentifier GetOrCreateDeviceId()
        {
            var context = _httpContextAccessor.HttpContext
                ?? throw new InvalidOperationException("HTTP context not available");

            if (context.Request.Cookies.TryGetValue("device_id", out var deviceId))
            {
                return DeviceIdentifier.Create(deviceId);
            }

            var generated = DeviceIdentifier.Generate("gen");

            var options = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None, 
                Path = "/",
                Expires = DateTimeOffset.UtcNow.AddYears(1)
            };

            context.Response.Cookies.Append("device_id", generated.Value, options);

            return generated;
        }
    }
}
