namespace Avancira.Infrastructure.Health;

/// <summary>
/// Public health check response for external clients.
/// Contains only safe, non-sensitive information.
/// </summary>
public sealed class PublicHealthResponse
{
    /// <summary>
    /// Overall system status: "healthy", "degraded", or "unhealthy"
    /// </summary>
    public required string Status { get; init; }

    /// <summary>
    /// UTC timestamp of the health check
    /// </summary>
    public required DateTime Timestamp { get; init; }

    /// <summary>
    /// Version of the service (optional)
    /// </summary>
    public string? Version { get; init; }

    /// <summary>
    /// Creates a healthy response
    /// </summary>
    public static PublicHealthResponse Healthy(string? version = null) => new()
    {
        Status = "healthy",
        Timestamp = DateTime.UtcNow,
        Version = version
    };

    /// <summary>
    /// Creates a degraded response
    /// </summary>
    public static PublicHealthResponse Degraded(string? version = null) => new()
    {
        Status = "degraded",
        Timestamp = DateTime.UtcNow,
        Version = version
    };

    /// <summary>
    /// Creates an unhealthy response
    /// </summary>
    public static PublicHealthResponse Unhealthy(string? version = null) => new()
    {
        Status = "unhealthy",
        Timestamp = DateTime.UtcNow,
        Version = version
    };
}
