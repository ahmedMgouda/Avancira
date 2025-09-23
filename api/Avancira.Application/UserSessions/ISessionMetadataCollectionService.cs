using Avancira.Domain.UserSessions.ValueObjects;

namespace Avancira.Application.UserSessions;

/// <summary>
/// Collects session metadata (IP, device, user agent, geolocation).
/// </summary>
public interface ISessionMetadataCollectionService
{
    /// <summary>
    /// Collects metadata for the current request context.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A populated <see cref="SessionMetadata"/> instance.</returns>
    Task<SessionMetadata> CollectAsync(CancellationToken cancellationToken = default);
}
