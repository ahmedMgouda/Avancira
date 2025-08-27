using System.Net.Http;
using System.Text.Json;
using System.Collections.Generic;
using Avancira.Application.Common;
using Avancira.Application.Identity;
using System.IdentityModel.Tokens.Jwt;
using Avancira.Application.Auth.Jwt;
using Microsoft.Extensions.Options;
using Avancira.Application.Identity.Tokens;
using Avancira.Application.Identity.Tokens.Dtos;
using System.Net;
using Avancira.Domain.Common.Exceptions;

namespace Avancira.Infrastructure.Auth;

public class AuthenticationService : IAuthenticationService
{
    private readonly HttpClient _httpClient;
    private readonly IClientInfoService _clientInfoService;
    private readonly JwtOptions _jwtOptions;
    private readonly ISessionService _sessionService;

    public AuthenticationService(
        IHttpClientFactory httpClientFactory,
        IClientInfoService clientInfoService,
        IOptions<JwtOptions> jwtOptions,
        ISessionService sessionService)
    {
        _httpClient = httpClientFactory.CreateClient();
        _clientInfoService = clientInfoService;
        _jwtOptions = jwtOptions.Value;
        _sessionService = sessionService;
    }

    public async Task<TokenPair> ExchangeCodeAsync(string code, string codeVerifier, string redirectUri)
    {
        var (response, clientInfo) = await RequestTokenAsync(new Dictionary<string, string?>
        {
            [AuthConstants.Parameters.GrantType] = AuthConstants.GrantTypes.AuthorizationCode,
            [AuthConstants.Parameters.Code] = code,
            [AuthConstants.Parameters.RedirectUri] = redirectUri,
            [AuthConstants.Parameters.CodeVerifier] = codeVerifier
        });

        return await HandleTokenResponseAsync(response, clientInfo, string.Empty);
    }

    public async Task<TokenPair> GenerateTokenAsync(string userId)
    {
        var (response, clientInfo) = await RequestTokenAsync(new Dictionary<string, string?>
        {
            [AuthConstants.Parameters.GrantType] = AuthConstants.GrantTypes.UserId,
            [AuthConstants.Parameters.UserId] = userId,
            [AuthConstants.Parameters.Scope] = "api offline_access"
        });

        return await HandleTokenResponseAsync(response, clientInfo, userId);
    }

    public async Task<TokenPair> RefreshTokenAsync(string refreshToken)
    {
        var (response, _) = await RequestTokenAsync(new Dictionary<string, string?>
        {
            [AuthConstants.Parameters.GrantType] = AuthConstants.GrantTypes.RefreshToken,
            [AuthConstants.Parameters.RefreshToken] = refreshToken
        });

        var (token, newRefresh, refreshExpiry) = await ParseTokenResponseAsync(response);

        var oldRefreshHash = TokenUtilities.HashToken(refreshToken);
        var info = await _sessionService.GetRefreshTokenInfoAsync(oldRefreshHash);
        if (info != null)
        {
            var newRefreshHash = TokenUtilities.HashToken(newRefresh);
            await _sessionService.RotateRefreshTokenAsync(info.Value.RefreshTokenId, newRefreshHash, refreshExpiry);
        }

        return new TokenPair(token, newRefresh, refreshExpiry);
    }

    private async Task<(HttpResponseMessage Response, ClientInfo ClientInfo)> RequestTokenAsync(Dictionary<string, string?> values)
    {
        var clientInfo = await _clientInfoService.GetClientInfoAsync();
        values[AuthConstants.Parameters.DeviceId] = clientInfo.DeviceId;

        var content = BuildTokenRequest(values);
        var response = await _httpClient.PostAsync(AuthConstants.Endpoints.Token, content);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new UnauthorizedException();
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new TokenRequestException(response.StatusCode);
        }

        return (response, clientInfo);
    }

    private async Task<TokenPair> HandleTokenResponseAsync(HttpResponseMessage response, ClientInfo clientInfo, string userId)
    {
        var (token, refresh, refreshExpiry) = await ParseTokenResponseAsync(response);

        if (string.IsNullOrEmpty(userId))
        {
            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(token);
            userId = jwt.Subject;
        }

        await _sessionService.StoreSessionAsync(userId, clientInfo, refresh, refreshExpiry);

        return new TokenPair(token, refresh, refreshExpiry);
    }

    private static FormUrlEncodedContent BuildTokenRequest(Dictionary<string, string?> values) =>
        new(values);

    private async Task<(string Token, string RefreshToken, DateTime RefreshExpiry)> ParseTokenResponseAsync(HttpResponseMessage response)
    {
        using var stream = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(stream);
        var root = document.RootElement;

        var token = root.GetProperty(AuthConstants.Parameters.AccessToken).GetString() ?? string.Empty;
        var refresh = root.GetProperty(AuthConstants.Parameters.RefreshToken).GetString() ?? string.Empty;
        DateTime refreshExpiry = DateTime.UtcNow;
        if (root.TryGetProperty(AuthConstants.Parameters.RefreshTokenExpiresIn, out var exp))
        {
            refreshExpiry = DateTime.UtcNow.AddSeconds(exp.GetInt32());
        }
        else
        {
            refreshExpiry = DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenDefaultDays);
        }

        return (token, refresh, refreshExpiry);
    }

}

