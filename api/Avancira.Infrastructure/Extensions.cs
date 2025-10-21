using Avancira.Application;
using Avancira.Application.Audit;
using Avancira.Application.Identity.Roles;
using Avancira.Application.Identity.Users.Abstractions;
using Avancira.Infrastructure.Auth;
using Avancira.Infrastructure.Caching;
using Avancira.Infrastructure.Catalog;
using Avancira.Infrastructure.Exceptions;
using Avancira.Infrastructure.Identity;
using Avancira.Infrastructure.Identity.Audit;
using Avancira.Infrastructure.Identity.Roles;
using Avancira.Infrastructure.Identity.Users.Services;
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
using Avancira.ServiceDefaults;
using FluentValidation;
using Mapster;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;

namespace Avancira.Infrastructure.Composition;

/// <summary>
/// Registers all Avancira infrastructure modules and feature services.
/// Dynamically adapts per project (Auth / API / BFF).
/// </summary>
public static class Extensions
{
    public static WebApplicationBuilder AddAvanciraInfrastructure(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var services = builder.Services;
        var config = builder.Configuration;
        var env = builder.Environment;

        var appName = env.ApplicationName ?? string.Empty;
        var isAuthProject = appName.Contains("Auth", StringComparison.OrdinalIgnoreCase);
        var isApiProject = appName.Contains("API", StringComparison.OrdinalIgnoreCase);
        var isBffProject = appName.Contains("BFF", StringComparison.OrdinalIgnoreCase);

        // ════════════════════════════════════════════════════════════
        // 1️⃣ Core Platform Defaults
        // ════════════════════════════════════════════════════════════
        builder.AddServiceDefaults();
        builder.ConfigureSerilog();
        builder.ConfigureDatabase();

        // 2️⃣ Identity & Database Seeding (Auth Server only)
        // ════════════════════════════════════════════════════════════
        services.ConfigureIdentity(config, env);
        services.AddDatabaseSeeders();
        services.ConfigureJobs(config);

        // ════════════════════════════════════════════════════════════
        // 3️⃣ Common Feature Modules (shared across projects)
        // ════════════════════════════════════════════════════════════
        services.ConfigureCatalog();
        services.ConfigureMailing();
        services.ConfigureMessaging();
        services.ConfigureCaching(config);
        services.ConfigureRateLimit(config);
        services.ConfigureSecurityHeaders(config);
        services.ConfigureFileStorage();
        services.AddCorsPolicy(config, env);
        services.ConfigureOpenApi();

        // ════════════════════════════════════════════════════════════
        // 4️⃣ Error Handling, Health, and HTTP Infrastructure
        // ════════════════════════════════════════════════════════════
        services.AddExceptionHandler<CustomExceptionHandler>();
        services.AddProblemDetails();
        services.AddHealthChecks();
        services.AddHttpContextAccessor();

        // ════════════════════════════════════════════════════════════
        // 5️⃣ Mapping, Validation, MediatR, and Domain Registrations
        // ════════════════════════════════════════════════════════════
        TypeAdapterConfig.GlobalSettings.Scan(typeof(IListingService).Assembly);

        var assemblies = AppDomain.CurrentDomain
            .GetAssemblies()
            .Where(a => a.FullName!.StartsWith("Avancira.", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        services.AddValidatorsFromAssemblies(assemblies);
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(assemblies));
        services.AddRepositories();
        services.AddApplicationServices();

        // ════════════════════════════════════════════════════════════
        // 6️⃣ Options & External Configuration Binding
        // ════════════════════════════════════════════════════════════
        BindAppOptions(services, config);

        return builder;
    }

    // ───────────────────────────────────────────────────────────────
    //  Internal helpers
    // ───────────────────────────────────────────────────────────────
    private static void BindAppOptions(IServiceCollection services, IConfiguration config)
    {
        services.Configure<AppOptions>(config.GetSection("Avancira:App"));
        services.Configure<StripeOptions>(config.GetSection("Avancira:Payments:Stripe"));
        services.Configure<PayPalOptions>(config.GetSection("Avancira:Payments:PayPal"));
        services.Configure<EmailOptions>(config.GetSection("Avancira:Notifications:Email"));
        services.Configure<GraphApiOptions>(config.GetSection("Avancira:Notifications:GraphApi"));
        services.Configure<SmtpOpctions>(config.GetSection("Avancira:Notifications:Smtp"));
        services.Configure<SendGridOptions>(config.GetSection("Avancira:Notifications:SendGrid"));
        services.Configure<TwilioOptions>(config.GetSection("Avancira:Notifications:Twilio"));
        services.Configure<JitsiOptions>(config.GetSection("Avancira:Jitsi"));
        services.Configure<GoogleOptions>(config.GetSection("Avancira:ExternalServices:Google"));
        services.Configure<FacebookOptions>(config.GetSection("Avancira:ExternalServices:Facebook"));
    }

    // ───────────────────────────────────────────────────────────────
    //  Optional HTTP Pipeline Composition (shared baseline)
    // ───────────────────────────────────────────────────────────────
    //public static WebApplication UseAvanciraFramework(this WebApplication app)
    //{
    //    // Default endpoints (health, metrics)
    //    app.MapDefaultEndpoints();

    //    // Security and middleware
    //    app.UseRateLimit();
    //    app.UseSecurityHeaders();
    //    app.UseExceptionHandler();

    //    app.UseRouting();
    //    app.UseCorsPolicy();
    //    app.UseOpenApi();
    //    app.UseJobDashboard(app.Configuration);

    //    // Static file support
    //    app.UseStaticFiles();

    //    var assetsPath = Path.Combine(app.Environment.ContentRootPath, "assets");
    //    if (Directory.Exists(assetsPath))
    //    {
    //        app.UseStaticFiles(new StaticFileOptions
    //        {
    //            FileProvider = new PhysicalFileProvider(assetsPath),
    //            RequestPath = new PathString("/api/assets")
    //        });
    //    }
    //    else
    //    {
    //        app.Logger.LogWarning("Static assets directory '{AssetsPath}' not found.", assetsPath);
    //    }

    //    // File uploads
    //    app.UseStaticFilesUploads();

    //    // Security middlewares
    //    app.UseAuthentication();
    //    app.UseAuthorization();

    //    // Current user middleware (audit context)
    //    app.UseMiddleware<CurrentUserMiddleware>();

    //    return app;
    //}
}
