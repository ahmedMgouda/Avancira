namespace Avancira.Infrastructure.Configuration;

/// <summary>
/// Centralized service and port configuration following DDD principles.
/// Defines all services, ports, and endpoints in one place.
/// </summary>
public static class ServiceRegistry
{
    private static readonly ServiceDefinition[] Services =
    [
        // Backend Services
        new("api", 9000, 5000, ServiceType.BackendService, "API Service"),
        new("auth", 9100, 5100, ServiceType.BackendService, "Authentication Server"),
        new("bff", 9200, 5200, ServiceType.BackendService, "Backend for Frontend"),

        // Frontend
        new("frontend", 4300, 4300, ServiceType.Frontend, "Angular Frontend"),

        // Infrastructure
        new("postgres", 5432, 5432, ServiceType.Infrastructure, "PostgreSQL Database"),
        new("loki", 3100, 3100, ServiceType.Infrastructure, "Loki Log Aggregation"),
        new("prometheus", 7070, 9090, ServiceType.Infrastructure, "Prometheus Metrics"),
        new("grafana", 3000, 3000, ServiceType.Infrastructure, "Grafana Dashboard"),
        new("pgadmin", 5050, 80, ServiceType.Infrastructure, "pgAdmin")
    ];

    private static readonly Dictionary<string, ServiceDefinition> Map =
        Services.ToDictionary(s => s.Name, StringComparer.OrdinalIgnoreCase);

    public static ServiceDefinition GetService(string name) =>
        Map.TryGetValue(name, out var svc)
            ? svc
            : throw new ArgumentException(
                $"Service '{name}' not configured. Available: {string.Join(", ", Map.Keys)}",
                nameof(name));

    public static IReadOnlyList<ServiceDefinition> All => Services.AsReadOnly();

    public static void ValidateConfiguration()
    {
        CheckConflicts(Services.GroupBy(s => s.HttpsPort), "HTTPS");
        CheckConflicts(Services.GroupBy(s => s.HttpPort), "HTTP");
    }

    private static void CheckConflicts(IEnumerable<IGrouping<int, ServiceDefinition>> groups, string label)
    {
        var conflicts = groups.Where(g => g.Count() > 1).ToList();
        if (!conflicts.Any()) return;

        var detail = string.Join("; ",
            conflicts.Select(g => $"{label} {g.Key}: {string.Join(", ", g.Select(s => s.Name))}"));
        throw new InvalidOperationException($"Port conflicts detected: {detail}");
    }

    public static class Endpoints
    {
        public static string Api => Url("api");
        public static string Auth => Url("auth");
        public static string Bff => Url("bff");
        public static string Frontend => Url("frontend");
        public static string Grafana => Url("grafana");

        private static string Url(string name) =>
            $"https://localhost:{GetService(name).HttpsPort}";
    }
}

public record ServiceDefinition(string Name, int HttpsPort, int HttpPort, ServiceType Type, string Description)
{
    public override string ToString() => $"{Description} ({Name}:{HttpsPort})";
}

public enum ServiceType
{
    BackendService,
    Frontend,
    Infrastructure
}
