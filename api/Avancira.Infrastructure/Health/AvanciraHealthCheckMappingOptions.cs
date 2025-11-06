namespace Avancira.Infrastructure.Health;

/// <summary>
/// Configures the health check endpoint routes for Avancira services.
/// </summary>
public sealed class AvanciraHealthCheckMappingOptions
{
    public string LivenessPath { get; set; } = "/health/live";

    public string ReadinessPath { get; set; } = "/health/ready";

    public string DetailedPath { get; set; } = "/health";
}
