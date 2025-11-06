namespace Avancira.BFF.Extensions;

using Avancira.Infrastructure.Health;

public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Configures middleware pipeline
    /// </summary>
    public static WebApplication UseBffMiddleware(
        this WebApplication app,
        IWebHostEnvironment environment)
    {
        // Security headers
        app.UseSecurityHeaders(environment);

        // Error handling
        if (environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
            app.UseSwagger();
            app.UseSwaggerUI();
        }
        else
        {
            app.UseExceptionHandler("/error");
            app.UseHsts();
        }

        // Request pipeline
        app.UseHttpsRedirection();
        app.UseWebSockets();
        app.UseRouting();
        app.UseCors();
        app.UseAuthentication();
        app.UseAuthorization();

        return app;
    }

    /// <summary>
    /// Maps endpoints (controllers, health checks, reverse proxy, debug)
    /// </summary>
    public static WebApplication MapBffEndpoints(
        this WebApplication app,
        IWebHostEnvironment environment)
    {
        app.MapAvanciraHealthChecks();
        app.MapControllers();
        app.MapReverseProxy();

        // Error endpoint
        app.MapGet("/error", () => Results.Problem("An error occurred"))
            .ExcludeFromDescription();

        return app;
    }

    /// <summary>
    /// Adds security headers to responses
    /// </summary>
    private static WebApplication UseSecurityHeaders(
        this WebApplication app,
        IWebHostEnvironment environment)
    {
        app.Use(async (context, next) =>
        {
            context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
            context.Response.Headers.Append("X-Frame-Options", "DENY");
            context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
            context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");

            if (!environment.IsDevelopment())
            {
                context.Response.Headers.Append(
                    "Content-Security-Policy",
                    "default-src 'self'; frame-ancestors 'none';");

                context.Response.Headers.Append(
                    "Strict-Transport-Security",
                    "max-age=31536000; includeSubDomains; preload");
            }

            await next();
        });

        return app;
    }
}