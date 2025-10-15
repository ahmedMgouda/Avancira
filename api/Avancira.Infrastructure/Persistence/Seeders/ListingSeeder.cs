using Avancira.Domain.Catalog;
using Avancira.Domain.Catalog.Enums;
using Avancira.Infrastructure.Common;
using Avancira.Infrastructure.Identity.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Avancira.Infrastructure.Persistence.Seeders;

/// <summary>
/// Seeds demo listings for tutors across various categories.
/// </summary>
internal sealed class ListingSeeder(
    ILogger<ListingSeeder> logger,
    AvanciraDbContext dbContext,
    UserManager<User> userManager
) : BaseSeeder<ListingSeeder>(logger)
{
    public override async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (await dbContext.Listings.AnyAsync(cancellationToken))
        {
            Logger.LogInformation("Skipping {Seeder} — listings already exist.", nameof(ListingSeeder));
            return;
        }

        var amr = await userManager.Users
            .Where(u => u.Email == "Amr.Mostafa@live.com")
            .Select(u => u.Id)
            .FirstOrDefaultAsync(cancellationToken);

        var amir = await userManager.Users
            .Where(u => u.Email == "Amir.Salah@live.com")
            .Select(u => u.Id)
            .FirstOrDefaultAsync(cancellationToken);

        var ahmed = await userManager.Users
            .Where(u => u.Email == "Ahmed.Mostafa@live.com")
            .Select(u => u.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (string.IsNullOrEmpty(amr) || string.IsNullOrEmpty(amir) || string.IsNullOrEmpty(ahmed))
        {
            Logger.LogWarning("Skipping {Seeder} — some users not found (Amr, Amir, Ahmed).", nameof(ListingSeeder));
            return;
        }

        var listings = new List<Listing>
        {
            Listing.Create(
                createdById: amr,
                hourlyRate: 50m,
                name: "Advanced Programming Lessons (C++)",
                description: "Master programming concepts with hands-on lessons in C++ for beginners to advanced levels.",
                locationType: ListingLocationType.Webcam | ListingLocationType.StudentLocation
            ),
            Listing.Create(
                createdById: amr,
                hourlyRate: 60m,
                name: "AWS and DevOps Fundamentals",
                description: "Learn AWS services (EC2, S3, RDS) and CI/CD pipelines with Jenkins and Docker.",
                locationType: ListingLocationType.Webcam | ListingLocationType.TutorLocation
            ),
            Listing.Create(
                createdById: amr,
                hourlyRate: 70m,
                name: "Introduction to Machine Learning",
                description: "Foundational machine learning lessons using Python, scikit-learn, and PyTorch.",
                locationType: ListingLocationType.Webcam | ListingLocationType.StudentLocation
            ),
            Listing.Create(
                createdById: amr,
                hourlyRate: 65m,
                name: "Backend Development with .NET",
                description: "Master backend development using .NET and build robust REST APIs.",
                locationType: ListingLocationType.Webcam | ListingLocationType.TutorLocation
            ),
            Listing.Create(
                createdById: amir,
                hourlyRate: 70m,
                name: "Financial Planning & Investment Strategies",
                description: "Learn personal and corporate finance, investments, and risk management.",
                locationType: ListingLocationType.Webcam | ListingLocationType.StudentLocation
            ),
            Listing.Create(
                createdById: ahmed,
                hourlyRate: 55m,
                name: "Photography Masterclass: From Beginner to Pro",
                description: "Learn the fundamentals of photography, lighting, and composition.",
                locationType: ListingLocationType.Webcam | ListingLocationType.StudentLocation
            ),
            Listing.Create(
                createdById: ahmed,
                hourlyRate: 60m,
                name: "Video Editing with Adobe Premiere Pro",
                description: "Learn video editing and storytelling techniques using Premiere Pro and After Effects.",
                locationType: ListingLocationType.Webcam | ListingLocationType.TutorLocation
            )
        };

        await dbContext.Listings.AddRangeAsync(listings, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        Logger.LogInformation("{Count} listings seeded successfully.", listings.Count);
    }
}
