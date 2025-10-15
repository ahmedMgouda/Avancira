using Avancira.Infrastructure.Catalog;
using Avancira.Infrastructure.Common;
using Avancira.Infrastructure.Identity.Users;
using Avancira.Shared.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;


namespace Avancira.Infrastructure.Persistence.Seeders;

internal sealed class UserSeeder(
    ILogger<UserSeeder> logger,
    AvanciraDbContext dbContext,
    UserManager<User> userManager
) : BaseSeeder<UserSeeder>(logger)
{
    public override async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var userCount = await dbContext.Users.CountAsync(cancellationToken);
        if (userCount > 1)
        {
            Logger.LogInformation("Skipping {Seeder} — Users already exist.", nameof(UserSeeder));
            return;
        }

        var auCountryId = await dbContext.Countries
            .Where(c => c.Code == "AU")
            .Select(c => c.Id)
            .FirstOrDefaultAsync(cancellationToken);

        var users = CreateDefaultUsers(auCountryId);

        foreach (var user in users)
        {
            if (await userManager.FindByEmailAsync(user.Email) != null)
                continue;

            await userManager.CreateAsync(user, AppConstants.DefaultPassword);
            await userManager.AddToRoleAsync(user, AvanciraRoles.Basic);
        }

        Logger.LogInformation("{Count} demo users seeded successfully.", users.Count);
    }

    private static List<User> CreateDefaultUsers(int countryId) => new()
    {
        new User
        {
            FirstName = "Tutor",
            UserName = "tutor@avancira.com",
            Email = "tutor@avancira.com",
            Bio = "An experienced tutor helping students achieve their goals.",
            CountryId = countryId,
            Address = new Address
            {
                StreetAddress = "101 Grafton Street",
                City = "Bondi Junction",
                State = "NSW",
                Country = "Australia",
                PostalCode = "2022"
            },
            TimeZoneId = "Australia/Sydney",
            ImageUrl = new Uri("https://robohash.org/tutor?size=200x200&set=set1"),
            IsActive = true,
            EmailConfirmed = true
        },
        new User
        {
            FirstName = "Student",
            UserName = "student@avancira.com",
            Email = "student@avancira.com",
            Bio = "A dedicated student eager to learn and grow.",
            CountryId = countryId,
            Address = new Address
            {
                StreetAddress = "22 Bronte Road",
                City = "Bondi Junction",
                State = "NSW",
                Country = "Australia",
                PostalCode = "2022"
            },
            TimeZoneId = "Australia/Sydney",
            ImageUrl = new Uri("https://robohash.org/student?size=200x200&set=set1"),
            IsActive = true,
            EmailConfirmed = true
        }
    };
}

