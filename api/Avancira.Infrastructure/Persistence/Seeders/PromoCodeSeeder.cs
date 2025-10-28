using Avancira.Domain.PromoCodes;
using Microsoft.Extensions.Logging;

namespace Avancira.Infrastructure.Persistence.Seeders;

internal sealed class PromoCodeSeeder : ISeeder
{
    private readonly AvanciraDbContext _context;
    private readonly ILogger<PromoCodeSeeder> _logger;

    public string Name => nameof(PromoCodeSeeder);

    public PromoCodeSeeder(AvanciraDbContext context, ILogger<PromoCodeSeeder> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (_context.PromoCodes.Any())
            return;

        var promoCodes = new[]
        {
            new PromoCode { Code = "WELCOME10", DiscountAmount = 10, IsActive = true },
            new PromoCode { Code = "SAVE20", DiscountAmount = 20, IsActive = true }
        };

        _context.PromoCodes.AddRange(promoCodes);
        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Seeded {Count} promo codes", promoCodes.Length);
    }
}