using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Avancira.Infrastructure.Persistence.Seeders;

/// <summary>
/// Orchestrates all registered data seeders by dependency phase.
/// Each phase runs after the previous completes; seeders within a phase run in parallel,
/// each with its own DI scope and DbContext instance.
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
        _logger.LogInformation("Starting database seeding process...");

        var seeders = new (int Phase, Type SeederType)[]
        {
            // Phase 1: Critical base data
            (1, typeof(RoleSeeder)),
            (1, typeof(CountrySeeder)),
            (1, typeof(CategorySeeder)),
            (1, typeof(PromoCodeSeeder)),

            // Phase 2: Users
            (2, typeof(UserSeeder)),

            // Phase 3: Listings
            (3, typeof(ListingSeeder)),

            // Phase 4: Listing-Category mappings
            (4, typeof(ListingCategorySeeder))
        };

        var allResults = new List<(string Name, bool Success, long DurationMs)>();

        // Sequential by phase, parallel within each phase (but isolated scopes)
        foreach (var group in seeders.GroupBy(s => s.Phase).OrderBy(g => g.Key))
        {
            _logger.LogInformation("Phase {Phase} ({Count} seeders)", group.Key, group.Count());

            // Run in parallel, but each seeder has its own scope (and DbContext)
            var tasks = group.Select(s => ExecuteSeederAsync(s.SeederType, cancellationToken));
            var results = await Task.WhenAll(tasks);

            allResults.AddRange(results);
        }

        LogSummary(allResults);
    }

    private async Task<(string Name, bool Success, long DurationMs)> ExecuteSeederAsync(
        Type seederType,
        CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var seeder = (ISeeder)scope.ServiceProvider.GetRequiredService(seederType);

        try
        {
            var start = Environment.TickCount64;
            _logger.LogInformation("Starting {SeederName}...", seeder.Name);

            await seeder.SeedAsync(cancellationToken);

            var duration = Environment.TickCount64 - start;
            _logger.LogInformation("{SeederName} completed successfully in {DurationMs} ms", seeder.Name, duration);

            return (seeder.Name, true, duration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed {SeederName}: {Error}", seederType.Name, ex.Message);
            return (seederType.Name, false, 0);
        }
    }

    private void LogSummary(List<(string Name, bool Success, long DurationMs)> results)
    {
        var succeeded = results.Count(r => r.Success);
        var failed = results.Count(r => !r.Success);
        var totalMs = results.Sum(r => r.DurationMs);

        _logger.LogInformation("SEEDING SUMMARY: {Succeeded} succeeded, {Failed} failed, Total {TotalMs} ms",
            succeeded, failed, totalMs);
    }
}
