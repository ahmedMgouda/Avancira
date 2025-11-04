var builder = DistributedApplication.CreateBuilder(args);

#region ===== INFRASTRUCTURE =====

var loki = builder.AddContainer("loki", "grafana/loki:latest")
    .WithBindMount("loki.yaml", "/etc/loki/loki.yaml")
    .WithBindMount("loki/chunks", "/loki/chunks")
    .WithBindMount("loki/index", "/loki/index")
    .WithArgs("-config.file=/etc/loki/loki.yaml")
    .WithHttpEndpoint(port: 3100, targetPort: 3100);

var prometheus = builder.AddContainer("prometheus", "prom/prometheus:latest")
    .WithBindMount("prometheus.yml", "/etc/prometheus/prometheus.yml")
    .WithHttpEndpoint(port: 3200, targetPort: 3200);

var grafana = builder.AddContainer("grafana", "grafana/grafana:latest")
    .WithEnvironment("GF_SECURITY_ADMIN_PASSWORD", "admin")
    .WithBindMount("grafana/provisioning/datasources", "/etc/grafana/provisioning/datasources")
    .WithBindMount("grafana/provisioning/dashboards", "/etc/grafana/provisioning/dashboards")
    .WithBindMount("grafana/dashboards", "/etc/grafana/dashboards")
    .WithHttpEndpoint(port: 3000, targetPort: 3000);

#endregion

#region ===== DATABASE =====

var postgresPassword = builder.AddParameter("postgres-password", "Avancira@2025", secret: true);
var dropDatabase = builder.AddParameter("drop-database", "false");
var runSeeding = builder.AddParameter("run-seeding", "false");

var postgresql = builder.AddPostgres("postgresql", password: postgresPassword)
    .WithBindMount("pgdata", "/var/lib/postgresql/data")
    .WithPgAdmin();

var postgresDb = postgresql.AddDatabase("avancira");

#endregion

#region ===== ENVIRONMENT VARIABLES =====

var env = new EnvVars(
    AuthIssuer: GetEnv("Avancira__Auth__Issuer", "https://localhost:9100"),

    DatabaseHost: GetEnv("Avancira__Database__Host", "localhost"),
    DatabasePort: GetEnv("Avancira__Database__Port", "5432"),
    DatabaseName: GetEnv("Avancira__Database__Name", "avancira"),
    DatabaseUser: GetEnv("Avancira__Database__User", "Avancira"),
    DatabasePassword: GetEnv("Avancira__Database__Password", "Avancira@2025"),
    SqlitePath: GetEnv("Avancira__Database__Sqlite__Path", string.Empty),

    AppBaseUrl: GetEnv("Avancira__App__BaseUrl", string.Empty),
    AppFrontEndUrl: GetEnv("Avancira__App__FrontEndUrl", string.Empty),
    AppName: GetEnv("Avancira__App__Name", "Avancira"),
    AppSupportEmail: GetEnv("Avancira__App__SupportEmail", "support@avancira.com"),
    AppSupportPhone: GetEnv("Avancira__App__SupportPhone", "+1-800-000-0000"),

    JwtAudience: GetEnv("Avancira__Jwt__Audience", "AvanciraAudience"),
    JwtIssuer: GetEnv("Avancira__Jwt__Issuer", "AvanciraIssuer"),
    JwtKey: GetEnv("Avancira__Jwt__Key", "AvanciraSecretKey"),

    GoogleClientId: GetEnv("Avancira__ExternalServices__Google__ClientId", string.Empty),
    GoogleClientSecret: GetEnv("Avancira__ExternalServices__Google__ClientSecret", string.Empty),
    FacebookAppId: GetEnv("Avancira__ExternalServices__Facebook__AppId", string.Empty),
    FacebookAppSecret: GetEnv("Avancira__ExternalServices__Facebook__AppSecret", string.Empty),

    SmtpHost: GetEnv("Avancira__Notifications__Smtp__Host", string.Empty),
    SmtpPort: GetEnv("Avancira__Notifications__Smtp__Port", string.Empty),
    EmailFrom: GetEnv("Avancira__Notifications__Email__From", string.Empty),
    EmailFromName: GetEnv("Avancira__Notifications__Email__FromName", string.Empty),
    SendGridApiKey: GetEnv("Avancira__Notifications__SendGrid__ApiKey", string.Empty),
    TwilioAccountSid: GetEnv("Avancira__Notifications__Twilio__AccountSid", string.Empty),
    TwilioAuthToken: GetEnv("Avancira__Notifications__Twilio__AuthToken", string.Empty),
    TwilioPhoneNumber: GetEnv("Avancira__Notifications__Twilio__PhoneNumber", string.Empty),
    GraphApiClientId: GetEnv("Avancira__Notifications__GraphApi__ClientId", string.Empty),
    GraphApiClientSecret: GetEnv("Avancira__Notifications__GraphApi__ClientSecret", string.Empty),
    GraphApiTenantId: GetEnv("Avancira__Notifications__GraphApi__TenantId", string.Empty),

    StripeApiKey: GetEnv("Avancira__Payments__Stripe__ApiKey", string.Empty),
    StripePublishableKey: GetEnv("Avancira__Payments__Stripe__PublishableKey", string.Empty),
    StripeSecretKey: GetEnv("Avancira__Payments__Stripe__SecretKey", string.Empty),
    PaypalClientId: GetEnv("Avancira__Payments__Paypal__ClientId", string.Empty),
    PaypalClientSecret: GetEnv("Avancira__Payments__Paypal__ClientSecret", string.Empty),
    PaypalEnvironment: GetEnv("Avancira__Payments__Paypal__Environment", string.Empty),

    JitsiAppId: GetEnv("Avancira__Jitsi__AppId", string.Empty),
    JitsiAppSecret: GetEnv("Avancira__Jitsi__AppSecret", string.Empty),
    JitsiDomain: GetEnv("Avancira__Jitsi__Domain", string.Empty)
);

#endregion

#region ===== AUTH SERVER (MVC) =====

var authServer = builder.AddProject<Projects.Avancira_Auth>("avancira-auth")
    .WithReference(postgresDb)
    .WithEnvironment("ASPIRE_DROP_DATABASE", dropDatabase.Resource.Value)
    .WithEnvironment("ASPIRE_RUN_SEEDING", runSeeding.Resource.Value)
    .WithDatabaseEnvironments(env)
    .WithAppEnvironments(env)
    .WithAuthEnvironments(env)
    .WithExternalServiceEnvironments(env)
    .WithNotificationEnvironments(env)
    .WithPaymentEnvironments(env)
    .WaitFor(postgresql);

#endregion

#region ===== BACKEND API =====

var backend = builder.AddProject<Projects.Avancira_API>("avancira-api")
    .WithReference(postgresDb)
    .WithEnvironment("ASPIRE_DROP_DATABASE", dropDatabase.Resource.Value)
    .WithEnvironment("ASPIRE_RUN_SEEDING", runSeeding.Resource.Value)
    .WithDatabaseEnvironments(env)
    .WithAppEnvironments(env)
    .WithAuthEnvironments(env)
    .WithJwtEnvironments(env)
    .WithExternalServiceEnvironments(env)
    .WithNotificationEnvironments(env)
    .WithPaymentEnvironments(env)
    .WithJitsiEnvironments(env)
    .WaitFor(postgresql)
    .WaitFor(authServer);
#endregion


#region ===== BFF =====
var bff = builder.AddProject<Projects.Avancira_BFF>("avancira-bff")
    .WithReference(postgresDb)
    .WithReference(authServer)
    .WithReference(backend)
    .WithDatabaseEnvironments(env)
    .WithAppEnvironments(env)
    .WithExternalHttpEndpoints()
    .WaitFor(authServer)
    .WaitFor(backend);

#endregion


#region ===== FRONTEND =====

builder.AddNpmApp("avancira-frontend", "../../Frontend.Angular", "start:ssl")
    .WithHttpsEndpoint(port: 4200, targetPort: 4200, name: "frontend-https", isProxied: false)
    .WithExternalHttpEndpoints();

#endregion

builder.Build().Run();

#region ===== HELPERS =====

static string GetEnv(string key, string fallback) =>
    Environment.GetEnvironmentVariable(key) ?? fallback;

#endregion


#region ===== STRONGLY-TYPED ENV MODEL & EXTENSIONS =====

public record EnvVars(
    string AuthIssuer,
    string DatabaseHost,
    string DatabasePort,
    string DatabaseName,
    string DatabaseUser,
    string DatabasePassword,
    string SqlitePath,
    string AppBaseUrl,
    string AppFrontEndUrl,
    string AppName,
    string AppSupportEmail,
    string AppSupportPhone,
    string JwtAudience,
    string JwtIssuer,
    string JwtKey,
    string GoogleClientId,
    string GoogleClientSecret,
    string FacebookAppId,
    string FacebookAppSecret,
    string SmtpHost,
    string SmtpPort,
    string EmailFrom,
    string EmailFromName,
    string SendGridApiKey,
    string TwilioAccountSid,
    string TwilioAuthToken,
    string TwilioPhoneNumber,
    string GraphApiClientId,
    string GraphApiClientSecret,
    string GraphApiTenantId,
    string StripeApiKey,
    string StripePublishableKey,
    string StripeSecretKey,
    string PaypalClientId,
    string PaypalClientSecret,
    string PaypalEnvironment,
    string JitsiAppId,
    string JitsiAppSecret,
    string JitsiDomain
);

public static class AspireExtensions
{
    public static IResourceBuilder<T> WithDatabaseEnvironments<T>(
        this IResourceBuilder<T> builder, EnvVars e) where T : IResourceWithEnvironment =>
        builder
            .WithEnvironment("Avancira__Database__Host", e.DatabaseHost)
            .WithEnvironment("Avancira__Database__Port", e.DatabasePort)
            .WithEnvironment("Avancira__Database__Name", e.DatabaseName)
            .WithEnvironment("Avancira__Database__User", e.DatabaseUser)
            .WithEnvironment("Avancira__Database__Password", e.DatabasePassword)
            .WithEnvironment("Avancira__Database__Sqlite__Path", e.SqlitePath);

    public static IResourceBuilder<T> WithAppEnvironments<T>(
        this IResourceBuilder<T> builder, EnvVars e) where T : IResourceWithEnvironment =>
        builder
            .WithEnvironment("Avancira__App__BaseUrl", e.AppBaseUrl)
            .WithEnvironment("Avancira__App__FrontEndUrl", e.AppFrontEndUrl)
            .WithEnvironment("Avancira__App__Name", e.AppName)
            .WithEnvironment("Avancira__App__SupportEmail", e.AppSupportEmail)
            .WithEnvironment("Avancira__App__SupportPhone", e.AppSupportPhone);

    public static IResourceBuilder<T> WithAuthEnvironments<T>(
        this IResourceBuilder<T> builder, EnvVars e) where T : IResourceWithEnvironment =>
        builder
            .WithEnvironment("Avancira__Auth__Issuer", e.AuthIssuer)
            .WithEnvironment("Auth__Issuer", e.AuthIssuer);

    public static IResourceBuilder<T> WithJwtEnvironments<T>(
        this IResourceBuilder<T> builder, EnvVars e) where T : IResourceWithEnvironment =>
        builder
            .WithEnvironment("Avancira__Jwt__Audience", e.JwtAudience)
            .WithEnvironment("Avancira__Jwt__Issuer", e.JwtIssuer)
            .WithEnvironment("Avancira__Jwt__Key", e.JwtKey);

    public static IResourceBuilder<T> WithExternalServiceEnvironments<T>(
        this IResourceBuilder<T> builder, EnvVars e) where T : IResourceWithEnvironment =>
        builder
            .WithEnvironment("Avancira__ExternalServices__Google__ClientId", e.GoogleClientId)
            .WithEnvironment("Avancira__ExternalServices__Google__ClientSecret", e.GoogleClientSecret)
            .WithEnvironment("Avancira__ExternalServices__Facebook__AppId", e.FacebookAppId)
            .WithEnvironment("Avancira__ExternalServices__Facebook__AppSecret", e.FacebookAppSecret)
            .WithEnvironment("Avancira__Jitsi__AppId", e.JitsiAppId)
            .WithEnvironment("Avancira__Jitsi__AppSecret", e.JitsiAppSecret)
            .WithEnvironment("Avancira__Jitsi__Domain", e.JitsiDomain);

    public static IResourceBuilder<T> WithNotificationEnvironments<T>(
        this IResourceBuilder<T> builder, EnvVars e) where T : IResourceWithEnvironment =>
        builder
            .WithEnvironment("Avancira__Notifications__Smtp__Host", e.SmtpHost)
            .WithEnvironment("Avancira__Notifications__Smtp__Port", e.SmtpPort)
            .WithEnvironment("Avancira__Notifications__Email__From", e.EmailFrom)
            .WithEnvironment("Avancira__Notifications__Email__FromName", e.EmailFromName)
            .WithEnvironment("Avancira__Notifications__SendGrid__ApiKey", e.SendGridApiKey)
            .WithEnvironment("Avancira__Notifications__Twilio__AccountSid", e.TwilioAccountSid)
            .WithEnvironment("Avancira__Notifications__Twilio__AuthToken", e.TwilioAuthToken)
            .WithEnvironment("Avancira__Notifications__Twilio__PhoneNumber", e.TwilioPhoneNumber)
            .WithEnvironment("Avancira__Notifications__GraphApi__ClientId", e.GraphApiClientId)
            .WithEnvironment("Avancira__Notifications__GraphApi__ClientSecret", e.GraphApiClientSecret)
            .WithEnvironment("Avancira__Notifications__GraphApi__TenantId", e.GraphApiTenantId);

    public static IResourceBuilder<T> WithPaymentEnvironments<T>(
        this IResourceBuilder<T> builder, EnvVars e) where T : IResourceWithEnvironment =>
        builder
            .WithEnvironment("Avancira__Payments__Stripe__ApiKey", e.StripeApiKey)
            .WithEnvironment("Avancira__Payments__Stripe__PublishableKey", e.StripePublishableKey)
            .WithEnvironment("Avancira__Payments__Stripe__SecretKey", e.StripeSecretKey)
            .WithEnvironment("Avancira__Payments__Paypal__ClientId", e.PaypalClientId)
            .WithEnvironment("Avancira__Payments__Paypal__ClientSecret", e.PaypalClientSecret)
            .WithEnvironment("Avancira__Payments__Paypal__Environment", e.PaypalEnvironment);

    public static IResourceBuilder<T> WithJitsiEnvironments<T>(
        this IResourceBuilder<T> builder, EnvVars e) where T : IResourceWithEnvironment =>
        builder
            .WithEnvironment("Avancira__Jitsi__AppId", e.JitsiAppId)
            .WithEnvironment("Avancira__Jitsi__AppSecret", e.JitsiAppSecret)
            .WithEnvironment("Avancira__Jitsi__Domain", e.JitsiDomain);
}

#endregion
