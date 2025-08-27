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

        var token = root.GetProperty("access_token").GetString() ?? string.Empty;
        var refresh = root.GetProperty("refresh_token").GetString() ?? string.Empty;
        DateTime refreshExpiry = DateTime.UtcNow;
        if (root.TryGetProperty("refresh_token_expires_in", out var exp))
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

