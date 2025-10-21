using Avancira.Domain.Catalog;
using Microsoft.Extensions.Logging;

namespace Avancira.Infrastructure.Persistence.Seeders;

internal sealed class CategorySeeder : ISeeder
{
    private readonly AvanciraDbContext _context;
    private readonly ILogger<CategorySeeder> _logger;

    public string Name => nameof(CategorySeeder);

    public CategorySeeder(AvanciraDbContext context, ILogger<CategorySeeder> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (_context.Categories.Any())
            return;

        var categories = new[]
        {
            new Category { Name = "Math" },
            new Category { Name = "Physics" },
            new Category { Name = "Chemistry" },
            new Category { Name = "Biology" },
            new Category { Name = "Science" },
            new Category { Name = "Writing", ImageUrl = new Uri("assets/img/categories/cate-8.png", UriKind.Relative), DisplayInLandingPage = true },
            new Category { Name = "Marketing", ImageUrl = new Uri("assets/img/categories/cate-10.png", UriKind.Relative), DisplayInLandingPage = true },
            new Category { Name = "Finance", ImageUrl = new Uri("assets/img/categories/cate-17.png", UriKind.Relative), DisplayInLandingPage = true },
            new Category { Name = "Backend Development", ImageUrl = new Uri("assets/img/categories/cate-26.png", UriKind.Relative), DisplayInLandingPage = true },
            new Category { Name = "Photography", ImageUrl = new Uri("assets/img/categories/cate-14.png", UriKind.Relative), DisplayInLandingPage = true },
            new Category { Name = "Cloud Computing", ImageUrl = new Uri("assets/img/categories/cate-11.png", UriKind.Relative), DisplayInLandingPage = true },
            new Category { Name = "Cybersecurity", ImageUrl = new Uri("assets/img/categories/cate-15.png", UriKind.Relative), DisplayInLandingPage = true },
            new Category { Name = "Graphic Design", ImageUrl = new Uri("assets/img/categories/cate-7.png", UriKind.Relative), DisplayInLandingPage = true }
        };

        _context.Categories.AddRange(categories);
        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Seeded {Count} categories", categories.Length);
    }
}