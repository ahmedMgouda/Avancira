using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

namespace Avancira.Infrastructure.Persistence.Seeders;

/// <summary>
/// Runs all seeders in defined phases with safe parallel execution and isolation.
/// Each phase waits for the previous to finish. Each seeder has its own DbContext.
/// </summary>
internal sealed class DataSeederOrchestrator
{
    private readonly ILogger<DataSeederOrchestrator> _logger;
    private readonly IServiceProvider _serviceProvider;

    public DataSeederOrchestrator(
        ILogger<DataSeederOrchestrator> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("=== Starting Database Seeding Process ===");

        // Check schema health before starting
        using (var scope = _serviceProvider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AvanciraDbContext>();
            var canConnect = await db.Database.CanConnectAsync(cancellationToken);
            if (!canConnect)
            {
                _logger.LogError("❌ Cannot connect to database. Seeding aborted.");
                return;
            }

            var pending = (await db.Database.GetPendingMigrationsAsync(cancellationToken)).ToList();
            if (pending.Count > 0)
            {
                _logger.LogWarning("⚠️ Found {Count} pending migrations before seeding. Applying them now...", pending.Count);
                await db.Database.MigrateAsync(cancellationToken);
                _logger.LogInformation("✅ Pending migrations applied before seeding.");
            }
        }

        // Define seeders and their phases
        var seeders = new (int Phase, Type SeederType)[]
        {
            (0, typeof(OpenIddictClientSeeder)),
            (1, typeof(RoleSeeder)),
            (1, typeof(CountrySeeder)),
            (1, typeof(CategorySeeder)),
            (1, typeof(PromoCodeSeeder)),
            (2, typeof(UserSeeder)),
            (3, typeof(ListingSeeder)),
            (4, typeof(ListingCategorySeeder))
        };

        var results = new List<(string Name, bool Success, long DurationMs)>();

        foreach (var phase in seeders.GroupBy(s => s.Phase).OrderBy(g => g.Key))
        {
            _logger.LogInformation("── Phase {Phase} ({Count} seeders) ──", phase.Key, phase.Count());

            var tasks = phase.Select(s => ExecuteSeederAsync(s.SeederType, cancellationToken));
            var phaseResults = await Task.WhenAll(tasks);

            results.AddRange(phaseResults);
        }

        LogSummary(results);
    }

    private async Task<(string Name, bool Success, long DurationMs)> ExecuteSeederAsync(
        Type seederType,
        CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var seeder = (ISeeder)scope.ServiceProvider.GetRequiredService(seederType);

        try
        {
            var db = scope.ServiceProvider.GetRequiredService<AvanciraDbContext>();
            if (!await db.Database.CanConnectAsync(cancellationToken))
            {
                _logger.LogWarning("Skipping {SeederName}: Cannot connect to DB.", seederType.Name);
                return (seederType.Name, false, 0);
            }

            var start = Environment.TickCount64;
            _logger.LogInformation("▶ Running {SeederName}...", seeder.Name);

            await seeder.SeedAsync(cancellationToken);

            var duration = Environment.TickCount64 - start;
            _logger.LogInformation("✅ {SeederName} completed in {Duration} ms", seeder.Name, duration);

            return (seeder.Name, true, duration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed {SeederName}: {Error}", seederType.Name, ex.Message);
            return (seederType.Name, false, 0);
        }
    }

    private void LogSummary(List<(string Name, bool Success, long DurationMs)> results)
    {
        var succeeded = results.Count(r => r.Success);
        var failed = results.Count(r => !r.Success);
        var total = results.Sum(r => r.DurationMs);

        _logger.LogInformation("=== SEEDING SUMMARY: {Succeeded} succeeded, {Failed} failed, Total {TotalMs} ms ===",
            succeeded, failed, total);

        foreach (var result in results)
        {
            var status = result.Success ? "✔️" : "❌";
            _logger.LogInformation("  {Status} {Seeder} ({Ms} ms)", status, result.Name, result.DurationMs);
        }
    }
}
