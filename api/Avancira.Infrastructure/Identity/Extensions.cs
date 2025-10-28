using Avancira.Application.Audit;
using Avancira.Application.Identity.Roles;
using Avancira.Application.Identity.Users.Abstractions;
using Avancira.Infrastructure.Auth;
using Avancira.Infrastructure.Identity.Audit;
using Avancira.Infrastructure.Identity.Roles;
using Avancira.Infrastructure.Identity.Users;
using Avancira.Infrastructure.Identity.Users.Services;
using Avancira.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using IdentityDefaults = Avancira.Shared.Authorization.IdentityDefaults;

namespace Avancira.Infrastructure.Identity;

public static class Extensions
{
    /// <summary>
    /// Registers ASP.NET Core Identity with environment-aware configuration.
    /// </summary>
    public static IServiceCollection ConfigureIdentity(
        this IServiceCollection services,
        IConfiguration? configuration = null,
        IWebHostEnvironment? environment = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        // 1️⃣ Current User Context
        services.AddScoped<CurrentUserMiddleware>();
        services.AddScoped<ICurrentUser, CurrentUser>();
        services.AddScoped(sp => (ICurrentUserInitializer)sp.GetRequiredService<ICurrentUser>());

        // 2️⃣ Application Identity & Audit Services
        services.AddTransient<IdentityLinkBuilder>();
        services.AddTransient<IUserService, UserService>();
        services.AddTransient<IRoleService, RoleService>();
        services.AddTransient<IAuditService, AuditService>();

        // 3️⃣ Choose Identity Setup by Environment
        bool isAuthServer = environment?.ApplicationName?.Contains("Auth", StringComparison.OrdinalIgnoreCase) ?? false;

        if (isAuthServer)
        {
            // Full Identity stack (used in Auth Server with login UI)
            services.AddIdentity<User, Role>(options =>
            {
                ConfigurePassword(options.Password);
                ConfigureUser(options.User);
                ConfigureSignIn(options.SignIn, environment);
                ConfigureLockout(options.Lockout);
                ConfigureTokens(options.Tokens);
            })
            .AddEntityFrameworkStores<AvanciraDbContext>()
            .AddDefaultTokenProviders()
            .AddTokenProvider<DataProtectorTokenProvider<User>>("EmailConfirmation")
            .AddTokenProvider<DataProtectorTokenProvider<User>>("PasswordReset");
        }
        else
        {
            // Lightweight identity (for API / BFF)
            services.AddIdentityCore<User>(options =>
            {
                ConfigurePassword(options.Password);
                ConfigureUser(options.User);
            })
            .AddRoles<Role>()
            .AddEntityFrameworkStores<AvanciraDbContext>()
            .AddDefaultTokenProviders();
        }

        // 4️⃣ Token Lifespans
        ConfigureTokenLifespans(services, configuration);

        return services;
    }

    // ───────────────────────────────
    // Helper Configurations
    // ───────────────────────────────

    private static void ConfigurePassword(PasswordOptions options)
    {
        options.RequiredLength = IdentityDefaults.PasswordLength;
        options.RequireDigit = false;
        options.RequireLowercase = false;
        options.RequireNonAlphanumeric = false;
        options.RequireUppercase = false;
    }

    private static void ConfigureUser(UserOptions options)
    {
        options.RequireUniqueEmail = true;
    }

    private static void ConfigureSignIn(SignInOptions options, IWebHostEnvironment? env)
    {
        bool isDevelopment = env?.IsDevelopment() ?? false;
        options.RequireConfirmedAccount = !isDevelopment;
        options.RequireConfirmedEmail = !isDevelopment;
    }

    private static void ConfigureLockout(LockoutOptions options)
    {
        options.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
        options.MaxFailedAccessAttempts = 5;
        options.AllowedForNewUsers = true;
    }

    private static void ConfigureTokens(TokenOptions options)
    {
        options.PasswordResetTokenProvider = "PasswordReset";
        options.EmailConfirmationTokenProvider = "EmailConfirmation";
    }

    private static void ConfigureTokenLifespans(IServiceCollection services, IConfiguration? config)
    {
        var generalLifespan = GetTokenLifespan(config, "Identity:Tokens:GeneralHours", TimeSpan.FromHours(2));
        var emailLifespan = GetTokenLifespan(config, "Identity:Tokens:EmailHours", TimeSpan.FromDays(1));
        var passwordLifespan = GetTokenLifespan(config, "Identity:Tokens:PasswordHours", TimeSpan.FromHours(24));

        services.Configure<DataProtectionTokenProviderOptions>(o =>
        {
            o.TokenLifespan = generalLifespan;
        });

        services.Configure<DataProtectionTokenProviderOptions>("EmailConfirmation", o =>
        {
            o.TokenLifespan = emailLifespan;
        });

        services.Configure<DataProtectionTokenProviderOptions>("PasswordReset", o =>
        {
            o.TokenLifespan = passwordLifespan;
        });
    }

    private static TimeSpan GetTokenLifespan(IConfiguration? config, string key, TimeSpan fallback)
    {
        if (config == null) return fallback;
        var value = config[key];
        return int.TryParse(value, out var hours) ? TimeSpan.FromHours(hours) : fallback;
    }
}
