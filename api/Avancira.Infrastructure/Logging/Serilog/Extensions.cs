using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Events;
using Serilog.Filters;

namespace Avancira.Infrastructure.Logging.Serilog;

/// <summary>
/// Provides extension methods to configure Serilog with optional OpenTelemetry export.
/// Handles environment variable expansion, safe header parsing,
/// and reasonable defaults for development and production.
/// </summary>
public static class Extensions
{
    /// <summary>
    /// Configures Serilog for the host, enriching logs and forwarding to OpenTelemetry if enabled.
    /// </summary>
    /// <remarks>
    /// Reads from configuration keys:
    /// <list type="bullet">
    /// <item><description><c>OTEL_EXPORTER_OTLP_ENDPOINT</c></description></item>
    /// <item><description><c>OTEL_EXPORTER_OTLP_HEADERS</c></description></item>
    /// <item><description><c>OTEL_RESOURCE_ATTRIBUTES</c></description></item>
    /// </list>
    /// </remarks>
    public static WebApplicationBuilder ConfigureSerilog(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Host.UseSerilog((context, logger) =>
        {
            var configuration = context.Configuration;

            ConfigureOpenTelemetryExport(logger, configuration);
            ConfigureMinimumLevels(logger);
            ConfigureEnrichment(logger);

            // Allow overrides from appsettings.json
            logger.ReadFrom.Configuration(configuration);
        });

        return builder;
    }

    // ──────────────────────────────────────────────
    //  Helper Methods
    // ──────────────────────────────────────────────

    private static void ConfigureOpenTelemetryExport(LoggerConfiguration logger, IConfiguration configuration)
    {
        var endpoint = configuration["OTEL_EXPORTER_OTLP_ENDPOINT"];
        if (string.IsNullOrWhiteSpace(endpoint))
            return; // no OTLP target → local logging only

        logger.WriteTo.OpenTelemetry(options =>
        {
            options.Endpoint = endpoint;

            // Service name (fallback to application name)
            var serviceName = configuration["OTEL_SERVICE_NAME"]
                              ?? configuration["ApplicationName"]
                              ?? AppDomain.CurrentDomain.FriendlyName;
            options.ResourceAttributes.Add("service.name", serviceName);

            // Parse headers safely: key1=value1,key2=value2
            var headerPairs = configuration["OTEL_EXPORTER_OTLP_HEADERS"]?.Split(',');
            if (headerPairs != null)
            {
                foreach (var pair in headerPairs)
                {
                    var parts = pair.Split('=', 2, StringSplitOptions.TrimEntries);
                    if (parts.Length == 2)
                        options.Headers[parts[0]] = parts[1];
                }
            }

            // Parse resource attributes: key=value
            var resourcePairs = configuration["OTEL_RESOURCE_ATTRIBUTES"]?.Split(',');
            if (resourcePairs != null)
            {
                foreach (var pair in resourcePairs)
                {
                    var parts = pair.Split('=', 2, StringSplitOptions.TrimEntries);
                    if (parts.Length == 2)
                        options.ResourceAttributes[parts[0]] = parts[1];
                }
            }
        });
    }

    private static void ConfigureMinimumLevels(LoggerConfiguration logger)
    {
        logger.MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
              .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
              .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Error)
              .MinimumLevel.Override("Hangfire", LogEventLevel.Warning)
              .MinimumLevel.Override("Finbuckle.MultiTenant", LogEventLevel.Warning);
    }

    private static void ConfigureEnrichment(LoggerConfiguration logger)
    {
        logger.Enrich.FromLogContext()
              .Enrich.WithCorrelationId()
              .Filter.ByExcluding(Matching.FromSource(
                  "Microsoft.AspNetCore.Diagnostics.ExceptionHandlerMiddleware"));
    }
}
