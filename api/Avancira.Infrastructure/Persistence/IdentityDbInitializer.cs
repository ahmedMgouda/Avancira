using Avancira.Application.Origin;
using Avancira.Application.Persistence;
using Avancira.Infrastructure.Identity.Roles;
using Avancira.Infrastructure.Identity.Users;
using Avancira.Shared.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;

namespace Avancira.Infrastructure.Persistence;
internal sealed class IdentityDbInitializer(
    ILogger<IdentityDbInitializer> logger,
    AvanciraDbContext context,
    RoleManager<Role> roleManager,
    UserManager<User> userManager,
    TimeProvider timeProvider,
    IOptions<OriginOptions> originSettings,
    IOpenIddictApplicationManager applicationManager) : IDbInitializer
{
    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        await SeedRolesAsync();
        await SeedAdminUserAsync();
        await SeedOpenIddictApplicationsAsync();

        CountrySeeder.Seed(context, userManager);
        UserSeeder.Seed(context, userManager);
        CategorySeeder.Seed(context, userManager);
        ListingSeeder.Seed(context, userManager);
        ListingCategorySeeder.Seed(context, userManager);

        PromoCodeSeeder.Seed(context);
        // LessonSeeder.Seed(context);
        // ReviewSeeder.Seed(context);
        // ChatSeeder.Seed(context);
        // MessageSeeder.Seed(context);

    }

    private async Task SeedOpenIddictApplicationsAsync()
    {
        const string clientId = "avancira-web";
        if (await applicationManager.FindByClientIdAsync(clientId) is null)
        {
            var descriptor = new OpenIddictApplicationDescriptor
            {
                ClientId = clientId,
                ClientSecret = "client-secret"
            };

            if (originSettings.Value.OriginUrl is { } origin)
            {
                descriptor.RedirectUris.Add(origin);
            }

            descriptor.Permissions.Add(OpenIddictConstants.Permissions.Endpoints.Token);

            await applicationManager.CreateAsync(descriptor);
        }
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
                Id = Guid.NewGuid().ToString(),
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
            await userManager.CreateAsync(adminUser, AppConstants.DefaultPassword);
        }

        if (!await userManager.IsInRoleAsync(adminUser, AvanciraRoles.Admin))
        {
            logger.LogInformation("Assigning Admin role to Admin user");
            await userManager.AddToRoleAsync(adminUser, AvanciraRoles.Admin);
        }
    }
}
