using System.Collections.Generic;
using Avancira.Domain.Geography;
using Avancira.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Avancira.Infrastructure.Persistence.Seeders;

internal sealed class CountrySeeder : ISeeder
{
    private readonly AvanciraDbContext _dbContext;
    private readonly ILogger<CountrySeeder> _logger;

    public string Name => nameof(CountrySeeder);

    public CountrySeeder(AvanciraDbContext dbContext, ILogger<CountrySeeder> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Seeding countries...");

        var countries = new List<Country>
        {
            Country.Create("AU", "Australia", "AUD", "+61"),
            Country.Create("EG", "Egypt", "EGP", "+20"),
            Country.Create("US", "United States", "USD", "+1"),
            Country.Create("GB", "United Kingdom", "GBP", "+44")
        };

        foreach (var country in countries)
        {
            bool exists = await _dbContext.Countries.AnyAsync(c => c.Code == country.Code, cancellationToken);
            if (!exists)
            {
                _dbContext.Countries.Add(country);
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Country seeding completed.");
    }
}
