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
var postgresPassword = builder.AddParameter("postgres-password", "Avancira@2025", secret: true);
var dropDatabase = builder.AddParameter("drop-database", "true", secret: false);
var runSeeding = builder.AddParameter("run-seeding", "true", secret: false);

var postgresql = builder.AddPostgres("postgresql", password: postgresPassword)
   .WithBindMount("pgdata", "/var/lib/postgresql/data")
   .WithPgAdmin();

var postgresDb = postgresql.AddDatabase("avancira");

// Backend
builder.AddProject<Projects.Avancira_API>("avancira-backend-container")
    .WithReference(postgresDb)
    .WithEnvironment("ASPIRE_DROP_DATABASE", dropDatabase.Resource.Value)
    .WithEnvironment("ASPIRE_RUN_SEEDING", runSeeding.Resource.Value)
    // Database Configuration - Use actual values instead of string.Empty to ensure they're set
    .WithEnvironment("Avancira__Database__Host", Environment.GetEnvironmentVariable("Avancira__Database__Host") ?? "localhost")
    .WithEnvironment("Avancira__Database__Port", Environment.GetEnvironmentVariable("Avancira__Database__Port") ?? "5432")
    .WithEnvironment("Avancira__Database__Name", Environment.GetEnvironmentVariable("Avancira__Database__Name") ?? "avancira")
    .WithEnvironment("Avancira__Database__User", Environment.GetEnvironmentVariable("Avancira__Database__User") ?? "Avancira")
    .WithEnvironment("Avancira__Database__Password", Environment.GetEnvironmentVariable("Avancira__Database__Password") ?? "Avancira@2025")
    .WithEnvironment("Avancira__Database__Sqlite__Path", Environment.GetEnvironmentVariable("Avancira__Database__Sqlite__Path") ?? string.Empty)
    // App Configuration
    .WithEnvironment("Avancira__App__BaseUrl", Environment.GetEnvironmentVariable("Avancira__App__BaseUrl") ?? string.Empty)
    .WithEnvironment("Avancira__App__FrontEndUrl", Environment.GetEnvironmentVariable("Avancira__App__FrontEndUrl") ?? string.Empty)
    .WithEnvironment("Avancira__App__Name", Environment.GetEnvironmentVariable("Avancira__App__Name") ?? string.Empty)
    .WithEnvironment("Avancira__App__SupportEmail", Environment.GetEnvironmentVariable("Avancira__App__SupportEmail") ?? string.Empty)
    .WithEnvironment("Avancira__App__SupportPhone", Environment.GetEnvironmentVariable("Avancira__App__SupportPhone") ?? string.Empty)
    // JWT Configuration
    .WithEnvironment("Avancira__Jwt__Audience", Environment.GetEnvironmentVariable("Avancira__Jwt__Audience") ?? string.Empty)
    .WithEnvironment("Avancira__Jwt__Issuer", Environment.GetEnvironmentVariable("Avancira__Jwt__Issuer") ?? string.Empty)
    .WithEnvironment("Avancira__Jwt__Key", Environment.GetEnvironmentVariable("Avancira__Jwt__Key") ?? string.Empty)
    // External Services Configuration
    .WithEnvironment("Avancira__ExternalServices__Facebook__AppId", Environment.GetEnvironmentVariable("Avancira__ExternalServices__Facebook__AppId") ?? string.Empty)
    .WithEnvironment("Avancira__ExternalServices__Facebook__AppSecret", Environment.GetEnvironmentVariable("Avancira__ExternalServices__Facebook__AppSecret") ?? string.Empty)
    .WithEnvironment("Avancira__ExternalServices__Google__ApiKey", Environment.GetEnvironmentVariable("Avancira__ExternalServices__Google__ApiKey") ?? string.Empty)
    .WithEnvironment("Avancira__ExternalServices__Google__ClientId", Environment.GetEnvironmentVariable("Avancira__ExternalServices__Google__ClientId") ?? string.Empty)
    .WithEnvironment("Avancira__ExternalServices__Google__ClientSecret", Environment.GetEnvironmentVariable("Avancira__ExternalServices__Google__ClientSecret") ?? string.Empty)
    // Jitsi Configuration
    .WithEnvironment("Avancira__Jitsi__AppId", Environment.GetEnvironmentVariable("Avancira__Jitsi__AppId") ?? string.Empty)
    .WithEnvironment("Avancira__Jitsi__AppSecret", Environment.GetEnvironmentVariable("Avancira__Jitsi__AppSecret") ?? string.Empty)
    .WithEnvironment("Avancira__Jitsi__Domain", Environment.GetEnvironmentVariable("Avancira__Jitsi__Domain") ?? string.Empty)
    // Notifications Configuration
    .WithEnvironment("Avancira__Notifications__Email__From", Environment.GetEnvironmentVariable("Avancira__Notifications__Email__From") ?? string.Empty)
    .WithEnvironment("Avancira__Notifications__Email__FromName", Environment.GetEnvironmentVariable("Avancira__Notifications__Email__FromName") ?? string.Empty)
    .WithEnvironment("Avancira__Notifications__GraphApi__ClientId", Environment.GetEnvironmentVariable("Avancira__Notifications__GraphApi__ClientId") ?? string.Empty)
    .WithEnvironment("Avancira__Notifications__GraphApi__ClientSecret", Environment.GetEnvironmentVariable("Avancira__Notifications__GraphApi__ClientSecret") ?? string.Empty)
    .WithEnvironment("Avancira__Notifications__GraphApi__TenantId", Environment.GetEnvironmentVariable("Avancira__Notifications__GraphApi__TenantId") ?? string.Empty)
    .WithEnvironment("Avancira__Notifications__SendGrid__ApiKey", Environment.GetEnvironmentVariable("Avancira__Notifications__SendGrid__ApiKey") ?? string.Empty)
    .WithEnvironment("Avancira__Notifications__Smtp__Host", Environment.GetEnvironmentVariable("Avancira__Notifications__Smtp__Host") ?? string.Empty)
    .WithEnvironment("Avancira__Notifications__Smtp__Port", Environment.GetEnvironmentVariable("Avancira__Notifications__Smtp__Port") ?? string.Empty)
    .WithEnvironment("Avancira__Notifications__Twilio__AccountSid", Environment.GetEnvironmentVariable("Avancira__Notifications__Twilio__AccountSid") ?? string.Empty)
    .WithEnvironment("Avancira__Notifications__Twilio__AuthToken", Environment.GetEnvironmentVariable("Avancira__Notifications__Twilio__AuthToken") ?? string.Empty)
    .WithEnvironment("Avancira__Notifications__Twilio__PhoneNumber", Environment.GetEnvironmentVariable("Avancira__Notifications__Twilio__PhoneNumber") ?? string.Empty)
    // Payment Configuration
    .WithEnvironment("Avancira__Payments__Paypal__ClientId", Environment.GetEnvironmentVariable("Avancira__Payments__Paypal__ClientId") ?? string.Empty)
    .WithEnvironment("Avancira__Payments__Paypal__ClientSecret", Environment.GetEnvironmentVariable("Avancira__Payments__Paypal__ClientSecret") ?? string.Empty)
    .WithEnvironment("Avancira__Payments__Paypal__Environment", Environment.GetEnvironmentVariable("Avancira__Payments__Paypal__Environment") ?? string.Empty)
    .WithEnvironment("Avancira__Payments__Stripe__ApiKey", Environment.GetEnvironmentVariable("Avancira__Payments__Stripe__ApiKey") ?? string.Empty)
    .WithEnvironment("Avancira__Payments__Stripe__PublishableKey", Environment.GetEnvironmentVariable("Avancira__Payments__Stripe__PublishableKey") ?? string.Empty)
    .WithEnvironment("Avancira__Payments__Stripe__SecretKey", Environment.GetEnvironmentVariable("Avancira__Payments__Stripe__SecretKey") ?? string.Empty)
    .WaitFor(postgresql);


builder.AddNpmApp("avancira-frontend-container", "../../Frontend.Angular")
    .WithHttpEndpoint(env: "PORT", port: 4200, name: "frontend-app")
    .WithExternalHttpEndpoints();

// Admin Dashboard
builder.AddProject<Projects.Client>("admin-dashboard");


builder.Build().Run();
