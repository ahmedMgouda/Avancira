using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Facebook;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace Avancira.Auth.Extensions;

public static class AuthenticationExtensions
{
    private const string ApplicationCookieName = ".Avancira.Identity";
    private const string ExternalCookieName = ".Avancira.Identity.External";

    private static readonly TimeSpan ApplicationCookieLifetime = TimeSpan.FromHours(2);
    private static readonly TimeSpan ExternalCookieLifetime = TimeSpan.FromMinutes(15);

    /// <summary>
    /// Configures internal cookies for Identity (application + external sign-ins).
    /// </summary>
    public static IServiceCollection AddAuthServerAuthentication(
        this IServiceCollection services,
        IWebHostEnvironment? environment = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        bool isDevelopment = environment?.IsDevelopment() == true;
        var securePolicy = isDevelopment ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
        var externalSameSite = isDevelopment ? SameSiteMode.Lax : SameSiteMode.None;

        // Application (identity) cookie
        services.ConfigureApplicationCookie(options =>
        {
            options.Cookie.Name = ApplicationCookieName;
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = securePolicy;
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.IsEssential = true;

            options.LoginPath = "/account/login";
            options.LogoutPath = "/account/logout";
            options.AccessDeniedPath = "/account/access-denied";

            options.SlidingExpiration = true;
            options.ExpireTimeSpan = ApplicationCookieLifetime;
        });

        // External (temporary) cookie
        services.ConfigureExternalCookie(options =>
        {
            options.Cookie.Name = ExternalCookieName;
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = securePolicy;
            options.Cookie.SameSite = externalSameSite;
            options.Cookie.IsEssential = true;
            options.ExpireTimeSpan = ExternalCookieLifetime;
        });

        return services;
    }

    /// <summary>
    /// Configures external authentication providers (Google, Facebook).
    /// </summary>
    public static AuthenticationBuilder AddExternalAuthentication(
        this IServiceCollection services,
        IConfiguration configuration,
        ILogger logger,
        IWebHostEnvironment? environment = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        bool isDevelopment = environment?.IsDevelopment() == true;
        var correlationSecurePolicy = isDevelopment ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
        var correlationSameSite = isDevelopment ? SameSiteMode.Lax : SameSiteMode.None;

        var builder = services.AddAuthentication(options =>
        {
            options.DefaultScheme = IdentityConstants.ApplicationScheme;
            options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
        });

        AddGoogleAuthentication(builder, configuration, logger, correlationSecurePolicy, correlationSameSite);
        AddFacebookAuthentication(builder, configuration, logger, correlationSecurePolicy, correlationSameSite);

        return builder;
    }

    private static void AddGoogleAuthentication(
        AuthenticationBuilder builder,
        IConfiguration config,
        ILogger logger,
        CookieSecurePolicy securePolicy,
        SameSiteMode sameSite)
    {
        var section = config.GetSection("Avancira:ExternalServices:Google");
        var clientId = section["ClientId"];
        var clientSecret = section["ClientSecret"];

        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
        {
            logger.LogWarning("Google OAuth credentials not configured");
            return;
        }

        builder.AddGoogle(GoogleDefaults.AuthenticationScheme, options =>
        {
            options.ClientId = clientId!;
            options.ClientSecret = clientSecret!;
            options.SignInScheme = IdentityConstants.ExternalScheme;

            // Correlation cookie setup
            options.CorrelationCookie.SecurePolicy = securePolicy;
            options.CorrelationCookie.SameSite = sameSite;
            options.CorrelationCookie.IsEssential = true;

            // Scopes and claims
            options.Scope.Clear();
            options.Scope.Add("profile");
            options.Scope.Add("email");

            options.ClaimActions.MapJsonKey(ClaimTypes.NameIdentifier, "sub");
            options.ClaimActions.MapJsonKey(ClaimTypes.Name, "name");
            options.ClaimActions.MapJsonKey(ClaimTypes.GivenName, "given_name");
            options.ClaimActions.MapJsonKey(ClaimTypes.Surname, "family_name");
            options.ClaimActions.MapJsonKey(ClaimTypes.Email, "email");

            // Events
            options.Events.OnRemoteFailure = ctx =>
            {
                logger.LogError(ctx.Failure, "Google authentication failed");
                var message = ctx.Failure?.Message ?? "google_failed";
                ctx.Response.Redirect($"/account/login?error={Uri.EscapeDataString(message)}");
                ctx.HandleResponse();
                return Task.CompletedTask;
            };

            options.Events.OnCreatingTicket = ctx =>
            {
                var email = ctx.Principal?.FindFirstValue(ClaimTypes.Email);
                logger.LogInformation("Google authentication succeeded for {Email}", email ?? "unknown");
                return Task.CompletedTask;
            };
        });

        logger.LogInformation("Google authentication configured successfully");
    }

    private static void AddFacebookAuthentication(
        AuthenticationBuilder builder,
        IConfiguration config,
        ILogger logger,
        CookieSecurePolicy securePolicy,
        SameSiteMode sameSite)
    {
        var section = config.GetSection("Avancira:ExternalServices:Facebook");
        var appId = section["AppId"];
        var appSecret = section["AppSecret"];

        if (string.IsNullOrWhiteSpace(appId) || string.IsNullOrWhiteSpace(appSecret))
        {
            logger.LogWarning("⚠️ Facebook OAuth credentials not configured");
            return;
        }

        builder.AddFacebook(FacebookDefaults.AuthenticationScheme, options =>
        {
            options.AppId = appId!;
            options.AppSecret = appSecret!;
            options.SignInScheme = IdentityConstants.ExternalScheme;

            options.CorrelationCookie.SecurePolicy = securePolicy;
            options.CorrelationCookie.SameSite = sameSite;
            options.CorrelationCookie.IsEssential = true;

            // Permissions
            options.Scope.Clear();
            options.Scope.Add("email");
            options.Scope.Add("public_profile");

            // Requested fields
            options.Fields.Clear();
            options.Fields.Add("email");
            options.Fields.Add("first_name");
            options.Fields.Add("last_name");
            options.Fields.Add("name");
            options.Fields.Add("picture");

            options.ClaimActions.MapJsonKey(ClaimTypes.NameIdentifier, "id");
            options.ClaimActions.MapJsonKey(ClaimTypes.Email, "email");
            options.ClaimActions.MapJsonKey(ClaimTypes.Name, "name");
            options.ClaimActions.MapJsonKey(ClaimTypes.GivenName, "first_name");
            options.ClaimActions.MapJsonKey(ClaimTypes.Surname, "last_name");

            options.Events.OnRemoteFailure = ctx =>
            {
                logger.LogError(ctx.Failure, "Facebook authentication failed");
                var message = ctx.Failure?.Message ?? "facebook_failed";
                ctx.Response.Redirect($"/account/login?error={Uri.EscapeDataString(message)}");
                ctx.HandleResponse();
                return Task.CompletedTask;
            };

            options.Events.OnCreatingTicket = ctx =>
            {
                var email = ctx.Principal?.FindFirstValue(ClaimTypes.Email);
                logger.LogInformation("Facebook authentication succeeded for {Email}", email ?? "unknown");
                return Task.CompletedTask;
            };
        });

        logger.LogInformation("Facebook authentication configured successfully");
    }
}