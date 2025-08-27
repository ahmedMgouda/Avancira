using Avancira.Application.Common;
using Avancira.Application.Identity;
using System.IdentityModel.Tokens.Jwt;
using Avancira.Application.Identity.Tokens;
using Avancira.Application.Identity.Tokens.Dtos;
using FluentValidation;
using System;
using System.Linq;
using Microsoft.Extensions.Options;

namespace Avancira.Infrastructure.Auth;

public class AuthenticationService : IAuthenticationService
{
    private readonly IClientInfoService _clientInfoService;
    private readonly ITokenEndpointClient _tokenClient;
    private readonly ISessionService _sessionService;
    private readonly IValidator<TokenRequestParams> _validator;
    private readonly TokenHashingOptions _options;

    public AuthenticationService(
        IClientInfoService clientInfoService,
        ITokenEndpointClient tokenClient,
        ISessionService sessionService,
        IValidator<TokenRequestParams> validator,
        IOptions<TokenHashingOptions> options)
    {
        _clientInfoService = clientInfoService;
        _tokenClient = tokenClient;
        _sessionService = sessionService;
        _validator = validator;
        _options = options.Value;
    }

    public async Task<TokenPair> ExchangeCodeAsync(string code, string codeVerifier, string redirectUri)
    {
        var request = new TokenRequestParams
        {
            GrantType = AuthConstants.GrantTypes.AuthorizationCode,
            Code = code,
            RedirectUri = redirectUri,
            CodeVerifier = codeVerifier
        };

        await _validator.ValidateAndThrowAsync(request);

        return await RequestTokenAsync(request, async (pair, clientInfo) =>
        {
            var userId = GetUserId(pair.Token);
            var sessionId = GetSessionId(pair.Token);
            await _sessionService.StoreSessionAsync(userId, sessionId, clientInfo, pair.RefreshToken, pair.RefreshTokenExpiryTime);
        });
    }

    public async Task<TokenPair> GenerateTokenAsync(string userId)
    {
        var request = new TokenRequestParams
        {
            GrantType = AuthConstants.GrantTypes.UserId,
            UserId = userId,
            Scope = "api offline_access"
        };

        await _validator.ValidateAndThrowAsync(request);

        return await RequestTokenAsync(request, (pair, clientInfo) =>
        {
            var sessionId = GetSessionId(pair.Token);
            return _sessionService.StoreSessionAsync(userId, sessionId, clientInfo, pair.RefreshToken, pair.RefreshTokenExpiryTime);
        });
    }

    public async Task<TokenPair> RefreshTokenAsync(string refreshToken)
    {
        var request = new TokenRequestParams
        {
            GrantType = AuthConstants.GrantTypes.RefreshToken,
            RefreshToken = refreshToken
        };

        await _validator.ValidateAndThrowAsync(request);

        return await RequestTokenAsync(request, async (pair, _) =>
        {
            var oldRefreshHash = TokenUtilities.HashToken(refreshToken, _options.Secret);
            var info = await _sessionService.GetRefreshTokenInfoAsync(oldRefreshHash);
            if (info != null)
            {
                var newRefreshHash = TokenUtilities.HashToken(pair.RefreshToken, _options.Secret);
                await _sessionService.RotateRefreshTokenAsync(info.Value.RefreshTokenId, newRefreshHash, pair.RefreshTokenExpiryTime);
            }
        });
    }

    private async Task<TokenPair> RequestTokenAsync(
        TokenRequestParams parameters,
        Func<TokenPair, ClientInfo, Task>? postProcess = null)
    {
        var clientInfo = await _clientInfoService.GetClientInfoAsync();
        var updated = parameters with { DeviceId = clientInfo.DeviceId };

        var pair = await _tokenClient.RequestTokenAsync(updated);
        if (postProcess != null)
        {
            await postProcess(pair, clientInfo);
        }

        return pair;
    }

    private static string GetUserId(string token)
    {
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);
        return jwt.Subject ?? string.Empty;
    }

    private static Guid GetSessionId(string token)
    {
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);
        var sid = jwt.Claims.FirstOrDefault(c => c.Type == AuthConstants.Claims.SessionId)?.Value;
        return Guid.TryParse(sid, out var id) ? id : Guid.Empty;
    }
}
