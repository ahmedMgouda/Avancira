namespace Avancira.BFF.Services;

/// <summary>
/// Configuration options for token management
/// FIXED: Clear documentation of each setting
/// </summary>
public class TokenManagementOptions
{
    public const string SectionName = "TokenManagement";

    /// <summary>
    /// Maximum duration to cache an access token in memory
    /// Default: 5 minutes (300 seconds)
    /// 
    /// Even if token lives longer (15+ minutes), we only cache for 5 minutes.
    /// This prevents stale tokens if auth server revokes permissions.
    /// 
    /// Shorter duration = more auth server calls but fresher permissions
    /// Longer duration = fewer calls but stale permissions possible
    /// 5 minutes is a good balance for most applications
    /// </summary>
    public int MaxCacheDurationSeconds { get; set; } = 300;

    /// <summary>
    /// Seconds before token expiration to signal refresh needed
    /// Default: 60 seconds
    /// 
    /// If token expires in 60 seconds or less, we return NeedsRefresh=true
    /// This gives client time to refresh before token actually expires.
    /// 
    /// Example: Token expires in 15 minutes
    /// - If RefreshThreshold is 60s: NeedsRefresh false, token good
    /// - Token expires in 50s: NeedsRefresh true, client should refresh
    /// - Token expires: NeedsRefresh true, client must refresh immediately
    /// </summary>
    public int RefreshThresholdSeconds { get; set; } = 60;

    /// <summary>
    /// How long to keep session revocation record
    /// Default: 24 hours
    /// 
    /// After revoking a session, we track it for 24 hours in cache.
    /// This prevents race conditions where:
    /// 1. Session revoked at DB
    /// 2. Old cache entry still has valid token
    /// 3. Request served with revoked token
    /// 
    /// By tracking revocations, we reject token requests for 24 hours.
    /// </summary>
    public int RevokedSessionCacheDurationHours { get; set; } = 24;

    /// <summary>
    /// Session idle timeout (when to consider session stale for activity updates)
    /// Default: 120 minutes (2 hours)
    /// 
    /// LastActivityAt is only written to database every N minutes
    /// to reduce database load. This value controls that threshold.
    /// </summary>
    public int SessionIdleTimeoutMinutes { get; set; } = 120;
}