using Avancira.Application.Storage;
using Avancira.Infrastructure.Storage;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;

namespace Avancira.Infrastructure.Storage;

public static class Extensions
{
    /// <summary>
    /// Register file storage services
    /// </summary>
    public static IServiceCollection AddFileStorage(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Bind configuration
        services.Configure<FileStorageSettings>(
            configuration.GetSection("Avancira:Storage"));

        // Register the storage service
        services.AddSingleton<IFileStorageService, LocalFileStorageService>();

        return services;
    }

    /// <summary>
    /// Configure static file serving for uploaded files
    /// </summary>
    public static IApplicationBuilder UseFileStorage(
        this IApplicationBuilder app,
        IConfiguration configuration)
    {
        var settings = configuration.GetSection("Avancira:Storage")
            .Get<FileStorageSettings>() ?? new FileStorageSettings();

        var storagePath = Path.Combine(
            Directory.GetCurrentDirectory(),
            settings.StorageDirectory ?? "storage");

        // Ensure directory exists
        if (!Directory.Exists(storagePath))
            Directory.CreateDirectory(storagePath);

        // Serve files from storage directory
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(storagePath),
            RequestPath = new PathString(settings.PublicUrlPrefix ?? "/api/files"),
            OnPrepareResponse = ctx =>
            {
                // Add cache headers for better performance
                ctx.Context.Response.Headers.Append("Cache-Control", "public,max-age=31536000");
            }
        });

        return app;
    }
}