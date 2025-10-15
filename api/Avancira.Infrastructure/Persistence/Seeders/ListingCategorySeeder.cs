using Avancira.Infrastructure.Common;
using Avancira.Domain.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Avancira.Infrastructure.Persistence.Seeders;

/// <summary>
/// Maps demo listings to their related categories.
/// </summary>
internal sealed class ListingCategorySeeder(
    ILogger<ListingCategorySeeder> logger,
    AvanciraDbContext dbContext
) : BaseSeeder<ListingCategorySeeder>(logger)
{
    public override async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (await dbContext.ListingCategories.AnyAsync(cancellationToken))
        {
            Logger.LogInformation("Skipping {Seeder} — listing categories already exist.", nameof(ListingCategorySeeder));
            return;
        }

        // Helper to find IDs by name (case-insensitive)
        async Task<Guid?> FindListingIdAsync(string name) =>
            await dbContext.Listings
                .Where(l => EF.Functions.ILike(l.Name, name))
                .Select(l => (Guid?)l.Id)
                .FirstOrDefaultAsync(cancellationToken);

        async Task<Guid?> FindCategoryIdAsync(string name) =>
            await dbContext.Categories
                .Where(c => EF.Functions.ILike(c.Name, name))
                .Select(c => (Guid?)c.Id)
                .FirstOrDefaultAsync(cancellationToken);

        var pairs = new (string Listing, string Category)[]
        {
            ("Advanced Programming Lessons (C++)", "C++"),
            ("AWS and DevOps Fundamentals", "AWS"),
            ("Introduction to Machine Learning", "Machine Learning"),
            ("Backend Development with .NET", "Backend Development"),
            ("Financial Planning & Investment Strategies", "Finance"),
            ("Photography Masterclass: From Beginner to Pro", "Photography"),
            ("Video Editing with Adobe Premiere Pro", "Video Editing")
        };

        var listingCategories = new List<ListingCategory>();

        foreach (var (listingName, categoryName) in pairs)
        {
            var listingId = await FindListingIdAsync(listingName);
            var categoryId = await FindCategoryIdAsync(categoryName);

            if (listingId is null || categoryId is null)
            {
                Logger.LogWarning("Skipping mapping: {Listing} → {Category} (missing ID)", listingName, categoryName);
                continue;
            }

            listingCategories.Add(new ListingCategory
            {
                ListingId = listingId.Value,
                CategoryId = categoryId.Value
            });
        }

        if (listingCategories.Count == 0)
        {
            Logger.LogWarning("No listing-category pairs to insert.");
            return;
        }

        await dbContext.ListingCategories.AddRangeAsync(listingCategories, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        Logger.LogInformation("{Count} listing-category pairs seeded successfully.", listingCategories.Count);
    }
}
