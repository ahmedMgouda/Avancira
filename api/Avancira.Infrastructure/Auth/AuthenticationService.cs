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
        var clientInfo = await _clientInfoService.GetClientInfoAsync();

        var content = BuildTokenRequest(new Dictionary<string, string?>
        {
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["redirect_uri"] = redirectUri,
            ["code_verifier"] = codeVerifier,
            ["device_id"] = clientInfo.DeviceId
        });

        var response = await _httpClient.PostAsync("/connect/token", content);
        response.EnsureSuccessStatusCode();

        return await HandleTokenResponseAsync(response, clientInfo, string.Empty);
    }

    public async Task<TokenPair> GenerateTokenAsync(string userId)
    {
        var clientInfo = await _clientInfoService.GetClientInfoAsync();

        var content = BuildTokenRequest(new Dictionary<string, string?>
        {
            ["grant_type"] = "user_id",
            ["user_id"] = userId,
            ["scope"] = "api offline_access",
            ["device_id"] = clientInfo.DeviceId
        });

        var response = await _httpClient.PostAsync("/connect/token", content);
        response.EnsureSuccessStatusCode();

        return await HandleTokenResponseAsync(response, clientInfo, userId);
    }

    public async Task<TokenPair> RefreshTokenAsync(string refreshToken)
    {
        var clientInfo = await _clientInfoService.GetClientInfoAsync();

        var content = BuildTokenRequest(new Dictionary<string, string?>
        {
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = refreshToken,
            ["device_id"] = clientInfo.DeviceId
        });

        var response = await _httpClient.PostAsync("/connect/token", content);
        response.EnsureSuccessStatusCode();

        var tokenResponse = await ParseTokenResponseAsync(response);

        var oldRefreshHash = TokenUtilities.HashToken(refreshToken);
        var info = await _sessionService.GetRefreshTokenInfoAsync(oldRefreshHash);
        if (info != null)
        {
            var newRefreshHash = TokenUtilities.HashToken(tokenResponse.RefreshToken);
            await _sessionService.RotateRefreshTokenAsync(info.Value.RefreshTokenId, newRefreshHash, tokenResponse.RefreshExpiry);
        }

        return new TokenPair(tokenResponse.Token, tokenResponse.RefreshToken, tokenResponse.RefreshExpiry);
    }

    private async Task<TokenPair> HandleTokenResponseAsync(HttpResponseMessage response, ClientInfo clientInfo, string userId)
    {
        var tokenResponse = await ParseTokenResponseAsync(response);

        if (string.IsNullOrEmpty(userId))
        {
            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(tokenResponse.Token);
            userId = jwt.Subject;
        }

        await _sessionService.StoreSessionAsync(userId, clientInfo, tokenResponse.RefreshToken, tokenResponse.RefreshExpiry);

        return new TokenPair(tokenResponse.Token, tokenResponse.RefreshToken, tokenResponse.RefreshExpiry);
    }

    private static FormUrlEncodedContent BuildTokenRequest(Dictionary<string, string?> values) =>
        new(values);

    private async Task<TokenResponse> ParseTokenResponseAsync(HttpResponseMessage response)
    {
        await using var stream = await response.Content.ReadAsStreamAsync();
        var tokenResponse = await JsonSerializer.DeserializeAsync<TokenResponse>(stream) ?? new TokenResponse();

        var expirySeconds = tokenResponse.RefreshTokenExpiresIn;
        var refreshExpiry = expirySeconds.HasValue
            ? DateTime.UtcNow.AddSeconds(expirySeconds.Value)
            : DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenDefaultDays);

        return tokenResponse with { RefreshExpiry = refreshExpiry };
    }

}

