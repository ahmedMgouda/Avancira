using Avancira.Domain.Catalog;
using Microsoft.Extensions.Logging;

namespace Avancira.Infrastructure.Persistence.Seeders;

internal sealed class ListingCategorySeeder : ISeeder
{
    private readonly AvanciraDbContext _context;
    private readonly ILogger<ListingCategorySeeder> _logger;

    public string Name => nameof(ListingCategorySeeder);

    public ListingCategorySeeder(AvanciraDbContext context, ILogger<ListingCategorySeeder> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (_context.ListingCategories.Any())
            return;

        var listings = _context.Listings.ToList();
        var categories = _context.Categories.ToList();

        if (!listings.Any() || !categories.Any())
        {
            _logger.LogWarning("Cannot seed listing categories: Missing dependencies");
            return;
        }

        var listingCategories = new List<ListingCategory>();

        foreach (var listing in listings)
        {
            var category = categories.FirstOrDefault();
            if (category != null)
            {
                listingCategories.Add(new ListingCategory
                {
                    ListingId = listing.Id,
                    CategoryId = category.Id
                });
            }
        }

        _context.ListingCategories.AddRange(listingCategories);
        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Seeded {Count} listing categories", listingCategories.Count);
    }
}