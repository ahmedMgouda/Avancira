using System.Net.Http;
using System.Text.Json;
using System.Collections.Generic;
using Avancira.Application.Common;
using Avancira.Application.Identity;
using Avancira.Application.Identity.Tokens.Dtos;

namespace Avancira.Infrastructure.Auth;

public class AuthenticationService : IAuthenticationService
{
    private readonly HttpClient _httpClient;
    private readonly IClientInfoService _clientInfoService;

    public AuthenticationService(IHttpClientFactory httpClientFactory, IClientInfoService clientInfoService)
    {
        _httpClient = httpClientFactory.CreateClient();
        _clientInfoService = clientInfoService;
    }

    public async Task<TokenPair> GenerateTokenAsync(TokenGenerationDto request)
    {
        _ = await _clientInfoService.GetClientInfoAsync();

        var content = new FormUrlEncodedContent(new Dictionary<string, string?>
        {
            ["grant_type"] = "password",
            ["username"] = request.Email,
            ["password"] = request.Password,
            ["scope"] = "api offline_access"
        });

        var response = await _httpClient.PostAsync("/connect/token", content);
        response.EnsureSuccessStatusCode();

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
            refreshExpiry = DateTime.UtcNow.AddDays(7);
        }

        return new TokenPair(token, refresh, refreshExpiry);
    }

    public async Task<TokenPair> RefreshTokenAsync(string refreshToken)
    {
        _ = await _clientInfoService.GetClientInfoAsync();

        var content = new FormUrlEncodedContent(new Dictionary<string, string?>
        {
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = refreshToken
        });

        var response = await _httpClient.PostAsync("/connect/token", content);
        response.EnsureSuccessStatusCode();

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
            refreshExpiry = DateTime.UtcNow.AddDays(7);
        }

        return new TokenPair(token, refresh, refreshExpiry);
    }
}

