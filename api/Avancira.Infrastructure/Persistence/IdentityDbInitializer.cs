using Avancira.Application.Origin;
using Avancira.Application.Persistence;
using Avancira.Infrastructure.Identity.Roles;
using Avancira.Infrastructure.Identity.Users;
using Avancira.Shared.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Avancira.Infrastructure.Persistence;
internal sealed class IdentityDbInitializer(
    ILogger<IdentityDbInitializer> logger,
    AvanciraDbContext context,
    RoleManager<Role> roleManager,
    UserManager<User> userManager,
    TimeProvider timeProvider,
    IOptions<OriginOptions> originSettings) : IDbInitializer
{
    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        await SeedRolesAsync();
        await SeedAdminUserAsync();
        await SeedDefaultUsersAsync();
    }

    private async Task SeedRolesAsync()
    {
        foreach (string roleName in AvanciraRoles.DefaultRoles)
        {
            if (await roleManager.Roles.SingleOrDefaultAsync(r => r.Name == roleName) is not Role role)
            {
                // Create role
                role = new Role(roleName, $"{roleName} Role");
                await roleManager.CreateAsync(role);
            }

            // Assign permissions
            if (roleName == AvanciraRoles.Basic)
            {
                await AssignPermissionsToRoleAsync(context, AvanciraPermissions.Basic, role);
            }
            else if (roleName == AvanciraRoles.Admin)
            {
                await AssignPermissionsToRoleAsync(context, AvanciraPermissions.Admin, role);
                await AssignPermissionsToRoleAsync(context, AvanciraPermissions.Root, role); // Always assign root permissions now
            }
        }
    }

    private async Task AssignPermissionsToRoleAsync(AvanciraDbContext dbContext, IReadOnlyList<AvanciraPermission> permissions, Role role)
    {
        var currentClaims = await roleManager.GetClaimsAsync(role);
        var newClaims = permissions
            .Where(permission => !currentClaims.Any(c => c.Type == AvanciraClaims.Permission && c.Value == permission.Name))
            .Select(permission => new RoleClaim
            {
                RoleId = role.Id,
                ClaimType = AvanciraClaims.Permission,
                ClaimValue = permission.Name,
                CreatedBy = "application",
                CreatedOn = timeProvider.GetUtcNow()
            })
            .ToList();

        foreach (var claim in newClaims)
        {
            logger.LogInformation("Seeding {Role} Permission '{Permission}'", role.Name, claim.ClaimValue);
            await dbContext.RoleClaims.AddAsync(claim);
        }

        if (newClaims.Count != 0)
        {
            await dbContext.SaveChangesAsync();
        }
    }

    private async Task SeedAdminUserAsync()
    {
        if (await userManager.Users.FirstOrDefaultAsync(u => u.Email == AppConstants.EmailAddress) is not User adminUser)
        {
            adminUser = new User
            {
                FirstName = "System",
                LastName = "Administrator",
                Email = AppConstants.EmailAddress,
                UserName = AppConstants.AdminUserName,
                EmailConfirmed = true,
                PhoneNumberConfirmed = true,
                NormalizedEmail = AppConstants.EmailAddress.ToUpperInvariant(),
                NormalizedUserName = AppConstants.AdminUserName.ToUpperInvariant(),
                ImageUrl = new Uri(originSettings.Value.OriginUrl! + AppConstants.DefaultProfilePicture),
                IsActive = true
            };

            logger.LogInformation("Seeding default Admin user");
            var password = new PasswordHasher<User>();
            adminUser.PasswordHash = password.HashPassword(adminUser, AppConstants.DefaultPassword);
            await userManager.CreateAsync(adminUser);
        }

        if (!await userManager.IsInRoleAsync(adminUser, AvanciraRoles.Admin))
        {
            logger.LogInformation("Assigning Admin role to Admin user");
            await userManager.AddToRoleAsync(adminUser, AvanciraRoles.Admin);
        }
    }

    private async Task SeedDefaultUsersAsync()
    {
        var defaultUsers = new List<User>
        {
                new User
                {
                    FirstName = "Tutor",
                    LastName = "",
                    //Bio = "An experienced tutor in various subjects, passionate about helping students achieve their full potential. Offers personalized lessons in multiple areas to cater to diverse learning needs.",
                    UserName = "tutor@avancira.com",
                    Email = "tutor@avancira.com",
                    //Address = new Address
                    //{
                    //    StreetAddress = "101 Grafton Street",
                    //    City = "Bondi Junction",
                    //    State = "NSW",
                    //    Country = "Australia",
                    //    PostalCode = "2022",
                    //    Latitude = -33.8912,
                    //    Longitude = 151.2646,
                    //    FormattedAddress = "101 Grafton Street, Bondi Junction NSW 2022, Australia"
                    //},
                    //CountryId = context.Countries.FirstOrDefault(c => EF.Functions.Like(c.Code, "AU"))?.Id ?? null,
                    TimeZoneId = "Australia/Sydney",
                    //ProfileImageUrl = $"https://robohash.org/{Guid.NewGuid()}?size=200x200&set=set1",
                },
                new User
                {
                    FirstName = "Student",
                    LastName = "",
                    //Bio = "A dedicated student, always eager to learn and expand knowledge. Focused on achieving academic success with the support of talented tutors and mentors.",
                    UserName = "student@avancira.com",
                    Email = "student@avancira.com",
                    //Address = new Address
                    //{
                    //    StreetAddress = "22 Bronte Road",
                    //    City = "Bondi Junction",
                    //    State = "NSW",
                    //    Country = "Australia",
                    //    PostalCode = "2022",
                    //    Latitude = -33.8915,
                    //    Longitude = 151.2691,
                    //    FormattedAddress = "22 Bronte Road, Bondi Junction NSW 2022, Australia"
                    //},
                    //CountryId = context.Countries.FirstOrDefault(c => EF.Functions.Like(c.Code, "AU"))?.Id ?? null,
                    TimeZoneId = "Australia/Sydney",
                    //ProfileImageUrl = $"https://robohash.org/{Guid.NewGuid()}?size=200x200&set=set1",
                },
        };

        var password = new PasswordHasher<User>();
        foreach (var user in defaultUsers)
        {
            if (await userManager.FindByEmailAsync(user.Email) is null)
            {
                user.PasswordHash = password.HashPassword(user, AppConstants.DefaultPassword);
                await userManager.CreateAsync(user);
                await userManager.AddToRoleAsync(user, AvanciraRoles.Basic);
                logger.LogInformation("Seeded user {Email}", user.Email);
            }
        }
    }

}
