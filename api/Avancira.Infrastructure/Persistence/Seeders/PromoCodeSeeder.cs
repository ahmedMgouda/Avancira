using Avancira.Domain.PromoCodes;
using Avancira.Infrastructure.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Avancira.Infrastructure.Persistence.Seeders;

internal sealed class PromoCodeSeeder(
    ILogger<PromoCodeSeeder> logger,
    AvanciraDbContext dbContext
) : BaseSeeder<PromoCodeSeeder>(logger)
{
    public override async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (await dbContext.PromoCodes.AnyAsync(cancellationToken))
        {
            Logger.LogInformation("Skipping {Seeder} — PromoCodes already exist.", nameof(PromoCodeSeeder));
            return;
        }

        var promoCodes = new List<PromoCode>
        {
            new() { Code = "WELCOME10", DiscountAmount = 10, IsActive = true },
            new() { Code = "WELCOME20", DiscountAmount = 20, IsActive = true },
            new() { Code = "WELCOME25PCT", DiscountPercentage = 25, IsActive = true },
            new() { Code = "WELCOME50PCT", DiscountPercentage = 50, IsActive = true },
            new() { Code = "WELCOME100PCT", DiscountPercentage = 100, IsActive = true }
        };

        await dbContext.PromoCodes.AddRangeAsync(promoCodes, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        Logger.LogInformation("{Count} promo codes seeded successfully.", promoCodes.Count);
    }
}
