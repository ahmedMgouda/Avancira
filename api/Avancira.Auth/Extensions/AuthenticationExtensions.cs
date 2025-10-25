using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Facebook;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace Avancira.Auth.Extensions;

/// <summary>
/// Authentication configuration for the Auth MVC project
/// Handles both Identity cookies and external providers (Google, Facebook)
/// </summary>
public static class AuthenticationExtensions
{
    private const string ApplicationCookieName = ".Avancira.Identity";
    private const string ExternalCookieName = ".Avancira.Identity.External";

    private static readonly TimeSpan ApplicationCookieLifetime = TimeSpan.FromHours(2);
    private static readonly TimeSpan ExternalCookieLifetime = TimeSpan.FromMinutes(15);

    /// <summary>
    /// Configures Identity cookie authentication for the Auth server
    /// </summary>
    public static IServiceCollection AddAuthServerAuthentication(
        this IServiceCollection services,
        IWebHostEnvironment? environment = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddAuthorization();

        // Determine secure policy based on environment
        var securePolicy = environment?.EnvironmentName == "Development" 
            ? CookieSecurePolicy.SameAsRequest 
            : CookieSecurePolicy.Always;

        // Configure the main application cookie (used after login)
        services.ConfigureApplicationCookie(options =>
        {
            options.Cookie.Name = ApplicationCookieName;
            options.LoginPath = "/account/login";
            options.LogoutPath = "/account/logout";
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = securePolicy;
            options.Cookie.SameSite = SameSiteMode.Lax;

            options.SlidingExpiration = true;
            options.ExpireTimeSpan = ApplicationCookieLifetime;
            options.AccessDeniedPath = "/account/access-denied";
        });

        // Configure external authentication cookie (temporary during external login)
        services.ConfigureExternalCookie(options =>
        {
            options.Cookie.Name = ExternalCookieName;
            options.ExpireTimeSpan = ExternalCookieLifetime;
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = securePolicy;
            options.Cookie.SameSite = SameSiteMode.Lax;
        });

        return services;
    }

    /// <summary>
    /// Configures external authentication providers (Google, Facebook)
    /// </summary>
    public static AuthenticationBuilder AddExternalAuthentication(
        this IServiceCollection services,
        IConfiguration configuration,
        ILogger logger,
        IWebHostEnvironment? environment = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var builder = services.AddAuthentication();

        // Determine secure policy based on environment
        var securePolicy = environment?.EnvironmentName == "Development" 
            ? CookieSecurePolicy.SameAsRequest 
            : CookieSecurePolicy.Always;

        // ---- Google Configuration ----
        var googleSection = configuration.GetSection("Avancira:ExternalServices:Google");
        var google = googleSection.Get<GoogleOptions>();

        if (!string.IsNullOrWhiteSpace(google?.ClientId) &&
            !string.IsNullOrWhiteSpace(google.ClientSecret))
        {
            builder.AddGoogle(GoogleDefaults.AuthenticationScheme, o =>
            {
                // Bind all configuration from appsettings
                googleSection.Bind(o);

                // FIX 1: Ensure callback path is exactly as configured in Google Console
                o.CallbackPath = "/account/external-callback";
                
                // FIX 2: Configure correlation cookie with environment-aware settings
                o.CorrelationCookie.Name = ".Avancira.Correlation.Google";
                o.CorrelationCookie.Path = "/";
                o.CorrelationCookie.SameSite = SameSiteMode.None; // Changed to None for OAuth
                o.CorrelationCookie.HttpOnly = true;
                o.CorrelationCookie.SecurePolicy = CookieSecurePolicy.Always;
                o.CorrelationCookie.IsEssential = true;

                o.SignInScheme = IdentityConstants.ExternalScheme;
                o.SaveTokens = true;

                // Ensure required scopes are present
                if (!o.Scope.Contains("email"))
                {
                    o.Scope.Add("email");
                }

                if (!o.Scope.Contains("profile"))
                {
                    o.Scope.Add("profile");
                }

                // Add claim mapping for better compatibility
                o.ClaimActions.MapJsonKey(ClaimTypes.Email, "email");
                o.ClaimActions.MapJsonKey(ClaimTypes.Name, "name");
                o.ClaimActions.MapJsonKey(ClaimTypes.GivenName, "given_name");
                o.ClaimActions.MapJsonKey(ClaimTypes.Surname, "family_name");

                // FIX 3: Add event handlers for better debugging
                o.Events.OnRemoteFailure = context =>
                {
                    var errorMessage = context.Failure?.Message ?? "Unknown error";
                    var errorType = context.Failure?.GetType().Name ?? "Unknown";
                    
                    logger.LogError(context.Failure, 
                        "Google authentication failed. Error Type: {ErrorType}, Message: {Message}, " +
                        "Request Path: {Path}, Query: {Query}", 
                        errorType, 
                        errorMessage,
                        context.Request.Path,
                        context.Request.QueryString);
                    
                    // Log inner exception if exists
                    if (context.Failure?.InnerException != null)
                    {
                        logger.LogError(context.Failure.InnerException, 
                            "Inner exception: {InnerMessage}", 
                            context.Failure.InnerException.Message);
                    }
                    
                    context.Response.Redirect($"/account/login?error=google_failed&detail={Uri.EscapeDataString(errorMessage)}");
                    context.HandleResponse();
                    return Task.CompletedTask;
                };

                o.Events.OnTicketReceived = context =>
                {
                    logger.LogInformation("Google authentication ticket received for {Email}", 
                        context.Principal?.FindFirst(ClaimTypes.Email)?.Value ?? "unknown");
                    return Task.CompletedTask;
                };
                
                o.Events.OnCreatingTicket = context =>
                {
                    logger.LogInformation("Creating ticket for Google user. AccessToken exists: {HasToken}", 
                        !string.IsNullOrEmpty(context.AccessToken));
                    return Task.CompletedTask;
                };
            });

            logger.LogInformation("Google authentication configured successfully");
        }
        else
        {
            logger.LogWarning(
                "Google OAuth configuration is missing or incomplete. " +
                "Google authentication will not be available.");
        }

        // ---- Facebook Configuration ----
        var facebookSection = configuration.GetSection("Avancira:ExternalServices:Facebook");
        var facebook = facebookSection.Get<FacebookOptions>();

        if (!string.IsNullOrWhiteSpace(facebook?.AppId) &&
            !string.IsNullOrWhiteSpace(facebook.AppSecret))
        {
            builder.AddFacebook(FacebookDefaults.AuthenticationScheme, o =>
            {
                facebookSection.Bind(o);

                o.CallbackPath = "/account/external-callback";
                
                // Configure correlation cookie
                o.CorrelationCookie.Name = ".Avancira.Correlation.Facebook";
                o.CorrelationCookie.Path = "/";
                o.CorrelationCookie.SameSite = SameSiteMode.Lax;
                o.CorrelationCookie.HttpOnly = true;
                o.CorrelationCookie.SecurePolicy = securePolicy;
                o.CorrelationCookie.IsEssential = true;

                o.SignInScheme = IdentityConstants.ExternalScheme;
                o.SaveTokens = true;

                // Facebook requires explicit email scope and field
                if (!o.Scope.Contains("email"))
                {
                    o.Scope.Add("email");
                }

                // Request specific fields from Facebook Graph API
                o.Fields.Add("email");
                o.Fields.Add("name");
                o.Fields.Add("first_name");
                o.Fields.Add("last_name");

                // Map Facebook claims to standard claim types
                o.ClaimActions.MapJsonKey(ClaimTypes.Email, "email");
                o.ClaimActions.MapJsonKey(ClaimTypes.Name, "name");
                o.ClaimActions.MapJsonKey(ClaimTypes.GivenName, "first_name");
                o.ClaimActions.MapJsonKey(ClaimTypes.Surname, "last_name");

                // Add event handlers for debugging
                o.Events.OnRemoteFailure = context =>
                {
                    logger.LogError(context.Failure, "Facebook authentication failed");
                    context.Response.Redirect("/account/login?error=facebook_failed");
                    context.HandleResponse();
                    return Task.CompletedTask;
                };

                o.Events.OnTicketReceived = context =>
                {
                    logger.LogInformation("Facebook authentication ticket received for {Email}", 
                        context.Principal?.FindFirst(ClaimTypes.Email)?.Value ?? "unknown");
                    return Task.CompletedTask;
                };
            });

            logger.LogInformation("Facebook authentication configured successfully");
        }
        else
        {
            logger.LogWarning(
                "Facebook OAuth configuration is missing or incomplete. " +
                "Facebook authentication will not be available.");
        }

        return builder;
    }
}