using Avancira.Application.Audit;
using Avancira.Application.Identity.Roles;
using Avancira.Application.Identity.Tokens;
using Avancira.Application.Identity.Users.Abstractions;
using Avancira.Application.Persistence;
using Avancira.Infrastructure.Auth;
using Avancira.Infrastructure.Identity.Audit;
using Avancira.Infrastructure.Identity.Roles;
using Avancira.Infrastructure.Identity.Tokens;
using Avancira.Infrastructure.Identity.Users;
using Avancira.Infrastructure.Identity.Users.Services;
using Avancira.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using IdentityConstants = Avancira.Shared.Authorization.IdentityConstants;

namespace Avancira.Infrastructure.Identity;
internal static class Extensions
{
    internal static IServiceCollection ConfigureIdentity(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<CurrentUserMiddleware>();
        services.AddScoped<ICurrentUser, CurrentUser>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddTransient<TokenCleanupService>();
        services.AddScoped(sp => (ICurrentUserInitializer)sp.GetRequiredService<ICurrentUser>());
        services.AddTransient<Avancira.Application.Identity.Users.Abstractions.IUserService, UserService>();
        services.AddTransient<IRoleService, RoleService>();
        services.AddTransient<IAuditService, AuditService>();
        services.AddScoped<IDbInitializer, IdentityDbInitializer>();

        services.AddIdentity<User, Role>(options =>
        {
            options.Password.RequiredLength = IdentityConstants.PasswordLength;
            options.Password.RequireDigit = false;
            options.Password.RequireLowercase = false;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireUppercase = false;
            options.User.RequireUniqueEmail = true;
            options.Tokens.PasswordResetTokenProvider = "PasswordReset";
            options.Tokens.EmailConfirmationTokenProvider = "EmailConfirmation";
        })
           .AddEntityFrameworkStores<AvanciraDbContext>()
           .AddDefaultTokenProviders()
            .AddTokenProvider<DataProtectorTokenProvider<User>>("EmailConfirmation")
            .AddTokenProvider<DataProtectorTokenProvider<User>>("PasswordReset");


        // Token lifetimes
        services.Configure<DataProtectionTokenProviderOptions>(o =>
        {
            o.TokenLifespan = TimeSpan.FromHours(2);
        });

        services.Configure<DataProtectionTokenProviderOptions>("EmailConfirmation", o =>
        {
            o.TokenLifespan = TimeSpan.FromDays(1);
        });

        services.Configure<DataProtectionTokenProviderOptions>("PasswordReset", o =>
        {
            o.TokenLifespan = TimeSpan.FromHours(2);
        });

        return services;
    }
}