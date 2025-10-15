using Avancira.Infrastructure.Catalog;
using Avancira.Infrastructure.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Avancira.Infrastructure.Persistence.Seeders;

internal sealed class CountrySeeder(
    ILogger<CountrySeeder> logger,
    AvanciraDbContext dbContext
) : BaseSeeder<CountrySeeder>(logger)
{
    public override async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (await dbContext.Countries.AnyAsync(cancellationToken))
        {
            Logger.LogInformation("Skipping {Seeder} — Countries already exist.", nameof(CountrySeeder));
            return;
        }

        var countries = new List<Country>
        {
            new() { Name = "Australia", Code = "AU" }
        };

        await dbContext.Countries.AddRangeAsync(countries, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        Logger.LogInformation("{Count} country records seeded successfully.", countries.Count);
    }
}
