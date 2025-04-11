var builder = DistributedApplication.CreateBuilder(args);


// Loki
var loki = builder.AddContainer("loki", "grafana/loki:latest")
    .WithBindMount("loki.yaml", "/etc/loki/loki.yaml")
    .WithBindMount("loki/chunks", "/loki/chunks")
    .WithBindMount("loki/index", "/loki/index")
    .WithEnvironment("LOKI_PORT", "3100")
    .WithArgs("-config.file=/etc/loki/loki.yaml")
    .WithHttpEndpoint(port: 3100, targetPort: 3100);

// Prometheus
var prometheus = builder.AddContainer("prometheus", "prom/prometheus:latest")
    .WithBindMount("prometheus.yml", "/etc/prometheus/prometheus.yml")
    .WithHttpEndpoint(port: 7070, targetPort: 9090);

// Grafana
var grafana = builder.AddContainer("grafana", "grafana/grafana:latest")
    .WithEnvironment("GF_SECURITY_ADMIN_PASSWORD", "admin")
    .WithBindMount("grafana/provisioning/datasources", "/etc/grafana/provisioning/datasources")
    .WithBindMount("grafana/provisioning/dashboards", "/etc/grafana/provisioning/dashboards")
    .WithBindMount("grafana/dashboards", "/etc/grafana/dashboards")
    .WithHttpEndpoint(port: 3000, targetPort: 3000, name: "http");

// PostgreSQL
var postgresql = builder.AddContainer("postgresql", "postgres:latest")
    .WithEnvironment("POSTGRES_USER", Environment.GetEnvironmentVariable("Avancira__Database__User") ?? "")
    .WithEnvironment("POSTGRES_PASSWORD", Environment.GetEnvironmentVariable("Avancira__Database__Password") ?? "")
    .WithEnvironment("POSTGRES_DB", Environment.GetEnvironmentVariable("Avancira__Database__Name") ?? "")
    .WithBindMount("postgresql", "/var/lib/postgresql/data")
    .WithHttpEndpoint(port: 5432, targetPort: 5432);

// Backend
builder.AddProject<Projects.Avancira_API>("avancira-backend-container")
    .WaitFor(postgresql);


builder.Build().Run();
