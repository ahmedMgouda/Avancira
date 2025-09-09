using System;
using System.Security.Cryptography;
using System.Text;
using Avancira.Application.Common;
using Avancira.Application.Identity.Tokens;
using Avancira.Infrastructure.Auth;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using static OpenIddict.Server.OpenIddictServerEvents;

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

public sealed class RefreshTokenHashValidator : IOpenIddictServerHandler<ValidateTokenRequestContext>
{
    private readonly ISessionService _sessionService;

    public RefreshTokenHashValidator(ISessionService sessionService)
        => _sessionService = sessionService;

    public async ValueTask HandleAsync(ValidateTokenRequestContext context)
    {
        if (context.Request is null ||
            context.Principal is null ||
            context.Request.GrantType != OpenIddictConstants.GrantTypes.RefreshToken ||
            string.IsNullOrEmpty(context.Request.RefreshToken))
        {
            return;
        }

        var userId = context.Principal.GetClaim(OpenIddictConstants.Claims.Subject);
        var sessionIdClaim = context.Principal.GetClaim(AuthConstants.Claims.SessionId);

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

public sealed class RefreshTokenHashStore : IOpenIddictServerHandler<ApplyTokenResponseContext>
{
    private readonly ISessionService _sessionService;
    private readonly IClientInfoService _clientInfoService;

    public RefreshTokenHashStore(ISessionService sessionService, IClientInfoService clientInfoService)
    {
        _sessionService = sessionService;
        _clientInfoService = clientInfoService;
    }

    public async ValueTask HandleAsync(ApplyTokenResponseContext context)
    {
        if (context.Principal is null || string.IsNullOrEmpty(context.Response?.RefreshToken))
        {
            return;
        }

        var userId = context.Principal.GetClaim(OpenIddictConstants.Claims.Subject);
        var sessionIdClaim = context.Principal.GetClaim(AuthConstants.Claims.SessionId);
        if (string.IsNullOrEmpty(userId) ||
            string.IsNullOrEmpty(sessionIdClaim) ||
            !Guid.TryParse(sessionIdClaim, out var sessionId))
        {
            return;
        }

        var newHash = RefreshTokenHash.ComputeHash(context.Response.RefreshToken);
        var refreshExpiry = DateTime.UtcNow.AddDays(7);

        if (context.Request?.GrantType == OpenIddictConstants.GrantTypes.RefreshToken)
        {
            var currentHash = RefreshTokenHash.ComputeHash(context.Request.RefreshToken);
            var updated = await _sessionService.UpdateSessionAsync(userId, sessionId, currentHash, newHash, refreshExpiry);
            if (!updated)
            {
                context.Reject(OpenIddictConstants.Errors.InvalidGrant, "The refresh token is no longer valid.");
                return;
            }
        }
        else
        {
            var clientInfo = await _clientInfoService.GetClientInfoAsync();
            await _sessionService.StoreSessionAsync(userId, sessionId, newHash, clientInfo, refreshExpiry);
        }
    }
}
