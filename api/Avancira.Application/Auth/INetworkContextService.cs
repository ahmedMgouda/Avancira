namespace Avancira.Application.Auth;

/// <summary>
/// Provides network context information (IP, device ID, etc)
/// Used to track sessions and detect suspicious activity.
/// </summary>
public interface INetworkContextService
{
    string GetOrCreateDeviceId();
    string GetClientIpAddress();
    string GetUserAgent();
}
