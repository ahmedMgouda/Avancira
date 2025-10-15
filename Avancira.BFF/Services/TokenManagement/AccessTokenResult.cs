namespace Avancira.BFF.Services;

/// <summary>
/// Result of accessing a token
/// FIXED: Clear status indicators for client logic
/// </summary>
public class AccessTokenResult
{
    /// <summary>
    /// Whether token retrieval succeeded
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// The JWT access token itself
    /// </summary>
    public string? Token { get; set; }

    /// <summary>
    /// When this access token expires
    /// </summary>
    public DateTimeOffset ExpiresAt { get; set; }

    /// <summary>
    /// Time remaining until expiration
    /// </summary>
    public TimeSpan ExpiresIn => ExpiresAt - DateTimeOffset.UtcNow;

    /// <summary>
    /// Error message if Success is false
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// Token needs refresh (valid but approaching expiration)
    /// Client should call refresh endpoint
    /// </summary>
    public bool NeedsRefresh { get; set; }

    /// <summary>
    /// Token is expired or missing - must refresh or re-login
    /// </summary>
    public bool IsExpired => ExpiresAt <= DateTimeOffset.UtcNow;

    /// <summary>
    /// Factory: Token not found in cache
    /// </summary>
    public static AccessTokenResult NotFound(string error = "Session not found")
        => new()
        {
            IsSuccess = false,
            Error = error,
            ExpiresAt = DateTimeOffset.UtcNow
        };

    /// <summary>
    /// Factory: Token is expired or invalid
    /// </summary>
    public static AccessTokenResult Expired(string error = "Token expired")
        => new()
        {
            IsSuccess = false,
            Error = error,
            ExpiresAt = DateTimeOffset.UtcNow
        };

    /// <summary>
    /// Factory: Token is valid and available
    /// </summary>
    public static AccessTokenResult Success(
        string token,
        DateTimeOffset expiresAt,
        bool needsRefresh = false)
        => new()
        {
            IsSuccess = true,
            Token = token,
            ExpiresAt = expiresAt,
            NeedsRefresh = needsRefresh
        };
}
