using Avancira.Application.Audit;
using Avancira.Application.Identity.Roles;
using Avancira.Application.Identity.Users.Abstractions;
using Avancira.Application.Persistence;
using Avancira.Infrastructure.Auth;
using Avancira.Infrastructure.Identity.Audit;
using Avancira.Infrastructure.Identity.Roles;
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
    /// Configures ASP.NET Core Identity with custom settings
    /// CHANGE 1: This is shared between Auth and API projects
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
        services.AddScoped<IDbInitializer, IdentityDbInitializer>();

        // ===== CHANGE 2: Configure ASP.NET Core Identity =====
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

            // CHANGE 3: Sign-in settings - Important for external logins
            options.SignIn.RequireConfirmedAccount = false;
            options.SignIn.RequireConfirmedEmail = false;
            // NOTE: Setting these to false allows users to sign in immediately
            // For production, consider setting RequireConfirmedEmail = true

            // CHANGE 4: Lockout settings (security best practice)
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.AllowedForNewUsers = true;

            // Token providers (for email confirmation, password reset)
            options.Tokens.PasswordResetTokenProvider = "PasswordReset";
            options.Tokens.EmailConfirmationTokenProvider = "EmailConfirmation";
        })
        .AddEntityFrameworkStores<AvanciraDbContext>()
        .AddDefaultTokenProviders()
        .AddTokenProvider<DataProtectorTokenProvider<User>>("EmailConfirmation")
        .AddTokenProvider<DataProtectorTokenProvider<User>>("PasswordReset");

        // ===== CHANGE 5: Configure token lifespans =====
        // Default token provider (used for general purposes)
        services.Configure<DataProtectionTokenProviderOptions>(o =>
        {
            o.TokenLifespan = TimeSpan.FromHours(2);
        });

        // Email confirmation token (longer lifespan)
        services.Configure<DataProtectionTokenProviderOptions>("EmailConfirmation", o =>
        {
            o.TokenLifespan = TimeSpan.FromDays(1);
        });

        // Password reset token (24 hours)
        services.Configure<DataProtectionTokenProviderOptions>("PasswordReset", o =>
        {
            o.TokenLifespan = TimeSpan.FromHours(24);
        });

        return services;
    }
}