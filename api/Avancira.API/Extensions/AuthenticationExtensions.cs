using Avancira.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;
using OpenIddict.Validation.AspNetCore;
using OpenIddict.Validation;
using OpenIddict.Abstractions;

namespace Avancira.API.Extensions;

/// <summary>
/// Authentication configuration for the API project.
/// This project validates access tokens issued by the Auth server.
/// </summary>
public static class AuthenticationExtensions
{
    public static IServiceCollection AddApiAuthentication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddAuthentication(options =>
        {
            options.DefaultScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
        });

        services.AddAuthorization();
        return services;
    }

    public static IServiceCollection AddApiOpenIddictValidation(
        this IServiceCollection services,
        string authServerUrl)
    {
        ArgumentNullException.ThrowIfNull(services);

        if (string.IsNullOrWhiteSpace(authServerUrl))
        {
            throw new ArgumentException(
                "Auth server URL is required for OpenIddict validation",
                nameof(authServerUrl));
        }

        services.AddOpenIddict()
            .AddCore(options =>
            {
                options.UseEntityFrameworkCore()
                       .UseDbContext<AvanciraDbContext>();
            })
            .AddValidation(options =>
            {
                // === Basic Setup ===
                options.SetIssuer(authServerUrl);

                // Use introspection because Auth and API run in different containers
                options.UseIntrospection()
                       .SetClientId("bff-client")
                       .SetClientSecret("dev-bff-secret");

                options.UseSystemNetHttp();
                options.UseAspNetCore();

                // === 🔍 Diagnostic Event Hooks ===

                options.AddEventHandler<OpenIddictValidationEvents.ApplyIntrospectionRequestContext>(builder =>
                {
                    builder.UseInlineHandler(context =>
                    {
                        var httpContext = context.Transaction.GetProperty<HttpContext>(typeof(HttpContext).FullName!);
                        var logger = httpContext?.RequestServices
                            .GetRequiredService<ILoggerFactory>()
                            .CreateLogger("OpenIddict.Introspection");

                        logger?.LogInformation("📡 Sending introspection request to: {Address}",
                            context.RequestUri?.AbsoluteUri ?? "unknown");

                        return default;
                    });
                });

                options.AddEventHandler<OpenIddictValidationEvents.HandleIntrospectionResponseContext>(builder =>
                {
                    builder.UseInlineHandler(context =>
                    {
                        var httpContext = context.Transaction.GetProperty<HttpContext>(typeof(HttpContext).FullName!);
                        var logger = httpContext?.RequestServices
                            .GetRequiredService<ILoggerFactory>()
                            .CreateLogger("OpenIddict.Introspection");

                        logger?.LogInformation("🔍 Introspection response: {Response}",
                            context.Response?.ToString() ?? "(null)");

                        return default;
                    });
                });

                options.AddEventHandler<OpenIddictValidationEvents.ProcessAuthenticationContext>(builder =>
                {
                    builder.UseInlineHandler(context =>
                    {
                        var httpContext = context.Transaction.GetProperty<HttpContext>(typeof(HttpContext).FullName!);
                        var logger = httpContext?.RequestServices
                            .GetRequiredService<ILoggerFactory>()
                            .CreateLogger("OpenIddict.Validation");

                        if (context.AccessTokenPrincipal is not null)
                        {
                            logger?.LogInformation(
                                "✅ Token validated successfully. Sub: {Sub}, Scopes: {Scopes}",
                                context.AccessTokenPrincipal.GetClaim(OpenIddictConstants.Claims.Subject));
                        }
                        else
                        {
                            logger?.LogWarning(
                                "❌ Token validation failed. Error: {Error}, Description: {Description}",
                                context.Error ?? "unknown",
                                context.ErrorDescription ?? "no details");
                        }

                        return default;
                    });
                });
            });

        return services;
    }
}
