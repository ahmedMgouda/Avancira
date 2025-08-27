using Avancira.Application;
using Avancira.Application.Jobs;
using Avancira.Application.Origin;
using Avancira.Infrastructure.Auth;
using Avancira.Infrastructure.Auth.DelegatingHandlers;
using Avancira.Infrastructure.Caching;
using Avancira.Infrastructure.Catalog;
using Avancira.Infrastructure.Cors;
using Avancira.Infrastructure.Exceptions;
using Avancira.Infrastructure.Identity;
using Avancira.Infrastructure.Jobs;
using Avancira.Infrastructure.Logging.Serilog;
using Avancira.Infrastructure.Mail;
using Avancira.Infrastructure.OpenApi;
using Avancira.Infrastructure.Persistence;
using Avancira.Infrastructure.Persistence.Repositories;
using Avancira.Infrastructure.RateLimit;
using Avancira.Infrastructure.SecurityHeaders;
using Avancira.Infrastructure.Storage;
using Avancira.Infrastructure.Storage.Files;
using Avancira.Infrastructure.Messaging;
using Avancira.ServiceDefaults;
using FluentValidation;
using Mapster;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;

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
        builder.Services.ConfigureCatalog();
        builder.Services.AddCorsPolicy(builder.Configuration, builder.Environment);
        builder.Services.ConfigureFileStorage();
        builder.Services.ConfigureOpenApi();
        builder.Services.ConfigureJobs(builder.Configuration);
        builder.Services.ConfigureMailing();
        builder.Services.ConfigureMessaging();
        builder.Services.ConfigureCaching(builder.Configuration);
        builder.Services.AddExceptionHandler<CustomExceptionHandler>();
        builder.Services.AddProblemDetails();
        builder.Services.AddHealthChecks();
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddHttpClient("TokenClient", (sp, client) =>
        {
            var configuration = sp.GetRequiredService<IConfiguration>();
            var issuer = configuration["Auth:Issuer"];
            if (!string.IsNullOrWhiteSpace(issuer))
            {
                client.BaseAddress = new Uri(issuer);
            }
        })
        .AddHttpMessageHandler<TokenErrorHandler>();
        builder.Services.AddScoped<IRefreshTokenCookieService, RefreshTokenCookieService>();
        builder.Services.AddOptions<OriginOptions>().BindConfiguration(nameof(OriginOptions));


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
        builder.Services.Configure<TokenHashingOptions>(builder.Configuration.GetSection("Avancira:Auth:TokenHashing"));


        // Configure Mappings
        TypeAdapterConfig.GlobalSettings.Scan(typeof(IListingService).Assembly);

        // Define module assemblies
        var assemblies = AppDomain.CurrentDomain
            .GetAssemblies()
            .Where(assembly => assembly.FullName!.StartsWith("Avancira."))
            .ToArray();

        // Register validators
        builder.Services.AddValidatorsFromAssemblies(assemblies);

        // Register MediatR
        builder.Services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblies(assemblies);
        });

        builder.Services.ConfigureRateLimit(builder.Configuration);
        builder.Services.ConfigureSecurityHeaders(builder.Configuration);

        // Register repositories
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
        app.UseCorsPolicy();
        app.UseOpenApi();
        app.UseJobDashboard(app.Configuration);
        app.UseRouting();
        app.UseStaticFiles();
        app.UseStaticFiles(new StaticFileOptions()
        {
            FileProvider = new PhysicalFileProvider(Path.Combine(Directory.GetCurrentDirectory(), "assets")),
            RequestPath = new PathString("/api/assets")
        });
        app.UseStaticFilesUploads();
        app.UseAuthentication();
        app.UseAuthorization();

        // Current user middleware
        app.UseMiddleware<CurrentUserMiddleware>();

        return app;
    }

    private static IServiceCollection ConfigureMessaging(this IServiceCollection services)
    {
        // Add SignalR
        services.AddSignalR();
        
        // Register notification channels
        services.AddScoped<Avancira.Application.Messaging.INotificationChannel, EmailNotificationChannel>();
        services.AddScoped<Avancira.Application.Messaging.INotificationChannel, SignalRNotificationChannel>();
        
        return services;
    }
}
