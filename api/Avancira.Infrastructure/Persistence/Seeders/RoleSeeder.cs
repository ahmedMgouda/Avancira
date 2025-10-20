using Avancira.Infrastructure.Common;
using Avancira.Infrastructure.Identity.Roles;
using Avancira.Infrastructure.Persistence;
using Avancira.Shared.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Avancira.Infrastructure.Persistence.Seeders;

/// <summary>
/// Seeds default system roles and permissions.
/// </summary>
internal sealed class RoleSeeder(
    ILogger<RoleSeeder> logger,
    RoleManager<Role> roleManager,
    AvanciraDbContext dbContext,
    TimeProvider timeProvider
) : BaseSeeder<RoleSeeder>(logger)
{
    public override async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        foreach (var roleName in AvanciraRoles.DefaultRoles)
        {
            var role = await roleManager.Roles
                .SingleOrDefaultAsync(r => r.Name == roleName, cancellationToken);

            if (role is null)
            {
                role = new Role(roleName, $"{roleName} Role");
                await roleManager.CreateAsync(role);
                Logger.LogInformation("Created role: {Role}", roleName);
            }

            if (roleName == AvanciraRoles.Basic)
                await AssignPermissionsAsync(role, AvanciraPermissions.Basic, cancellationToken);
            else if (roleName == AvanciraRoles.Admin)
            {
                await AssignPermissionsAsync(role, AvanciraPermissions.Admin, cancellationToken);
                await AssignPermissionsAsync(role, AvanciraPermissions.Root, cancellationToken);
            }
        }
    }

    private async Task AssignPermissionsAsync(Role role, IReadOnlyList<AvanciraPermission> permissions, CancellationToken ct)
    {
        var currentClaims = await roleManager.GetClaimsAsync(role);

        var newClaims = permissions
            .Where(p => !currentClaims.Any(c =>
                c.Type == AvanciraClaims.Permission && c.Value == p.Name))
            .Select(p => new RoleClaim
            {
                RoleId = role.Id,
                ClaimType = AvanciraClaims.Permission,
                ClaimValue = p.Name,
                CreatedBy = "system",
                CreatedOn = timeProvider.GetUtcNow()
            })
            .ToList();

        if (newClaims.Count == 0)
            return;

        await dbContext.RoleClaims.AddRangeAsync(newClaims, ct);
        await dbContext.SaveChangesAsync(ct);

        foreach (var claim in newClaims)
            Logger.LogInformation("Added permission '{Permission}' to role {Role}", claim.ClaimValue, role.Name);
    }
}
