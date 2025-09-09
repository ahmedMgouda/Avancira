using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Avancira.Application.Common;
using Avancira.Application.Identity.Tokens;
using Avancira.Infrastructure.Auth;
using OpenIddict.Abstractions;
using OpenIddict.Server;

namespace Avancira.Infrastructure.Identity;

internal static class RefreshTokenHash
{
    public static string ComputeHash(string token)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}

/// <summary>
/// Validates the *incoming* refresh token against the stored session hash.
/// Uses ValidateTokenRequestContext.RefreshTokenPrincipal.
/// </summary>
public sealed class RefreshTokenHashValidator : IOpenIddictServerHandler<OpenIddictServerEvents.ValidateTokenRequestContext>
{
    private readonly ISessionService _sessionService;

    public RefreshTokenHashValidator(ISessionService sessionService)
        => _sessionService = sessionService;

    public async ValueTask HandleAsync(OpenIddictServerEvents.ValidateTokenRequestContext context)
    {
        if (context.Request is null ||
            context.Request.GrantType != OpenIddictConstants.GrantTypes.RefreshToken ||
            string.IsNullOrEmpty(context.Request.RefreshToken))
        {
            return;
        }

        var principal = context.RefreshTokenPrincipal; 
        if (principal is null)
        {
            context.Reject(OpenIddictConstants.Errors.InvalidGrant, "Missing refresh token principal.");
            return;
        }

        var userId = principal.GetClaim(OpenIddictConstants.Claims.Subject);
        var sessionIdClaim = principal.GetClaim(AuthConstants.Claims.SessionId);

        if (string.IsNullOrEmpty(userId) ||
            string.IsNullOrEmpty(sessionIdClaim) ||
            !Guid.TryParse(sessionIdClaim, out var sessionId))
        {
            context.Reject(OpenIddictConstants.Errors.InvalidGrant, "The refresh token is no longer valid.");
            return;
        }

        var hash = RefreshTokenHash.ComputeHash(context.Request.RefreshToken);
        if (!await _sessionService.ValidateSessionAsync(userId, sessionId, hash))
        {
            context.Reject(OpenIddictConstants.Errors.InvalidGrant, "The refresh token is no longer valid.");
        }
    }
}

/// <summary>
/// Persists or updates the session hash for the *newly issued* refresh token.
/// Run during ApplyTokenResponseContext (no Principal available).
/// </summary>
public sealed class RefreshTokenHashStore : IOpenIddictServerHandler<OpenIddictServerEvents.ApplyTokenResponseContext>
{
    private readonly ISessionService _sessionService;
    private readonly IClientInfoService _clientInfoService;

    public RefreshTokenHashStore(ISessionService sessionService, IClientInfoService clientInfoService)
    {
        _sessionService = sessionService;
        _clientInfoService = clientInfoService;
    }

    public async ValueTask HandleAsync(OpenIddictServerEvents.ApplyTokenResponseContext context)
    {
        var newRefresh = context.Response?.RefreshToken;
        if (string.IsNullOrEmpty(newRefresh))
            return;

        // The only thing we have at this stage is the issued token + original request.
        // We must re-extract identifiers from the principals that were already validated.

        ClaimsPrincipal? principal = null;

        if (string.Equals(context.Request?.GrantType, OpenIddictConstants.GrantTypes.RefreshToken, StringComparison.Ordinal))
        {
            // Refresh token grant → reuse the validated RefreshTokenPrincipal
            principal = context.Transaction.GetProperty<ClaimsPrincipal>(typeof(OpenIddictServerEvents.ValidateTokenRequestContext).FullName + ".RefreshTokenPrincipal");
        }
        else if (string.Equals(context.Request?.GrantType, OpenIddictConstants.GrantTypes.AuthorizationCode, StringComparison.Ordinal))
        {
            principal = context.Transaction.GetProperty<ClaimsPrincipal>(typeof(OpenIddictServerEvents.ValidateTokenRequestContext).FullName + ".AuthorizationCodePrincipal");
        }

        if (principal is null)
            return;

        var userId = principal.GetClaim(OpenIddictConstants.Claims.Subject);
        var sessionIdClaim = principal.GetClaim(AuthConstants.Claims.SessionId);

        if (string.IsNullOrEmpty(userId) ||
            string.IsNullOrEmpty(sessionIdClaim) ||
            !Guid.TryParse(sessionIdClaim, out var sessionId))
        {
            return;
        }

        var newHash = RefreshTokenHash.ComputeHash(newRefresh);
        var refreshExpiry = DateTime.UtcNow.AddDays(7);

        if (string.Equals(context.Request?.GrantType, OpenIddictConstants.GrantTypes.RefreshToken, StringComparison.Ordinal))
        {
            var currentHash = RefreshTokenHash.ComputeHash(context.Request!.RefreshToken!);
            var updated = await _sessionService.UpdateSessionAsync(userId, sessionId, currentHash, newHash, refreshExpiry);

            if (!updated)
            {
                context.Reject(OpenIddictConstants.Errors.InvalidGrant, "The refresh token is no longer valid.");
            }
        }
        else
        {
            var clientInfo = await _clientInfoService.GetClientInfoAsync();
            await _sessionService.StoreSessionAsync(userId, sessionId, newHash, clientInfo, refreshExpiry);
        }
    }
}
