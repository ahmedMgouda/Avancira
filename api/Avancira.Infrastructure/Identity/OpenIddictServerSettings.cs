namespace Avancira.Infrastructure.Identity;

/// <summary>
/// OpenIddict server settings from configuration
/// FIXED: Proper token lifetime defaults
/// </summary>
public class OpenIddictServerSettings
{
    /// <summary>
    /// How long access tokens are valid
    /// Default: 15 minutes
    /// 
    /// SHORT-LIVED tokens are safer:
    /// - Limits damage if token is compromised
    /// - Reduces permission staleness
    /// - Requires refresh token for longer sessions
    /// 
    /// 15 minutes is industry standard
    /// </summary>
    public TimeSpan AccessTokenLifetime { get; set; } = TimeSpan.FromMinutes(15);

    /// <summary>
    /// How long refresh tokens are valid
    /// Default: 14 days
    /// 
    /// LONG-LIVED refresh tokens allow:
    /// - Users stay logged in across sessions
    /// - Remember-me functionality
    /// - Long-running background jobs
    /// 
    /// 14 days is standard for "offline_access" scope
    /// Reduce this to 7 days for security-sensitive applications
    /// </summary>
    public TimeSpan RefreshTokenLifetime { get; set; } = TimeSpan.FromDays(14);

    /// <summary>
    /// How long authorization codes are valid
    /// Default: 10 minutes
    /// 
    /// Authorization codes are short-lived for security:
    /// - Granted by auth server during login
    /// - Exchanged for tokens at token endpoint
    /// - If intercepted, expiration limits damage
    /// 
    /// 10 minutes allows time for code exchange
    /// while limiting window for attacks
    /// </summary>
    public TimeSpan AuthorizationCodeLifetime { get; set; } = TimeSpan.FromMinutes(10);
}
