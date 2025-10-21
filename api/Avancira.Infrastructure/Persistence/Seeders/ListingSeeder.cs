using Avancira.Domain.Catalog;
using Avancira.Domain.Catalog.Enums;
using Avancira.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Avancira.Infrastructure.Persistence.Seeders;

internal sealed class ListingSeeder : ISeeder
{
    private readonly ILogger<ListingSeeder> _logger;
    private readonly AvanciraDbContext _dbContext;

    public string Name => nameof(ListingSeeder);

    public ListingSeeder(
        ILogger<ListingSeeder> logger,
        AvanciraDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting ListingSeeder...");

        if (await _dbContext.Listings.AnyAsync(cancellationToken))
        {
            _logger.LogInformation("Skipping {Seeder} — listings already exist.", nameof(ListingSeeder));
            return;
        }

        // Get all tutor users by role name
        var tutorIds = await (from user in _dbContext.Users
                              join userRole in _dbContext.UserRoles on user.Id equals userRole.UserId
                              join role in _dbContext.Roles on userRole.RoleId equals role.Id
                              where role.Name == SeedDefaults.Roles.Tutor
                              select user.Id)
            .ToListAsync(cancellationToken);

        if (tutorIds.Count == 0)
        {
            _logger.LogWarning("⚠️ No Tutor users found. Please seed Tutor accounts first.");
            return;
        }

        // Get any category and country
        var category = await _dbContext.Categories.FirstOrDefaultAsync(cancellationToken);
        var country = await _dbContext.Countries.FirstOrDefaultAsync(cancellationToken);

        if (category == null || country == null)
        {
            _logger.LogWarning("Cannot seed listings: Missing required Category or Country data.");
            return;
        }

        _logger.LogInformation("Found {TutorCount} tutors — seeding demo listings for each...", tutorIds.Count);

        var listings = new List<Listing>();
        var random = new Random();

        // Demo listing templates
        var templates = new[]
        {
            new { Name = "Advanced Programming Lessons (C++)", Desc = "Master programming concepts in C++ from basics to advanced.", Rate = 50m },
            new { Name = "AWS and DevOps Fundamentals", Desc = "Learn AWS, Docker, and CI/CD pipelines.", Rate = 60m },
            new { Name = "Introduction to Machine Learning", Desc = "Foundational ML lessons using Python and PyTorch.", Rate = 70m },
            new { Name = "Backend Development with .NET", Desc = "Build robust REST APIs with ASP.NET Core.", Rate = 65m },
            new { Name = "Photography Masterclass", Desc = "Learn composition, lighting, and editing.", Rate = 55m },
            new { Name = "Video Editing with Adobe Premiere Pro", Desc = "Edit videos like a pro with Adobe Premiere.", Rate = 60m }
        };

        // Distribute listings among tutors
        foreach (var tutorId in tutorIds)
        {
            foreach (var template in templates.OrderBy(_ => random.Next()).Take(3)) // 3 random listings per tutor
            {
                listings.Add(Listing.Create(
                    createdById: tutorId,
                    hourlyRate: template.Rate,
                    name: template.Name,
                    description: template.Desc,
                    locationType: ListingLocationType.Webcam | ListingLocationType.TutorLocation
                ));
            }
        }

        await _dbContext.Listings.AddRangeAsync(listings, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("{Count} demo listings seeded successfully for {TutorCount} tutors.",
            listings.Count, tutorIds.Count);
    }
}
