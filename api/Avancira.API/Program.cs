using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Validation.AspNetCore;
using System.Text.Json;
using System.Text.Json.Serialization;

public partial class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        var authServerUrl = builder.Configuration["Auth:Issuer"];
        if (string.IsNullOrWhiteSpace(authServerUrl))
        {
            throw new InvalidOperationException(
                "Auth:Issuer configuration is required. " +
                "Example: https://localhost:9100");
        }

        Console.WriteLine($"🔐 API configured with Auth Issuer: {authServerUrl}");

        // ===== Authentication =====
        builder.Services.AddAuthentication(options =>
        {
            options.DefaultScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
        });

        builder.Services.AddAuthorization();

        // ===== OpenIddict Validation (Introspection) =====
        builder.Services.AddOpenIddict()
            .AddValidation(options =>
            {
                options.SetIssuer(authServerUrl);

                // Use introspection to validate encrypted tokens
                options.UseIntrospection()
                       .SetClientId("resource_server")
                       .SetClientSecret("846B62D0-DEF9-4215-A99D-86E6B8DAB342");

                options.UseSystemNetHttp();
                options.UseAspNetCore();

                // Debug logging
                options.AddEventHandler<OpenIddict.Validation.OpenIddictValidationEvents.ProcessAuthenticationContext>(
                    handler => handler.UseInlineHandler(context =>
                    {
                        if (context.AccessTokenPrincipal is not null)
                        {
                            Console.WriteLine("✅ TOKEN VALIDATED");
                            Console.WriteLine($"   Subject: {context.AccessTokenPrincipal.FindFirst("sub")?.Value}");
                            Console.WriteLine($"   Scopes: {context.AccessTokenPrincipal.FindFirst("scope")?.Value}");
                        }
                        else
                        {
                            Console.WriteLine("❌ TOKEN VALIDATION FAILED");
                            Console.WriteLine($"   Error: {context.Error}");
                            Console.WriteLine($"   Description: {context.ErrorDescription}");
                        }
                        return default;
                    }));
            });

        // ===== Controllers =====
        builder.Services.AddControllers(options =>
        {
            options.Filters.Add(new ProducesAttribute("application/json"));
        })
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.Converters.Add(
                new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        });

        // ===== Build App =====
        var app = builder.Build();

        app.UseHttpsRedirection();
        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();

        Console.WriteLine("✅ API Started successfully");
        app.Run();
    }
}

public partial class Program { }