namespace Avancira.BFF.Services;

/// <summary>
/// Result of accessing a token
/// </summary>
public class AccessTokenResult
{
    public bool Success { get; set; }
    public string? Token { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public TimeSpan ExpiresIn => ExpiresAt - DateTimeOffset.UtcNow;
    public string? Error { get; set; }

    /// <summary>Token needs refresh (is valid but approaching expiration)</summary>
    public bool NeedsRefresh { get; set; }

    /// <summary>Token is expired or missing - must refresh or re-login</summary>
    public bool IsExpired => ExpiresAt <= DateTimeOffset.UtcNow;

    public static AccessTokenResult NotFound(string error = "Session not found")
        => new() { Success = false, Error = error };

    public static AccessTokenResult Expired(string error = "Token expired")
        => new() { Success = false, Error = error, ExpiresAt = DateTimeOffset.UtcNow };

    public static AccessTokenResult SuccessResult(string token, DateTimeOffset expiresAt, bool needsRefresh = false)
        => new() { Success = true, Token = token, ExpiresAt = expiresAt, NeedsRefresh = needsRefresh };
}
