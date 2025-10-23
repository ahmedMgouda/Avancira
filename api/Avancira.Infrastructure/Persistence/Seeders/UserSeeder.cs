using Avancira.Infrastructure.Identity.Users;
using Avancira.Shared.Constants;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace Avancira.Infrastructure.Persistence.Seeders;

internal sealed class UserSeeder : ISeeder
{
    private readonly UserManager<User> _userManager;
    private readonly ILogger<UserSeeder> _logger;

    public string Name => nameof(UserSeeder);

    public UserSeeder(
        UserManager<User> userManager,
        ILogger<UserSeeder> logger)
    {
        _userManager = userManager;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting UserSeeder...");

        // Seed Admin
        await SeedUserAsync(
            SeedDefaults.AdminUser.Email,
            SeedDefaults.AdminUser.Username,
            SeedDefaults.AdminUser.Password,
            SeedDefaults.Roles.Admin,
            SeedDefaults.AdminUser.FirstName,
            SeedDefaults.AdminUser.LastName,
            "System administrator account.");

        // Seed Tutor
        await SeedUserAsync(
            SeedDefaults.TutorUser.Email,
            SeedDefaults.TutorUser.Username,
            SeedDefaults.TutorUser.Password,
            SeedDefaults.Roles.Tutor,
            SeedDefaults.TutorUser.FirstName,
            SeedDefaults.TutorUser.LastName,
            "Default tutor account for demonstration.");

        // Seed Student
        await SeedUserAsync(
            SeedDefaults.StudentUser.Email,
            SeedDefaults.StudentUser.Username,
            SeedDefaults.StudentUser.Password,
            SeedDefaults.Roles.Student,
            SeedDefaults.StudentUser.FirstName,
            SeedDefaults.StudentUser.LastName,
            "Default student account for demonstration.");

        _logger.LogInformation("UserSeeder completed successfully.");
    }

    private async Task SeedUserAsync(
        string email,
        string username,
        string password,
        string role,
        string firstName,
        string lastName,
        string bio)
    {
        var existingUser = await _userManager.FindByEmailAsync(email);
        if (existingUser != null)
        {
            _logger.LogInformation("User {Email} already exists. Skipping...", email);
            return;
        }

        var user = new User
        {
            UserName = username,
            Email = email,
            EmailConfirmed = true,
            FirstName = firstName,
            LastName = lastName,
            Bio = bio,
            IsActive = true,
            ImageUrl = new Uri($"https://robohash.org/{username}?size=200x200&set=set1")
        };

        var createResult = await _userManager.CreateAsync(user, password);
        if (!createResult.Succeeded)
        {
            _logger.LogError("Failed to create user {Email}: {Errors}",
                email,
                string.Join(", ", createResult.Errors.Select(e => e.Description)));
            return;
        }

        _logger.LogInformation("Created user {Email}", email);

        var roleResult = await _userManager.AddToRoleAsync(user, role);
        if (roleResult.Succeeded)
        {
            _logger.LogInformation("Assigned role {Role} to {Email}", role, email);
        }
        else
        {
            _logger.LogError("Failed to assign role {Role} to {Email}: {Errors}",
                role,
                email,
                string.Join(", ", roleResult.Errors.Select(e => e.Description)));
        }
    }
}
