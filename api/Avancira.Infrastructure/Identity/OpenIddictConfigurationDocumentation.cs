namespace Avancira.Infrastructure.Identity;

/// <summary>
/// IMPORTANT: Configuration needed in appsettings.json
/// 
/// Example configuration:
/// {
///   "Auth": {
///     "Issuer": "https://localhost:5005",  // Auth server URL
///     "ClientId": "bff-client",            // BFF client ID
///     "ClientSecret": "secret-key",        // BFF client secret
///     "Authority": "https://localhost:5005", // For BFF: same as Issuer
///     "OpenIddict": {
///       "AccessTokenLifetime": "00:15:00",    // 15 minutes
///       "RefreshTokenLifetime": "14.00:00:00", // 14 days
///       "AuthorizationCodeLifetime": "00:10:00" // 10 minutes
///     }
///   }
/// }
/// 
/// PRODUCTION CONFIGURATION:
/// {
///   "Auth": {
///     "Issuer": "https://yourdomain.com/auth",  // Auth server URL with /auth path
///     "ClientId": "bff-client",
///     "ClientSecret": "${BFF_CLIENT_SECRET}",  // Use environment variable
///     "Authority": "https://yourdomain.com/auth", // Through nginx
///     "RequireHttpsMetadata": true,  // ALWAYS true in production
///     "OpenIddict": {
///       "AccessTokenLifetime": "00:15:00",
///       "RefreshTokenLifetime": "07.00:00:00", // 7 days for production
///       "AuthorizationCodeLifetime": "00:10:00"
///     }
///   }
/// }
/// </summary>
public static class OpenIddictConfigurationDocumentation
{
    // This class exists only for documentation
    // See example configuration above

    /// <summary>
    /// Key configuration points:
    /// 
    /// 1. ISSUER vs AUTHORITY
    ///    - Issuer: The public URL of Auth server (for token issuer claim)
    ///    - Authority: What BFF uses to find metadata (might be different in nginx)
    ///    
    ///    Local dev:   Issuer = Authority = "https://localhost:5005"
    ///    Production: Issuer = "https://yourdomain.com/auth"
    ///                Authority = "https://yourdomain.com/auth" (through nginx)
    ///
    /// 2. SCOPES
    ///    Must include:
    ///    - "openid": Get subject (user ID)
    ///    - "profile": Get name, given_name, family_name
    ///    - "email": Get email address
    ///    - "offline_access": Get refresh token
    ///    - "api": Get access to API (FIXED in this version)
    ///
    /// 3. TOKEN LIFETIMES
    ///    - Access token: SHORT (15 min) - compromised token has limited window
    ///    - Refresh token: LONG (7-14 days) - allows session persistence
    ///    - Auth code: VERY SHORT (10 min) - code exchange window
    ///
    /// 4. REFERENCE REFRESH TOKENS
    ///    - Each refresh token gets an ID stored in database
    ///    - When user logs out, we revoke the reference
    ///    - Prevents old tokens from being used
    ///    - Opposite of self-contained JWT refresh tokens (can't revoke)
    /// </summary>
    public const string Documentation = "";
}