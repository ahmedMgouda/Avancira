using Avancira.Domain.Identity.ValueObjects;
using Avancira.Domain.UserSessions.ValueObjects;

namespace Avancira.Application.UserSessions;

/// <summary>
/// Resolves IP addresses to geographic locations (country/city).
/// </summary>
public interface IGeolocationService
{
    /// <summary>
    /// Resolves an IP address to a <see cref="GeographicLocation"/>.
    /// </summary>
    /// <param name="ipAddress">The IP address to resolve.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="GeographicLocation"/> if found; otherwise <c>null</c>.</returns>
    Task<GeographicLocation?> GetLocationAsync(IpAddress ipAddress, CancellationToken cancellationToken = default);
}
