using Avancira.Domain.Geography;
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
        _logger.LogInformation("🌍 Seeding Countries...");

        var predefined = new List<Country>
        {
            Country.Create("AU", "Australia", "AUD", "+61"),
            Country.Create("EG", "Egypt", "EGP", "+20"),
            Country.Create("US", "United States", "USD", "+1"),
            Country.Create("GB", "United Kingdom", "GBP", "+44")
        };

        foreach (var country in predefined)
        {
            var existing = await _dbContext.Countries
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == country.Code, cancellationToken);

            if (existing is null)
            {
                _dbContext.Countries.Add(country);
                _logger.LogInformation("✅ Added country {Code} - {Name}", country.Code, country.Name);
            }
            else if (existing.Name != country.Name ||
                     existing.DialingCode != country.DialingCode ||
                     existing.CurrencyCode != country.CurrencyCode)
            {
                _logger.LogInformation("🔄 Updating country {Code}", country.Code);
                existing.Update(country.Name, country.CurrencyCode, country.DialingCode, existing.IsActive);
                _dbContext.Countries.Update(existing);
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("✅ Country seeding completed successfully.");
    }
}
