namespace Avancira.Application.UserSessions.Services;

/// <summary>
/// Provides network context values for session metadata.
/// </summary>
public interface INetworkContextService
{
    string? GetIpAddress();
    string GetOrCreateDeviceId();
}
