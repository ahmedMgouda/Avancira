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

    private const string TokenEndpoint = "/connect/token";
    private const string GrantType = "grant_type";
    private const string Code = "code";
    private const string RedirectUri = "redirect_uri";
    private const string CodeVerifier = "code_verifier";
    private const string DeviceId = "device_id";
    private const string UserId = "user_id";
    private const string Scope = "scope";
    private const string RefreshToken = "refresh_token";
    private const string AuthorizationCodeGrant = "authorization_code";
    private const string UserIdGrant = "user_id";
    private const string RefreshTokenGrant = "refresh_token";

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
            [GrantType] = AuthorizationCodeGrant,
            [Code] = code,
            [RedirectUri] = redirectUri,
            [CodeVerifier] = codeVerifier
        });

        return await HandleTokenResponseAsync(response, clientInfo, string.Empty);
    }

    public async Task<TokenPair> GenerateTokenAsync(string userId)
    {
        var (response, clientInfo) = await RequestTokenAsync(new Dictionary<string, string?>
        {
            [GrantType] = UserIdGrant,
            [UserId] = userId,
            [Scope] = "api offline_access"
        });

        return await HandleTokenResponseAsync(response, clientInfo, userId);
    }

    public async Task<TokenPair> RefreshTokenAsync(string refreshToken)
    {
        var (response, _) = await RequestTokenAsync(new Dictionary<string, string?>
        {
            [GrantType] = RefreshTokenGrant,
            [RefreshToken] = refreshToken
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
        values[DeviceId] = clientInfo.DeviceId;

        var content = BuildTokenRequest(values);
        var response = await _httpClient.PostAsync(TokenEndpoint, content);
        response.EnsureSuccessStatusCode();

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

        var token = root.GetProperty("access_token").GetString() ?? string.Empty;
        var refresh = root.GetProperty(RefreshToken).GetString() ?? string.Empty;
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

