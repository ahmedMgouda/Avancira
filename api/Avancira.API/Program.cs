using Avancira.Infrastructure.Composition;
using Avancira.Infrastructure.Health;
using Avancira.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OpenIddict.Validation.AspNetCore;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.FileProviders;
using Avancira.Infrastructure.Auth;
using Avancira.Infrastructure.OpenApi;
using System;
using System.Collections.Generic;
using System.Net.Http;


public partial class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);


        builder.AddAvanciraInfrastructure();


        var isAspire = builder.Configuration.GetConnectionString("avancira") is not null ||
                       builder.Environment.IsDevelopment();

        if (isAspire)
        {
            builder.AddNpgsqlDbContext<AvanciraDbContext>("avancira", configureDbContextOptions: opts =>
            {
                opts.EnableSensitiveDataLogging();
            });
        }
        else
        {
            builder.Services.BindDbContext<AvanciraDbContext>();
        }


        var authIssuer = builder.Configuration["Auth:Issuer"]
            ?? throw new InvalidOperationException("Missing 'Auth:Issuer' configuration.");

        builder.Services.AddAuthentication(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);
        builder.Services.AddAuthorization();

        builder.Services.AddOpenIddict()
            .AddValidation(options =>
            {
                options.SetIssuer(authIssuer);

                // Introspection-based validation (for encrypted access tokens)
                options.UseIntrospection()
                       .SetClientId("resource_server")
                       .SetClientSecret("846B62D0-DEF9-4215-A99D-86E6B8DAB342");

                options.UseSystemNetHttp();
                options.UseAspNetCore();

                // Optional logging for development
                if (builder.Environment.IsDevelopment())
                {
                    options.AddEventHandler<
                        OpenIddict.Validation.OpenIddictValidationEvents.ProcessAuthenticationContext>(
                        handler => handler.UseInlineHandler(context =>
                        {
                            if (context.AccessTokenPrincipal is not null)
                            {
                                Console.WriteLine("Token validated successfully");
                                Console.WriteLine($"Subject: {context.AccessTokenPrincipal.FindFirst("sub")?.Value}");
                                Console.WriteLine($"Scopes: {context.AccessTokenPrincipal.FindFirst("scope")?.Value}");
                            }
                            else
                            {
                                Console.WriteLine("Token validation failed");
                                Console.WriteLine($"Error: {context.Error}");
                                Console.WriteLine($"Description: {context.ErrorDescription}");
                            }
                            return default;
                        }));
                }
            });

     
        builder.Services.AddControllers(options =>
        {
            options.Filters.Add(new ProducesAttribute("application/json"));
        })
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
            options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        });

        var healthChecksBuilder = builder.Services.AddAvanciraHealthChecks<AvanciraDbContext>(builder.Configuration);

        ConfigureDownstreamAuthHealthCheck(builder, authIssuer, healthChecksBuilder);

        var app = builder.Build();


        //app.MapDefaultEndpoints(); // Aspire service defaults (health, liveness, etc.)

        //app.UseRateLimit();
       // app.UseSecurityHeaders();
       // app.UseExceptionHandler();



        app.UseCorsPolicy();
       // app.UseOpenApi();
        // app.UseJobDashboard(app.Configuration);

        // Static file serving
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
            app.Logger.LogWarning("Static assets directory '{AssetsPath}' not found.", assetsPath);
        }

        //app.UseStaticFilesUploads();

        app.UseHttpsRedirection();
        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();

        // Current user middleware for audit context
        //app.UseMiddleware<CurrentUserMiddleware>();

        app.MapControllers();
        app.MapAvanciraHealthChecks();


        // ═════════════════════════════════════════════════════════
        // 9️⃣ STARTUP LOGGING
        // ═════════════════════════════════════════════════════════
        app.Logger.LogInformation("Avancira API Started");
        app.Logger.LogInformation("Environment: {Env}", app.Environment.EnvironmentName);
        app.Logger.LogInformation("Auth Issuer: {Issuer}", authIssuer);
        app.Logger.LogInformation("Health: /health/live, /health/ready, /health");
        app.Logger.LogInformation("Endpoints: /api/*");

        await app.RunAsync();
    }

    private static void ConfigureDownstreamAuthHealthCheck(
        WebApplicationBuilder builder,
        string authIssuer,
        IHealthChecksBuilder healthChecksBuilder)
    {
        var downstreamSection = builder.Configuration.GetSection("HealthCheck:Downstream:AuthServer");
        var isEnabled = !downstreamSection.Exists() || downstreamSection.GetValue("Enabled", true);

        if (!isEnabled)
        {
            return;
        }

        var healthPath = downstreamSection.GetValue<string>("HealthPath") ?? "health/live";
        var baseUrl = downstreamSection.GetValue<string>("Url") ?? authIssuer;
        var endpoint = DownstreamEndpoint.Create("auth_server", baseUrl, healthPath);

        var timeoutSeconds = downstreamSection.GetValue<int?>("Timeout");
        if (timeoutSeconds.HasValue && timeoutSeconds.Value > 0)
        {
            endpoint.Timeout = TimeSpan.FromSeconds(timeoutSeconds.Value);
        }

        var failureStatusValue = downstreamSection.GetValue<string>("FailureStatus");
        var failureStatus = Enum.TryParse<HealthStatus>(failureStatusValue, ignoreCase: true, out var parsed)
            ? parsed
            : HealthStatus.Degraded;

        var httpClientTimeout = endpoint.Timeout ?? TimeSpan.FromSeconds(5);

        builder.Services.AddHttpClient("downstream-health-check", client =>
        {
            client.Timeout = httpClientTimeout;
            client.DefaultRequestHeaders.Add("User-Agent", "Avancira-API-HealthCheck/1.0");
        })
        .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            AllowAutoRedirect = false
        });

        builder.Services.AddSingleton(new DownstreamHealthCheckOptions
        {
            HttpClientName = "downstream-health-check",
            Endpoints = new List<DownstreamEndpoint> { endpoint }
        });

        healthChecksBuilder.AddCheck<DownstreamHealthCheck>(
            "auth_server",
            failureStatus: failureStatus,
            tags: new[] { "ready", "downstream" });
    }

}

public partial class Program { }
