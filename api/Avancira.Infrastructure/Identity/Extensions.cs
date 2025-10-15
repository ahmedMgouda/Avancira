using Avancira.Application.Audit;
using Avancira.Application.Identity.Roles;
using Avancira.Application.Identity.Users.Abstractions;
using Avancira.Application.Persistence;
using Avancira.Infrastructure.Auth;
using Avancira.Infrastructure.Identity.Audit;
using Avancira.Infrastructure.Identity.Roles;
using Avancira.Infrastructure.Identity.Seeders;
using Avancira.Infrastructure.Identity.Users;
using Avancira.Infrastructure.Identity.Users.Services;
using Avancira.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using IdentityConstants = Avancira.Shared.Authorization.IdentityConstants;

namespace Avancira.Infrastructure.Identity;

internal static class Extensions
{
    /// <summary>
    /// Configures ASP.NET Core Identity and registers identity-related seeders.
    /// Shared between Auth and API projects.
    /// </summary>
    internal static IServiceCollection ConfigureIdentity(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // ===== Current User Services =====
        services.AddScoped<CurrentUserMiddleware>();
        services.AddScoped<ICurrentUser, CurrentUser>();
        services.AddScoped(sp => (ICurrentUserInitializer)sp.GetRequiredService<ICurrentUser>());

        // ===== Identity Application Services =====
        services.AddTransient<IdentityLinkBuilder>();
        services.AddTransient<IUserService, UserService>();
        services.AddTransient<IRoleService, RoleService>();
        services.AddTransient<IAuditService, AuditService>();

        // ===== ASP.NET Core Identity Configuration =====
        services.AddIdentity<User, Role>(options =>
        {
            // Password requirements
            options.Password.RequiredLength = IdentityConstants.PasswordLength;
            options.Password.RequireDigit = false;
            options.Password.RequireLowercase = false;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireUppercase = false;

            // User settings
            options.User.RequireUniqueEmail = true;

            // Sign-in settings (Important for external logins)
            options.SignIn.RequireConfirmedAccount = false;
            options.SignIn.RequireConfirmedEmail = false;
            // NOTE: For production, consider RequireConfirmedEmail = true

            // Lockout settings (security best practice)
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.AllowedForNewUsers = true;

            // Token providers (email confirmation, password reset)
            options.Tokens.PasswordResetTokenProvider = "PasswordReset";
            options.Tokens.EmailConfirmationTokenProvider = "EmailConfirmation";
        })
        .AddEntityFrameworkStores<AvanciraDbContext>()
        .AddDefaultTokenProviders()
        .AddTokenProvider<DataProtectorTokenProvider<User>>("EmailConfirmation")
        .AddTokenProvider<DataProtectorTokenProvider<User>>("PasswordReset");

        // ===== Token Lifespans =====
        services.Configure<DataProtectionTokenProviderOptions>(o =>
        {
            o.TokenLifespan = TimeSpan.FromHours(2); // General tokens
        });

        services.Configure<DataProtectionTokenProviderOptions>("EmailConfirmation", o =>
        {
            o.TokenLifespan = TimeSpan.FromDays(1);
        });

        services.Configure<DataProtectionTokenProviderOptions>("PasswordReset", o =>
        {
            o.TokenLifespan = TimeSpan.FromHours(24);
        });

        return services;
    }
}
