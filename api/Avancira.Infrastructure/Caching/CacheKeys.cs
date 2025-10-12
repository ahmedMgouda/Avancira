using System;

namespace Avancira.Infrastructure.Caching;

public static class CacheKeys
{
    // Access token cache: "token:userId:sessionId"
    public const string AccessTokenPrefix = "token";
    public static string AccessToken(string userId, Guid sessionId)
        => $"{AccessTokenPrefix}:{userId}:{sessionId}";

    // Session info cache: "session:sessionId"
    public const string SessionPrefix = "session";
    public static string Session(Guid sessionId)
        => $"{SessionPrefix}:{sessionId}";

    // All sessions for user: "user:sessions:userId"
    public const string UserSessionsPrefix = "user:sessions";
    public static string UserSessions(string userId)
        => $"{UserSessionsPrefix}:{userId}";

    // Cache tags for bulk invalidation: "tag:user:userId"
    public const string UserTag = "tag:user";
    public static string UserTagKey(string userId)
        => $"{UserTag}:{userId}";

    // Revoked sessions list: "revoked:sessionId" (for 24h after revocation)
    public const string RevokedSessionPrefix = "revoked";
    public static string RevokedSession(Guid sessionId)
        => $"{RevokedSessionPrefix}:{sessionId}";
}
