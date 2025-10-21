using Avancira.Infrastructure.Catalog;
using Microsoft.Extensions.Logging;

namespace Avancira.Infrastructure.Persistence.Seeders;

internal sealed class CountrySeeder : ISeeder
{
    private readonly AvanciraDbContext _context;
    private readonly ILogger<CountrySeeder> _logger;

    public string Name => nameof(CountrySeeder);

    public CountrySeeder(AvanciraDbContext context, ILogger<CountrySeeder> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (_context.Countries.Any())
            return;

        var countries = new[]
        {
            new Country { Name = "Australia", Code = "AU" },
            new Country { Code = "US", Name = "United States" },
            new Country { Code = "UK", Name = "United Kingdom" },
            new Country { Code = "CA", Name = "Canada" },
            new Country { Code = "EG", Name = "Egypt" }
        };

        _context.Countries.AddRange(countries);
        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Seeded {Count} countries", countries.Length);
    }
}