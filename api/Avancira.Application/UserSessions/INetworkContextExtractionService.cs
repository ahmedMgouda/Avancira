using Avancira.Domain.UserSessions.ValueObjects;

namespace Avancira.Application.UserSessions.Services;

/// <summary>
/// Provides network context values for session metadata.
/// </summary>
public interface INetworkContextService
{
    IpAddress GetIpAddress();
    DeviceIdentifier GetOrCreateDeviceId();
}
