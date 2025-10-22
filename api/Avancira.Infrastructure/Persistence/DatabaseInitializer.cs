using Avancira.Infrastructure.Persistence.Seeders;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Avancira.Infrastructure.Persistence;

/// <summary>
/// Handles database creation, migration, and data seeding orchestration.
/// Ensures migrations run before seeders and logs every phase clearly.
/// </summary>
internal sealed class DatabaseInitializer : IDatabaseInitializer
{
    private readonly AvanciraDbContext _context;
    private readonly DataSeederOrchestrator _orchestrator;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DatabaseInitializer> _logger;
    private static readonly SemaphoreSlim _lock = new(1, 1);

    public DatabaseInitializer(
        AvanciraDbContext context,
        DataSeederOrchestrator orchestrator,
        IConfiguration configuration,
        ILogger<DatabaseInitializer> logger)
    {
        _context = context;
        _orchestrator = orchestrator;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            _logger.LogInformation("=== Database Initialization Started ===");

            var dropDatabase = _configuration.GetValue("ASPIRE_DROP_DATABASE", false);
            var runSeeding = _configuration.GetValue("ASPIRE_RUN_SEEDING", true);

            if (dropDatabase)
            {
                _logger.LogWarning("ASPIRE_DROP_DATABASE=true → Dropping existing database...");
                await _context.Database.EnsureDeletedAsync(cancellationToken);
                _logger.LogWarning("Database dropped successfully.");
            }

            // 1️⃣ Ensure DB physically exists
            await _context.Database.EnsureCreatedAsync(cancellationToken);

            // 2️⃣ Apply migrations only if there are any
            var pendingMigrations = (await _context.Database.GetPendingMigrationsAsync(cancellationToken)).ToList();
            if (pendingMigrations.Any())
            {
                _logger.LogInformation("Applying {Count} pending migrations...", pendingMigrations.Count);
                await _context.Database.MigrateAsync(cancellationToken);
                _logger.LogInformation("✅ Migrations applied successfully.");
            }

            // 3️⃣ Run seeding last
            if (runSeeding)
            {
                _logger.LogInformation("Running data seeders...");
                await _orchestrator.RunAsync(cancellationToken);
                _logger.LogInformation("✅ Database seeding completed.");
            }

            _logger.LogInformation("=== Database Initialization Completed Successfully ===");
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Database initialization failed: {Error}", ex.Message);
            throw;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<bool> IsInitializedAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var canConnect = await _context.Database.CanConnectAsync(cancellationToken);
            if (!canConnect)
                return false;

            var applied = await _context.Database.GetAppliedMigrationsAsync(cancellationToken);
            return applied.Any();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Database initialization check failed: {Error}", ex.Message);
            return false;
        }
    }
}
