using Avancira.Application;
using Avancira.Application.UserSessions;
using Avancira.Application.UserSessions.Services;
using Avancira.Infrastructure.Auth;
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
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;

namespace Avancira.Infrastructure;

public static class Extensions
{
    public static WebApplicationBuilder ConfigureAvanciraFramework(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddServiceDefaults();
        builder.ConfigureSerilog();
        builder.ConfigureDatabase();

        builder.Services.ConfigureIdentity();

        // ===== Feature Services =====
        builder.Services.ConfigureCatalog();
        builder.Services.AddCorsPolicy(builder.Configuration, builder.Environment);
        builder.Services.ConfigureFileStorage();
        builder.Services.ConfigureOpenApi();
        builder.Services.ConfigureJobs(builder.Configuration);
        builder.Services.ConfigureMailing();
        builder.Services.ConfigureMessaging();
        builder.Services.ConfigureCaching(builder.Configuration);

        // ===== Error Handling & Health Checks =====
        builder.Services.AddExceptionHandler<CustomExceptionHandler>();
        builder.Services.AddProblemDetails();
        builder.Services.AddHealthChecks();
        builder.Services.AddHttpContextAccessor();

        // ===== Sessions & Security =====
        builder.Services.ConfigureUserSessions();
        builder.Services.ConfigureRateLimit(builder.Configuration);
        builder.Services.ConfigureSecurityHeaders(builder.Configuration);

        // ===== Configure Options =====
        // These are used throughout the application
        builder.Services.Configure<OpenIddictServerSettings>(
            builder.Configuration.GetSection("Auth:OpenIddict"));
        builder.Services.Configure<AppOptions>(
            builder.Configuration.GetSection("Avancira:App"));
        builder.Services.Configure<StripeOptions>(
            builder.Configuration.GetSection("Avancira:Payments:Stripe"));
        builder.Services.Configure<PayPalOptions>(
            builder.Configuration.GetSection("Avancira:Payments:PayPal"));
        builder.Services.Configure<EmailOptions>(
            builder.Configuration.GetSection("Avancira:Notifications:Email"));
        builder.Services.Configure<GraphApiOptions>(
            builder.Configuration.GetSection("Avancira:Notifications:GraphApi"));
        builder.Services.Configure<SmtpOpctions>(
            builder.Configuration.GetSection("Avancira:Notifications:Smtp"));
        builder.Services.Configure<SendGridOptions>(
            builder.Configuration.GetSection("Avancira:Notifications:SendGrid"));
        builder.Services.Configure<TwilioOptions>(
            builder.Configuration.GetSection("Avancira:Notifications:Twilio"));
        builder.Services.Configure<JitsiOptions>(
            builder.Configuration.GetSection("Avancira:Jitsi"));
        builder.Services.Configure<GoogleOptions>(
            builder.Configuration.GetSection("Avancira:ExternalServices:Google"));
        builder.Services.Configure<FacebookOptions>(
            builder.Configuration.GetSection("Avancira:ExternalServices:Facebook"));

        // ===== Mapping & Validation =====
        TypeAdapterConfig.GlobalSettings.Scan(typeof(IListingService).Assembly);

        var assemblies = AppDomain.CurrentDomain
            .GetAssemblies()
            .Where(a => a.FullName!.StartsWith("Avancira."))
            .ToArray();

        builder.Services.AddValidatorsFromAssemblies(assemblies);
        builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(assemblies));

        // ===== Repositories & Application Services =====
        builder.Services.AddRepositories();
        builder.Services.AddApplicationServices();

        return builder;
    }

    public static WebApplication UseAvanciraFramework(this WebApplication app, bool runDatabasePreparation = false)
    {
        app.MapDefaultEndpoints();

        if (runDatabasePreparation)
        {
            app.SetupDatabases();
        }

        app.UseRateLimit();
        app.UseSecurityHeaders();
        app.UseExceptionHandler();

        app.UseRouting();

        app.UseCorsPolicy();

        app.UseOpenApi();
        app.UseJobDashboard(app.Configuration);

        app.UseStaticFiles();

        var assetsPath = Path.Combine(app.Environment.ContentRootPath, "assets");
        if (Directory.Exists(assetsPath))
        {
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(assetsPath),
                RequestPath = new PathString("/api/assets")
            });
        }
        else
        {
            app.Logger.LogWarning("Static assets directory '{AssetsPath}' was not found. Static assets will not be served.", assetsPath);
        }

        app.UseStaticFilesUploads();

        app.UseAuthentication();
        app.UseAuthorization();

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