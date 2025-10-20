using Avancira.Infrastructure.Identity.Users;
using Avancira.Shared.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Avancira.Application.Origin;
using Avancira.Infrastructure.Common;

namespace Avancira.Infrastructure.Persistence.Seeders;

/// <summary>
/// Seeds the default admin user and assigns the Admin role.
/// </summary>
internal sealed class AdminUserSeeder(
    ILogger<AdminUserSeeder> logger,
    UserManager<User> userManager,
    IOptions<OriginOptions> originOptions
) : BaseSeeder<AdminUserSeeder>(logger)
{
    public override async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var email = AppConstants.EmailAddress;
        var userName = AppConstants.AdminUserName;

        var admin = await userManager.Users
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

        if (admin is null)
        {
            admin = new User
            {
                Id = Guid.NewGuid().ToString(),
                FirstName = "System",
                LastName = "Administrator",
                Email = email,
                UserName = userName,
                EmailConfirmed = true,
                PhoneNumberConfirmed = true,
                NormalizedEmail = email.ToUpperInvariant(),
                NormalizedUserName = userName.ToUpperInvariant(),
                IsActive = true
            };

            Logger.LogInformation("Creating default admin user...");
            await userManager.CreateAsync(admin, AppConstants.DefaultPassword);
        }

        if (!await userManager.IsInRoleAsync(admin, AvanciraRoles.Admin))
        {
            Logger.LogInformation("Assigning Admin role to default admin user...");
            await userManager.AddToRoleAsync(admin, AvanciraRoles.Admin);
        }
    }
}
