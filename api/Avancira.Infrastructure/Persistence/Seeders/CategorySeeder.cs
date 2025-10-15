using Avancira.Domain.Catalog;
using Avancira.Infrastructure.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Avancira.Infrastructure.Persistence.Seeders;

internal sealed class CategorySeeder(
    ILogger<CategorySeeder> logger,
    AvanciraDbContext dbContext
) : BaseSeeder<CategorySeeder>(logger)
{
    public override async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (await dbContext.Categories.AnyAsync(cancellationToken))
        {
            Logger.LogInformation("Skipping {Seeder} — Categories already exist.", nameof(CategorySeeder));
            return;
        }

        var categories = new List<Category>
        {
            new() { Name = "Maths" },
            new() { Name = "Physics" },
            new() { Name = "Chemistry" },
            new() { Name = "Biology" },
            new() { Name = "Science" },
            new() { Name = "Writing", ImageUrl = new Uri("assets/img/categories/cate-8.png", UriKind.Relative), DisplayInLandingPage = true },
            new() { Name = "Marketing", ImageUrl = new Uri("assets/img/categories/cate-10.png", UriKind.Relative), DisplayInLandingPage = true },
            new() { Name = "Finance", ImageUrl = new Uri("assets/img/categories/cate-17.png", UriKind.Relative), DisplayInLandingPage = true },
            new() { Name = "Backend Development", ImageUrl = new Uri("assets/img/categories/cate-26.png", UriKind.Relative), DisplayInLandingPage = true },
            new() { Name = "Photography", ImageUrl = new Uri("assets/img/categories/cate-14.png", UriKind.Relative), DisplayInLandingPage = true },
            new() { Name = "Cloud Computing", ImageUrl = new Uri("assets/img/categories/cate-11.png", UriKind.Relative), DisplayInLandingPage = true },
            new() { Name = "Cybersecurity", ImageUrl = new Uri("assets/img/categories/cate-15.png", UriKind.Relative), DisplayInLandingPage = true },
            new() { Name = "Graphic Design", ImageUrl = new Uri("assets/img/categories/cate-7.png", UriKind.Relative), DisplayInLandingPage = true }
        };

        await dbContext.Categories.AddRangeAsync(categories, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        Logger.LogInformation("{Count} categories seeded successfully.", categories.Count);
    }
}
