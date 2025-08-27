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
        var request = new TokenRequestParams
        {
            GrantType = AuthConstants.GrantTypes.AuthorizationCode,
            Code = code,
            RedirectUri = redirectUri,
            CodeVerifier = codeVerifier
        };
        var (parameters, clientInfo) = await BuildRequestWithClientInfo(request);

        return await RequestTokenAsync(parameters, async pair =>
        {
            var userId = GetUserId(pair.Token);
            await _sessionService.StoreSessionAsync(userId, clientInfo, pair.RefreshToken, pair.RefreshTokenExpiryTime);
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
        var (parameters, clientInfo) = await BuildRequestWithClientInfo(request);

        return await RequestTokenAsync(parameters, pair =>
            _sessionService.StoreSessionAsync(userId, clientInfo, pair.RefreshToken, pair.RefreshTokenExpiryTime));
    }

    public async Task<TokenPair> RefreshTokenAsync(string refreshToken)
    {
        var request = new TokenRequestParams
        {
            GrantType = AuthConstants.GrantTypes.RefreshToken,
            RefreshToken = refreshToken
        };
        var (parameters, _) = await BuildRequestWithClientInfo(request);

        return await RequestTokenAsync(parameters, async pair =>
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

    private async Task<TokenPair> RequestTokenAsync(TokenRequestParams parameters, Func<TokenPair, Task>? postRequest = null)
    {
        var pair = await _tokenClient.RequestTokenAsync(parameters);
        if (postRequest != null)
        {
            await postRequest(pair);
        }

        return pair;
    }

    private async Task<(TokenRequestParams Parameters, ClientInfo ClientInfo)> BuildRequestWithClientInfo(TokenRequestParams parameters)
    {
        var clientInfo = await _clientInfoService.GetClientInfoAsync();
        var updated = parameters with { DeviceId = clientInfo.DeviceId };
        return (updated, clientInfo);
    }

    private static string GetUserId(string token)
    {
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);
        return jwt.Subject ?? string.Empty;
    }
}
