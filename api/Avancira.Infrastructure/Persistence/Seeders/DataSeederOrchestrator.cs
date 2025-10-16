using Avancira.Infrastructure.Identity.Seeders;
using Avancira.Infrastructure.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Avancira.Infrastructure.Persistence.Seeders;

internal sealed class DataSeederOrchestrator(
    ILogger<DataSeederOrchestrator> logger,
    IServiceProvider serviceProvider
)
{
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("🚀 Starting database seeding...");

        using var scope = serviceProvider.CreateScope();
        var sp = scope.ServiceProvider;

        var roleSeeder = sp.GetRequiredService<RoleSeeder>();
        var userSeeder = sp.GetRequiredService<UserSeeder>();
        var openIdSeeder = sp.GetRequiredService<OpenIddictClientSeeder>();

        var countrySeeder = sp.GetRequiredService<CountrySeeder>();
        var categorySeeder = sp.GetRequiredService<CategorySeeder>();
        var listingSeeder = sp.GetRequiredService<ListingSeeder>();
        var listingCategorySeeder = sp.GetRequiredService<ListingCategorySeeder>();
        var promoCodeSeeder = sp.GetRequiredService<PromoCodeSeeder>();

        // Order matters (dependencies)
        await SafeRunAsync(roleSeeder, nameof(RoleSeeder), cancellationToken);
      
        await SafeRunAsync(categorySeeder, nameof(CategorySeeder), cancellationToken);
        await SafeRunAsync(countrySeeder, nameof(CountrySeeder), cancellationToken);

        await SafeRunAsync(promoCodeSeeder, nameof(PromoCodeSeeder), cancellationToken);
       
        await SafeRunAsync(userSeeder, nameof(UserSeeder), cancellationToken);
        await SafeRunAsync(openIdSeeder, nameof(OpenIddictClientSeeder), cancellationToken);

        await SafeRunAsync(listingSeeder, nameof(ListingSeeder), cancellationToken);
        await SafeRunAsync(listingCategorySeeder, nameof(ListingCategorySeeder), cancellationToken);

        logger.LogInformation("Database seeding completed successfully.");
    }

    private static async Task SafeRunAsync(BaseSeeder seeder, string name, CancellationToken ct)
    {
        try
        {
            Console.WriteLine($"Running {name}...");
            await seeder.SeedAsync(ct);
            Console.WriteLine($"{name} completed.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"{name} failed: {ex.Message}");
        }
    }
}
