
namespace Avancira.BFF.Configuration;

/// <summary>
/// Main BFF configuration settings
/// </summary>
public class BffSettings
{
    public AuthSettings Auth { get; set; } = new();
    public CookieSettings Cookie { get; set; } = new();
    public CorsSettings Cors { get; set; } = new();
    public string ApiBaseUrl { get; set; } = "https://localhost:9000";
    public string[] EssentialClaims { get; set; } = new[] { "sub", "sid" };
    public string? RedisConnection { get; set; }

    /// <summary>
    /// Indicates if Redis is configured
    /// </summary>
    public bool HasRedis => !string.IsNullOrEmpty(RedisConnection);
}


/// <summary>
/// Authentication server settings
/// </summary>
public class AuthSettings
{
    public string Authority { get; set; } = "https://localhost:9100";
    public string? MetadataAddress { get; set; }
    public string? ExternalIssuer { get; set; }
    public string ClientId { get; set; } = "bff-client";
    public string ClientSecret { get; set; } = string.Empty;
    public bool RequireHttpsMetadata { get; set; } = true;
    public string DefaultRedirectUrl { get; set; } = "https://localhost:4200/";
}


/// <summary>
/// Cookie configuration
/// </summary>
public class CookieSettings
{
    public string Name { get; set; } = ".Avancira.Auth";
    public int ExpirationHours { get; set; } = 8;
}


/// <summary>
/// CORS configuration
/// </summary>
public class CorsSettings
{
    public string[] AllowedOrigins { get; set; } = Array.Empty<string>();
}
