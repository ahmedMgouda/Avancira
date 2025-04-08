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
        const string adminEmail = "admin@system.local";
        const string adminUserName = "ADMIN";
        const string defaultProfilePicture = "/images/profile/default.png"; // <- set this to your actual default path
        const string defaultPassword = "Pa$$w0rd!"; // <- use a strong password or read from config

        if (await userManager.Users.FirstOrDefaultAsync(u => u.Email == adminEmail) is not User adminUser)
        {
            adminUser = new User
            {
                FirstName = "System",
                LastName = "Administrator",
                Email = adminEmail,
                UserName = adminUserName,
                EmailConfirmed = true,
                PhoneNumberConfirmed = true,
                NormalizedEmail = adminEmail.ToUpperInvariant(),
                NormalizedUserName = adminUserName.ToUpperInvariant(),
                ImageUrl = new Uri(originSettings.Value.OriginUrl! + defaultProfilePicture),
                IsActive = true
            };

            logger.LogInformation("Seeding default Admin user");
            var password = new PasswordHasher<User>();
            adminUser.PasswordHash = password.HashPassword(adminUser, defaultPassword);
            await userManager.CreateAsync(adminUser);
        }

        if (!await userManager.IsInRoleAsync(adminUser, AvanciraRoles.Admin))
        {
            logger.LogInformation("Assigning Admin role to Admin user");
            await userManager.AddToRoleAsync(adminUser, AvanciraRoles.Admin);
        }
    }
}
