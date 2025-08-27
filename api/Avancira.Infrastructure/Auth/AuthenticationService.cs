using System.Net.Http;
using System.Text.Json;
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
        _httpClient = httpClientFactory.CreateClient("TokenClient");
        _clientInfoService = clientInfoService;
        _jwtOptions = jwtOptions.Value;
        _sessionService = sessionService;
    }

    public async Task<TokenPair> ExchangeCodeAsync(string code, string codeVerifier, string redirectUri)
    {
        var builder = TokenRequestBuilder.BuildAuthorizationCodeRequest(code, codeVerifier, redirectUri);
        var (document, clientInfo) = await RequestTokenAsync(builder);
        using (document)
        {
            return await HandleTokenResponseAsync(document, clientInfo, string.Empty);
        }
    }

    public async Task<TokenPair> GenerateTokenAsync(string userId)
    {
        var builder = TokenRequestBuilder.BuildUserIdGrantRequest(userId);
        var (document, clientInfo) = await RequestTokenAsync(builder);
        using (document)
        {
            return await HandleTokenResponseAsync(document, clientInfo, userId);
        }
    }

    public async Task<TokenPair> RefreshTokenAsync(string refreshToken)
    {
        var builder = TokenRequestBuilder.BuildRefreshTokenRequest(refreshToken);
        var (document, _) = await RequestTokenAsync(builder);
        using (document)
        {
            var (token, newRefresh, refreshExpiry) = ParseTokenResponse(document);

            var oldRefreshHash = TokenUtilities.HashToken(refreshToken);
            var info = await _sessionService.GetRefreshTokenInfoAsync(oldRefreshHash);
            if (info != null)
            {
                var newRefreshHash = TokenUtilities.HashToken(newRefresh);
                await _sessionService.RotateRefreshTokenAsync(info.Value.RefreshTokenId, newRefreshHash, refreshExpiry);
            }

            return new TokenPair(token, newRefresh, refreshExpiry);
        }
    }

    private async Task<(JsonDocument Document, ClientInfo ClientInfo)> RequestTokenAsync(TokenRequestBuilder builder)
    {
        var clientInfo = await _clientInfoService.GetClientInfoAsync();
        builder.WithDeviceId(clientInfo.DeviceId);

        var content = builder.Build();
        using var response = await _httpClient.PostAsync(AuthConstants.Endpoints.Token, content);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new UnauthorizedException();
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new TokenRequestException(response.StatusCode);
        }

        await using var stream = await response.Content.ReadAsStreamAsync();
        var document = await JsonDocument.ParseAsync(stream);

        return (document, clientInfo);
    }

    private async Task<TokenPair> HandleTokenResponseAsync(JsonDocument document, ClientInfo clientInfo, string userId)
    {
        var (token, refresh, refreshExpiry) = ParseTokenResponse(document);

        if (string.IsNullOrEmpty(userId))
        {
            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(token);
            userId = jwt.Subject;
        }

        await _sessionService.StoreSessionAsync(userId, clientInfo, refresh, refreshExpiry);

        return new TokenPair(token, refresh, refreshExpiry);
    }

    private (string Token, string RefreshToken, DateTime RefreshExpiry) ParseTokenResponse(JsonDocument document)
    {
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

