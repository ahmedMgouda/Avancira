namespace Avancira.BFF.Services;

/// <summary>
/// Configuration options for token management
/// </summary>
public class TokenManagementOptions
{
    public const string SectionName = "TokenManagement";

    /// <summary>
    /// Maximum duration to cache an access token in memory
    /// Default: 5 minutes (even if token lives longer)
    /// Prevents stale tokens if auth server changes user permissions
    /// </summary>
    public int MaxCacheDurationSeconds { get; set; } = 300; // 5 minutes

    /// <summary>
    /// Seconds before token expiration to signal refresh needed
    /// Default: 60 seconds
    /// Gives client time to refresh before token actually expires
    /// </summary>
    public int RefreshThresholdSeconds { get; set; } = 60;

    /// <summary>
    /// How long to keep session revocation record
    /// Default: 24 hours
    /// Prevents race conditions between cache and database deletes
    /// </summary>
    public int RevokedSessionCacheDurationHours { get; set; } = 24;
}