using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;

namespace Avancira.BFF.Services;

public interface ITokenManagementService
{
    Task<TokenSnapshot> CaptureAsync(HttpContext httpContext);
    void StoreSessionSnapshot(HttpContext httpContext, TokenSnapshot snapshot);
    void ClearSession(HttpContext httpContext);
}

public sealed class TokenManagementService : ITokenManagementService
{
    public async Task<TokenSnapshot> CaptureAsync(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        var expiresAtString = await httpContext.GetTokenAsync("expires_at");
        if (string.IsNullOrWhiteSpace(expiresAtString))
        {
            return TokenSnapshot.Empty;
        }

        if (!DateTimeOffset.TryParse(expiresAtString, out var expiresAt))
        {
            return TokenSnapshot.Empty;
        }

        var expiresIn = expiresAt - DateTimeOffset.UtcNow;
        if (expiresIn < TimeSpan.Zero)
        {
            expiresIn = TimeSpan.Zero;
        }

        return new TokenSnapshot(expiresAt, expiresIn);
    }

    public void StoreSessionSnapshot(HttpContext httpContext, TokenSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        if (snapshot.ExpiresAt is { } expiresAt)
        {
            httpContext.Session.SetString("ExpiresAt", expiresAt.ToString("o"));
        }
        else
        {
            httpContext.Session.Remove("ExpiresAt");
        }
    }

    public void ClearSession(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        httpContext.Session.Remove("ExpiresAt");
    }
}

public sealed record TokenSnapshot(DateTimeOffset? ExpiresAt, TimeSpan? ExpiresIn)
{
    public static TokenSnapshot Empty { get; } = new(null, null);
}
