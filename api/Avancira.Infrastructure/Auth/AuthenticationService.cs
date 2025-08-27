using Avancira.Application.Common;
using Avancira.Application.Identity;
using System.IdentityModel.Tokens.Jwt;
using Avancira.Application.Identity.Tokens;
using Avancira.Application.Identity.Tokens.Dtos;

namespace Avancira.Infrastructure.Auth;

public class AuthenticationService : IAuthenticationService
{
    private readonly IClientInfoService _clientInfoService;
    private readonly ITokenEndpointClient _tokenClient;
    private readonly ISessionService _sessionService;

    public AuthenticationService(
        IClientInfoService clientInfoService,
        ITokenEndpointClient tokenClient,
        ISessionService sessionService)
    {
        _clientInfoService = clientInfoService;
        _tokenClient = tokenClient;
        _sessionService = sessionService;
    }

    public async Task<TokenPair> ExchangeCodeAsync(string code, string codeVerifier, string redirectUri)
    {
        var builder = TokenRequestBuilder.BuildAuthorizationCodeRequest(code, codeVerifier, redirectUri);
        var clientInfo = await _clientInfoService.GetClientInfoAsync();
        builder.WithDeviceId(clientInfo.DeviceId);

        return await RequestTokenAsync(builder, async pair =>
        {
            var userId = GetUserId(pair.Token);
            await _sessionService.StoreSessionAsync(userId, clientInfo, pair.RefreshToken, pair.RefreshTokenExpiryTime);
        });
    }

    public async Task<TokenPair> GenerateTokenAsync(string userId)
    {
        var builder = TokenRequestBuilder.BuildUserIdGrantRequest(userId);
        var clientInfo = await _clientInfoService.GetClientInfoAsync();
        builder.WithDeviceId(clientInfo.DeviceId);

        return await RequestTokenAsync(builder, pair =>
            _sessionService.StoreSessionAsync(userId, clientInfo, pair.RefreshToken, pair.RefreshTokenExpiryTime));
    }

    public async Task<TokenPair> RefreshTokenAsync(string refreshToken)
    {
        var builder = TokenRequestBuilder.BuildRefreshTokenRequest(refreshToken);

        return await RequestTokenAsync(builder, async pair =>
        {
            var oldRefreshHash = TokenUtilities.HashToken(refreshToken);
            var info = await _sessionService.GetRefreshTokenInfoAsync(oldRefreshHash);
            if (info != null)
            {
                var newRefreshHash = TokenUtilities.HashToken(pair.RefreshToken);
                await _sessionService.RotateRefreshTokenAsync(info.Value.RefreshTokenId, newRefreshHash, pair.RefreshTokenExpiryTime);
            }
        });
    }

    private async Task<TokenPair> RequestTokenAsync(TokenRequestBuilder builder, Func<TokenPair, Task>? postRequest = null)
    {
        var pair = await _tokenClient.RequestTokenAsync(builder);
        if (postRequest != null)
        {
            await postRequest(pair);
        }

        return pair;
    }

    private static string GetUserId(string token)
    {
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);
        return jwt.Subject ?? string.Empty;
    }
}

