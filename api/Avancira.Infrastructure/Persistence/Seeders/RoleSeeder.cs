using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Avancira.Shared.Authorization;
using Avancira.Shared.Constants;
using Avancira.Infrastructure.Identity.Roles;

namespace Avancira.Infrastructure.Persistence.Seeders;

internal sealed class RoleSeeder : ISeeder
{
    private readonly RoleManager<Role> _roleManager;
    private readonly ILogger<RoleSeeder> _logger;

    public string Name => nameof(RoleSeeder);

    public RoleSeeder(RoleManager<Role> roleManager, ILogger<RoleSeeder> logger)
    {
        _roleManager = roleManager;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting RoleSeeder...");

        // Define the role-permission mapping
        var rolePermissions = new Dictionary<string, IReadOnlyList<AvanciraPermission>>
        {
            [SeedDefaults.Roles.Admin] = AvanciraPermissions.Admin,
            [SeedDefaults.Roles.Tutor] = AvanciraPermissions.Tutor,
            [SeedDefaults.Roles.Student] = AvanciraPermissions.Student
        };

        foreach (var (roleName, permissions) in rolePermissions)
        {
            // Create role if not exists
            var role = await _roleManager.Roles
                .FirstOrDefaultAsync(r => r.Name == roleName, cancellationToken);

            if (role == null)
            {
                role = new Role(roleName, $"{roleName} role");
                var result = await _roleManager.CreateAsync(role);

                if (result.Succeeded)
                    _logger.LogInformation("Created role: {Role}", roleName);
                else
                {
                    _logger.LogError("Failed to create role {Role}: {Errors}",
                        roleName,
                        string.Join(", ", result.Errors.Select(e => e.Description)));
                    continue;
                }
            }
            else
            {
                _logger.LogInformation("Role {Role} already exists", roleName);
            }

            // Assign permissions (as claims)
            await AssignPermissionsAsync(role, permissions, cancellationToken);
        }

        _logger.LogInformation("RoleSeeder completed successfully.");
    }

    private async Task AssignPermissionsAsync(
        Role role,
        IReadOnlyList<AvanciraPermission> permissions,
        CancellationToken ct)
    {
        var currentClaims = await _roleManager.GetClaimsAsync(role);

        var newClaims = permissions
            .Where(p => !currentClaims.Any(c =>
                c.Type == AvanciraClaims.Permission && c.Value == p.Name))
            .Select(p => new Claim(AvanciraClaims.Permission, p.Name))
            .ToList();

        if (newClaims.Count == 0)
        {
            _logger.LogInformation("No new permissions to add for {Role}", role.Name);
            return;
        }

        foreach (var claim in newClaims)
        {
            await _roleManager.AddClaimAsync(role, claim);
            _logger.LogInformation("Added permission '{Permission}' to role {Role}", claim.Value, role.Name);
        }
    }
}
