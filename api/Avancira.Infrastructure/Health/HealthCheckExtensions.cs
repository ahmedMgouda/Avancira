using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Avancira.Infrastructure.Health;

/// <summary>
/// Provides a consistent set of health checks and endpoint mappings across Avancira services.
/// Follows security best practices for public health endpoints.
/// </summary>
public static class HealthCheckExtensions
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };

    public static IHealthChecksBuilder AddAvanciraHealthChecks<TDbContext>(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<AvanciraHealthCheckOptions>? configure = null)
        where TDbContext : DbContext
        => AddAvanciraHealthChecksCore(
            services,
            configuration,
            configure,
            (builder, _) => builder.AddDbContextCheck<TDbContext>(
                name: "database",
                failureStatus: HealthStatus.Unhealthy,
                tags: ["ready", "db"],
                customTestQuery: async (db, ct) => await db.Database.CanConnectAsync(ct)));

    public static IHealthChecksBuilder AddAvanciraHealthChecks(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<AvanciraHealthCheckOptions>? configure = null)
        => AddAvanciraHealthChecksCore(services, configuration, configure, registerDatabaseCheck: null);

    private static IHealthChecksBuilder AddAvanciraHealthChecksCore(
        IServiceCollection services,
        IConfiguration configuration,
        Action<AvanciraHealthCheckOptions>? configure,
        Action<IHealthChecksBuilder, AvanciraHealthCheckOptions>? registerDatabaseCheck)
    {
        var options = AvanciraHealthCheckOptions.FromConfiguration(configuration);
        configure?.Invoke(options);

        var builder = services.AddHealthChecks();

        EnsureSelfHealthCheckRegistration(services);

        if (options.CheckDatabase && registerDatabaseCheck is not null)
        {
            registerDatabaseCheck(builder, options);
        }

        if (options.CheckRedis)
        {
            var redisConnection = configuration.GetConnectionString("Redis");
            if (!string.IsNullOrWhiteSpace(redisConnection))
            {
                builder.AddRedis(
                    redisConnection,
                    name: "redis",
                    failureStatus: options.RedisFailureStatus,
                    tags: ["ready", "cache"]);
            }
        }

        if (options.CheckMemory && options.MemoryThresholdMb > 0)
        {
            builder.AddCheck(
                "memory",
                () =>
                {
                    var allocatedBytes = GC.GetTotalMemory(forceFullCollection: false);
                    var allocatedMb = allocatedBytes / 1024 / 1024;

                    return allocatedMb > options.MemoryThresholdMb
                        ? HealthCheckResult.Degraded(
                            $"Memory usage is {allocatedMb}MB (threshold: {options.MemoryThresholdMb}MB)")
                        : HealthCheckResult.Healthy($"Memory usage: {allocatedMb}MB");
                },
                tags: ["ready"]);
        }

        return builder;
    }
    
    /// <summary>
    /// Maps health check endpoints following best practices:
    /// - /health - Public endpoint with minimal info (safe for external monitoring)
    /// - /health/live - Liveness probe (internal use)
    /// - /health/ready - Readiness probe (internal use)
    /// </summary>
    public static WebApplication MapAvanciraHealthChecks(
        this WebApplication app,
        AvanciraHealthCheckMappingOptions? options = null)
    {
        options ??= new AvanciraHealthCheckMappingOptions();

        // PUBLIC ENDPOINT - Minimal information for external monitoring
        // Returns only: status, timestamp, (optionally version)
        // No internal details, no error messages, no dependency info
        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            ResponseWriter = WritePublicHealthResponse,
            AllowCachingResponses = false,
            ResultStatusCodes =
            {
                [HealthStatus.Healthy] = StatusCodes.Status200OK,
                [HealthStatus.Degraded] = StatusCodes.Status200OK,
                [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
            }
        });

        // INTERNAL ENDPOINTS - For Kubernetes/Docker health probes
        app.MapHealthChecks(options.LivenessPath, new HealthCheckOptions
        {
            Predicate = registration => registration.Tags.Contains("live"),
            ResponseWriter = WriteLivenessResponse,
            AllowCachingResponses = false
        });

        app.MapHealthChecks(options.ReadinessPath, new HealthCheckOptions
        {
            Predicate = registration => registration.Tags.Contains("ready"),
            ResponseWriter = WriteReadinessResponse,
            AllowCachingResponses = false,
            ResultStatusCodes =
            {
                [HealthStatus.Healthy] = StatusCodes.Status200OK,
                [HealthStatus.Degraded] = StatusCodes.Status200OK,
                [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
            }
        });

        // DETAILED ENDPOINT - For internal debugging (should be protected in production)
        app.MapHealthChecks(options.DetailedPath, new HealthCheckOptions
        {
            ResponseWriter = WriteDetailedResponse,
            AllowCachingResponses = false
        });

        return app;
    }

    /// <summary>
    /// Public health response - safe for external consumption
    /// No sensitive information, no internal details
    /// </summary>
    private static Task WritePublicHealthResponse(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";

        var response = report.Status switch
        {
            HealthStatus.Healthy => PublicHealthResponse.Healthy(),
            HealthStatus.Degraded => PublicHealthResponse.Degraded(),
            _ => PublicHealthResponse.Unhealthy()
        };

        return context.Response.WriteAsync(JsonSerializer.Serialize(response, JsonOptions));
    }

    private static Task WriteLivenessResponse(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";

        var response = new
        {
            status = report.Status.ToString(),
            timestamp = DateTime.UtcNow
        };

        return context.Response.WriteAsync(JsonSerializer.Serialize(response, JsonOptions));
    }

    private static Task WriteReadinessResponse(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";

        var response = new
        {
            status = report.Status.ToString(),
            timestamp = DateTime.UtcNow,
            checks = report.Entries
                .Where(entry => entry.Value.Status != HealthStatus.Healthy)
                .Select(entry => new
                {
                    name = entry.Key,
                    status = entry.Value.Status.ToString(),
                    description = entry.Value.Description
                })
        };

        return context.Response.WriteAsync(JsonSerializer.Serialize(response, JsonOptions));
    }

    private static Task WriteDetailedResponse(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";

        var response = new
        {
            status = report.Status.ToString(),
            timestamp = DateTime.UtcNow,
            totalDuration = report.TotalDuration.TotalMilliseconds,
            checks = report.Entries.Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString(),
                description = entry.Value.Description,
                duration = entry.Value.Duration.TotalMilliseconds,
                tags = entry.Value.Tags,
                data = entry.Value.Data,
                exception = entry.Value.Exception?.Message
            })
        };

        return context.Response.WriteAsync(JsonSerializer.Serialize(response, JsonOptions));
    }

    private static void EnsureSelfHealthCheckRegistration(IServiceCollection services)
    {
        services.Configure<HealthCheckServiceOptions>(options =>
        {
            if (options.Registrations.Any(r =>
                    string.Equals(r.Name, "self", StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }

            options.Registrations.Add(new HealthCheckRegistration(
                "self",
                _ => new InlineHealthCheck(),
                failureStatus: null,
                tags: new[] { "live" }));
        });
    }
    
    private sealed class InlineHealthCheck : IHealthCheck
    {
        public Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                HealthCheckResult.Healthy("Service is running"));
        }
    }
}
