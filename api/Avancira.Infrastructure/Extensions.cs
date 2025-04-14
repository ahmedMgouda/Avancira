using FluentValidation;
using Avancira.Application.Origin;
using Avancira.Infrastructure.Auth;
using Avancira.Infrastructure.Auth.Jwt;
using Avancira.Infrastructure.Caching;
using Avancira.Infrastructure.Cors;
using Avancira.Infrastructure.Exceptions;
using Avancira.Infrastructure.Jobs;
using Avancira.Infrastructure.Mail;
using Avancira.Infrastructure.Persistence;
using Avancira.Infrastructure.RateLimit;
using Avancira.Infrastructure.SecurityHeaders;
using Avancira.Infrastructure.Storage.Files;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Avancira.ServiceDefaults;
using Avancira.Infrastructure.OpenApi;
using Avancira.Infrastructure.Logging.Serilog;
using Avancira.Infrastructure.Identity;
using Avancira.Infrastructure.Storage;

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
        builder.Services.AddCorsPolicy(builder.Configuration);
        builder.Services.ConfigureFileStorage();
        builder.Services.ConfigureJwtAuth();
        builder.Services.ConfigureOpenApi();
        builder.Services.ConfigureJobs(builder.Configuration);
        builder.Services.ConfigureMailing();
        builder.Services.ConfigureCaching(builder.Configuration);
        builder.Services.AddExceptionHandler<CustomExceptionHandler>();
        builder.Services.AddProblemDetails();
        builder.Services.AddHealthChecks();
        builder.Services.AddOptions<OriginOptions>().BindConfiguration(nameof(OriginOptions));

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
            RequestPath = new PathString("/assets")
        });
        app.UseStaticFilesUploads();
        app.UseAuthentication();
        app.UseAuthorization();

        // Current user middleware
        app.UseMiddleware<CurrentUserMiddleware>();

        return app;
    }
}
