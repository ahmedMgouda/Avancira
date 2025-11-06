using System;
using System.Linq;
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
                tags: new[] { "ready", "db" },
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

        builder.AddCheck(
            "self",
            () => HealthCheckResult.Healthy("Service is running"),
            tags: new[] { "live" });

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
                    tags: new[] { "ready", "cache" });
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
                tags: new[] { "ready" });
        }

        return builder;
    }

    public static WebApplication MapAvanciraHealthChecks(
        this WebApplication app,
        AvanciraHealthCheckMappingOptions? options = null)
    {
        options ??= new AvanciraHealthCheckMappingOptions();

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

        app.MapHealthChecks(options.DetailedPath, new HealthCheckOptions
        {
            ResponseWriter = WriteDetailedResponse,
            AllowCachingResponses = false
        });

        return app;
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
}
