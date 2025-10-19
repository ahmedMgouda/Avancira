using Avancira.Auth.Extensions;
using Avancira.Infrastructure;
using Avancira.Infrastructure.Identity;
using Avancira.Infrastructure.Persistence;
using System.Text.Json;
using System.Text.Json.Serialization;

public partial class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        
        // ===== STEP 1: Database Configuration =====
        var isUsingAspire = builder.Configuration.GetConnectionString("avancira") != null ||
                   builder.Environment.IsDevelopment();
        
        if (isUsingAspire)
        {
            // Aspire mode - let Aspire handle database configuration
            builder.ConfigureAvanciraFramework();
            builder.AddNpgsqlDbContext<AvanciraDbContext>("avancira", configureDbContextOptions: options =>
            {
                options.EnableSensitiveDataLogging();
            });
        }
        else
        {
            // Production mode - use traditional database configuration
            builder.ConfigureAvanciraFramework();
            builder.Services.BindDbContext<AvanciraDbContext>();
        }

        // ===== STEP 2: Add OpenIddict SERVER (Auth project only) =====
        // CRITICAL: This must come after ConfigureAvanciraFramework
        builder.Services.AddInfrastructureIdentity(builder.Configuration);

        // ===== STEP 3: Configure Auth server authentication =====
        // This configures Identity cookies (ApplicationScheme and ExternalScheme)
        builder.Services.AddAuthServerAuthentication();

        // ===== STEP 4: Add external authentication providers =====
        // Get logger for external auth configuration
        using var authLoggerFactory = LoggerFactory.Create(logging => logging.AddConsole());
        var authLogger = authLoggerFactory.CreateLogger("AuthenticationExtensions");
        
        builder.Services.AddExternalAuthentication(builder.Configuration, authLogger);

        // ===== STEP 5: Add MVC with Views (for login/register pages) =====
        builder.Services.AddControllersWithViews(options =>
        {
            // API controllers should return JSON
            // options.Filters.Add(new ProducesAttribute("text/html")); // REMOVED
        })
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.Converters.Add(
                new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        });


        builder.Services.AddMemoryCache();

        var app = builder.Build();

        app.UseAvanciraFramework(runDatabasePreparation: true);
        
        app.UseHttpsRedirection();

        app.MapControllers();
        
        app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}");

        app.Run();
    }
}

// Required for integration tests
public partial class Program { }