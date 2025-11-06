using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Avancira.Infrastructure.Health;

/// <summary>
/// Options used to configure the standard Avancira health checks.
/// Values are hydrated from configuration ("HealthCheck" section) and
/// can be overridden per service through the configure callback.
/// </summary>
public sealed class AvanciraHealthCheckOptions
{
    public const string ConfigurationSectionName = "HealthCheck";

    public bool CheckDatabase { get; set; } = true;

    public bool CheckRedis { get; set; } = true;

    public bool CheckMemory { get; set; } = true;

    public int MemoryThresholdMb { get; set; } = 512;

    public HealthStatus RedisFailureStatus { get; set; } = HealthStatus.Degraded;

    internal static AvanciraHealthCheckOptions FromConfiguration(IConfiguration configuration)
    {
        var options = new AvanciraHealthCheckOptions();
        var section = configuration.GetSection(ConfigurationSectionName);

        if (!section.Exists())
        {
            return options;
        }

        var databaseSection = section.GetSection("Database");
        if (databaseSection.Exists())
        {
            options.CheckDatabase = databaseSection.GetValue(nameof(CheckDatabase), options.CheckDatabase);
            options.CheckDatabase = databaseSection.GetValue("Enabled", options.CheckDatabase);
        }

        var redisSection = section.GetSection("Redis");
        if (redisSection.Exists())
        {
            options.CheckRedis = redisSection.GetValue(nameof(CheckRedis), options.CheckRedis);
            options.CheckRedis = redisSection.GetValue("Enabled", options.CheckRedis);

            var failureStatus = redisSection.GetValue<string>(nameof(RedisFailureStatus))
                ?? redisSection.GetValue<string>("FailureStatus");
            if (!string.IsNullOrWhiteSpace(failureStatus) &&
                Enum.TryParse<HealthStatus>(failureStatus, ignoreCase: true, out var parsed))
            {
                options.RedisFailureStatus = parsed;
            }
        }

        var memorySection = section.GetSection("Memory");
        if (memorySection.Exists())
        {
            options.CheckMemory = memorySection.GetValue(nameof(CheckMemory), options.CheckMemory);
            options.CheckMemory = memorySection.GetValue("Enabled", options.CheckMemory);
            options.MemoryThresholdMb = memorySection.GetValue(nameof(MemoryThresholdMb), options.MemoryThresholdMb);
            options.MemoryThresholdMb = memorySection.GetValue("ThresholdMb", options.MemoryThresholdMb);
        }

        return options;
    }
}
