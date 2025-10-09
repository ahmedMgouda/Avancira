using System;
using Avancira.Application;
using Avancira.Application.UserSessions;
using Avancira.Application.UserSessions.Services;
using Avancira.Infrastructure.Caching;
using Avancira.Infrastructure.Catalog;
using Avancira.Infrastructure.Exceptions;
using Avancira.Infrastructure.Identity;
using Avancira.Infrastructure.Jobs;
using Avancira.Infrastructure.Logging.Serilog;
using Avancira.Infrastructure.Mail;
using Avancira.Infrastructure.Messaging;
using Avancira.Infrastructure.OpenApi;
using Avancira.Infrastructure.Persistence;
using Avancira.Infrastructure.Persistence.Repositories;
using Avancira.Infrastructure.RateLimit;
using Avancira.Infrastructure.SecurityHeaders;
using Avancira.Infrastructure.Storage;
using Avancira.Infrastructure.Storage.Files;
using Avancira.Infrastructure.UserSessions;
using Avancira.ServiceDefaults;
using FluentValidation;
using Mapster;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using OpenIddict.Validation.AspNetCore;

namespace Avancira.Infrastructure;

public static class Extensions
{
    public static WebApplicationBuilder ConfigureAvanciraFramework(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        // Base
        builder.AddServiceDefaults();
        builder.ConfigureSerilog();
        builder.ConfigureDatabase();

        // Identity & OpenIddict (server + validation should be inside AddInfrastructureIdentity)
        builder.Services.ConfigureIdentity();                       // registers Identity (users/roles/cookies)
        builder.Services.AddInfrastructureIdentity(builder.Configuration); // registers OpenIddict server+validation

        // Authentication: default = OpenIddict (for APIs). Identity cookies for web UI.
        builder.Services.AddAuthentication(options =>
        {
            options.DefaultScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
            options.DefaultAuthenticateScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
            options.DefaultForbidScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
            options.DefaultSignInScheme = IdentityConstants.ApplicationScheme;
            options.ForwardDefaultSelector = context =>
            {
                var path = context.Request.Path;

                if (!path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase))
                {
                    return IdentityConstants.ApplicationScheme;
                }

                return null;
            };
        })
        .AddIdentityCookies(options =>
        {
            // Application (login) cookie
            options.ApplicationCookie?.Configure(o =>
            {
                o.Cookie.Name = ".Avancira.Identity";
                o.LoginPath = "/account/login";
                o.LogoutPath = "/account/logout";
                o.Cookie.HttpOnly = true;
                o.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                o.Cookie.SameSite = SameSiteMode.Lax;
                o.SlidingExpiration = true;
                o.ExpireTimeSpan = TimeSpan.FromHours(2);
            });

            // External (Google/Facebook) temp cookie
            options.ExternalCookie?.Configure(o =>
            {
                o.Cookie.Name = ".Avancira.Identity.External";
                o.ExpireTimeSpan = TimeSpan.FromMinutes(15);
                o.Cookie.HttpOnly = true;
                o.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                o.Cookie.SameSite = SameSiteMode.Lax;
            });
        });

        builder.Services.AddAuthorization();

        // Feature services
        builder.Services.ConfigureCatalog();
        builder.Services.AddCorsPolicy(builder.Configuration, builder.Environment);
        builder.Services.ConfigureFileStorage();
        builder.Services.ConfigureOpenApi();
        builder.Services.ConfigureJobs(builder.Configuration);
        builder.Services.ConfigureMailing();
        builder.Services.ConfigureMessaging();
        builder.Services.ConfigureCaching(builder.Configuration);

        // Error & health
        builder.Services.AddExceptionHandler<CustomExceptionHandler>();
        builder.Services.AddProblemDetails();
        builder.Services.AddHealthChecks();
        builder.Services.AddHttpContextAccessor();

        // Sessions & security
        builder.Services.ConfigureUserSessions();
        builder.Services.ConfigureRateLimit(builder.Configuration);
        builder.Services.ConfigureSecurityHeaders(builder.Configuration);

        // Options
        builder.Services.Configure<OpenIddictServerSettings>(builder.Configuration.GetSection("Auth:OpenIddict"));
        builder.Services.Configure<AppOptions>(builder.Configuration.GetSection("Avancira:App"));
        builder.Services.Configure<StripeOptions>(builder.Configuration.GetSection("Avancira:Payments:Stripe"));
        builder.Services.Configure<PayPalOptions>(builder.Configuration.GetSection("Avancira:Payments:PayPal"));
        builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection("Avancira:Notifications:Email"));
        builder.Services.Configure<GraphApiOptions>(builder.Configuration.GetSection("Avancira:Notifications:GraphApi"));
        builder.Services.Configure<SmtpOpctions>(builder.Configuration.GetSection("Avancira:Notifications:Smtp"));
        builder.Services.Configure<SendGridOptions>(builder.Configuration.GetSection("Avancira:Notifications:SendGrid"));
        builder.Services.Configure<TwilioOptions>(builder.Configuration.GetSection("Avancira:Notifications:Twilio"));
        builder.Services.Configure<JitsiOptions>(builder.Configuration.GetSection("Avancira:Jitsi"));
        builder.Services.Configure<GoogleOptions>(builder.Configuration.GetSection("Avancira:ExternalServices:Google"));
        builder.Services.Configure<FacebookOptions>(builder.Configuration.GetSection("Avancira:ExternalServices:Facebook"));

        // Mapping, validation, MediatR
        TypeAdapterConfig.GlobalSettings.Scan(typeof(IListingService).Assembly);

        var assemblies = AppDomain.CurrentDomain
            .GetAssemblies()
            .Where(a => a.FullName!.StartsWith("Avancira."))
            .ToArray();

        builder.Services.AddValidatorsFromAssemblies(assemblies);
        builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(assemblies));

        // Repos + app services
        builder.Services.AddRepositories();
        builder.Services.AddApplicationServices();

        return builder;
    }

    public static WebApplication UseAvanciraFramework(this WebApplication app)
    {
        app.MapDefaultEndpoints();
        app.SetupDatabases();
        app.UseRateLimit();
        app.UseSecurityHeaders();
        app.UseExceptionHandler();

        // Routing first
        app.UseRouting();

        // CORS after routing, before auth
        app.UseCorsPolicy();

        // OpenAPI / Jobs
        app.UseOpenApi();
        app.UseJobDashboard(app.Configuration);

        // Static files
        app.UseStaticFiles();
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(Path.Combine(Directory.GetCurrentDirectory(), "assets")),
            RequestPath = new PathString("/api/assets")
        });
        app.UseStaticFilesUploads();

        // AuthN/Z
        app.UseAuthentication();
        app.UseAuthorization();

        // Current user context
        app.UseMiddleware<CurrentUserMiddleware>();

        return app;
    }

    private static IServiceCollection ConfigureUserSessions(this IServiceCollection services)
    {
        services.AddScoped<ISessionMetadataCollectionService, SessionMetadataCollectionService>();
        services.AddScoped<INetworkContextService, NetworkContextService>();
        services.AddSingleton<IUserAgentAnalysisService, UserAgentAnalysisService>();
        services.AddHttpClient<IGeolocationService, GeolocationService>();
        return services;
    }

    private static IServiceCollection ConfigureMessaging(this IServiceCollection services)
    {
        services.AddSignalR();
        services.AddScoped<Avancira.Application.Messaging.INotificationChannel, EmailNotificationChannel>();
        services.AddScoped<Avancira.Application.Messaging.INotificationChannel, SignalRNotificationChannel>();
        return services;
    }
}
