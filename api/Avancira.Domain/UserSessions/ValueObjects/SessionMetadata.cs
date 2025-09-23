using Avancira.Domain.Identity.ValueObjects;

namespace Avancira.Domain.UserSessions.ValueObjects
{
    public sealed record SessionMetadata
    {
        private SessionMetadata(
            IpAddress ipAddress,
            DeviceIdentifier deviceId,
            UserAgentString userAgent,
            DeviceInformation deviceInfo,
            GeographicLocation? location,
            DateTime collectedAt)
        {
            IpAddress = ipAddress;
            DeviceId = deviceId;
            UserAgent = userAgent;
            DeviceInfo = deviceInfo;
            Location = location;
            CollectedAt = collectedAt;
        }

        public IpAddress IpAddress { get; }
        public DeviceIdentifier DeviceId { get; }
        public UserAgentString UserAgent { get; }
        public DeviceInformation DeviceInfo { get; }
        public GeographicLocation? Location { get; }
        public DateTime CollectedAt { get; }

        public static SessionMetadata Create(
            IpAddress ip,
            DeviceIdentifier deviceId,
            UserAgentString userAgent,
            DeviceInformation deviceInfo,
            GeographicLocation? location = null) =>
            new(ip, deviceId, userAgent, deviceInfo, location, DateTime.UtcNow);

        public override string ToString() =>
            $"{DeviceInfo?.Name ?? "Unknown Device"} - {IpAddress.Value} ({UserAgent.Value})";
    }
}
