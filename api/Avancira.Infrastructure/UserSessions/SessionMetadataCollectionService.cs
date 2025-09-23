using Avancira.Application.UserSessions.Services;
using Avancira.Application.UserSessions;
using Avancira.Domain.UserSessions.ValueObjects;
using Microsoft.AspNetCore.Http;

namespace Avancira.Infrastructure.UserSessions
{

    public sealed class SessionMetadataCollectionService : ISessionMetadataCollectionService
    {
        private readonly INetworkContextService _networkService;
        private readonly IUserAgentAnalysisService _userAgentService;
        private readonly IGeolocationService _geolocationService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public SessionMetadataCollectionService(
            INetworkContextService networkService,
            IUserAgentAnalysisService userAgentService,
            IGeolocationService geolocationService,
            IHttpContextAccessor httpContextAccessor)
        {
            _networkService = networkService;
            _userAgentService = userAgentService;
            _geolocationService = geolocationService;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<SessionMetadata> CollectAsync(CancellationToken cancellationToken = default)
        {
            var context = _httpContextAccessor.HttpContext
                ?? throw new InvalidOperationException("HTTP context not available");

            // IP & DeviceId
            var ip = _networkService.GetIpAddress();
            var deviceId = _networkService.GetOrCreateDeviceId();

            // UserAgent → DeviceInfo
            var userAgentString = UserAgentString.Create(context.Request.Headers["User-Agent"].ToString());
            var deviceInfo = _userAgentService.AnalyzeUserAgent(userAgentString);

            // Geolocation
            var location = await _geolocationService.GetLocationAsync(ip, cancellationToken);

            return SessionMetadata.Create(ip, deviceId, userAgentString, deviceInfo, location);
        }
    }
}
