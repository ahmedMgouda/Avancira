using Avancira.Application.Persistence;
using Avancira.Infrastructure.Identity.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace Avancira.Infrastructure.Persistence;
internal sealed class AvanciraDbInitializer(
    ILogger<AvanciraDbInitializer> logger,
    AvanciraDbContext context,
    UserManager<User> userManager) : IDbInitializer
{
    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Starting Avancira application data seeding");

        CountrySeeder.Seed(context, userManager);
        UserSeeder.Seed(context, userManager);
        CategorySeeder.Seed(context, userManager);
        ListingSeeder.Seed(context, userManager);
        ListingCategorySeeder.Seed(context, userManager);

        PromoCodeSeeder.Seed(context);
        // LessonSeeder.Seed(context);
        // ReviewSeeder.Seed(context);
        // ChatSeeder.Seed(context);
        // MessageSeeder.Seed(context);

        logger.LogInformation("Avancira application data seeding completed");
    }
}
