using Avancira.Infrastructure.Persistence.Seeders;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Avancira.Infrastructure.Persistence;

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
            if (await IsInitializedAsync(cancellationToken))
            {
                _logger.LogInformation("Database already initialized");
                return;
            }

            _logger.LogInformation("Initializing database...");

            await _context.Database.MigrateAsync(cancellationToken);
            _logger.LogInformation("Migrations applied");

            var runSeeding = _configuration.GetValue("ASPIRE_RUN_SEEDING", true);
            if (runSeeding)
            {
                await _orchestrator.RunAsync(cancellationToken);
                _logger.LogInformation("Seeding completed");
            }
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

            var appliedMigrations = await _context.Database
                .GetAppliedMigrationsAsync(cancellationToken);

            return appliedMigrations.Any();
        }
        catch
        {
            return false;
        }
    }
}