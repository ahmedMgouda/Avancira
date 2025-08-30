using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Blazored.LocalStorage;
using Avancira.Admin.Infrastructure.Api;
using Avancira.Admin.Infrastructure.Storage;
using Avancira.Shared.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;

namespace Avancira.Admin.Infrastructure.Auth.Jwt;

public sealed class JwtAuthenticationService : AuthenticationStateProvider, IAuthenticationService, IAccessTokenProvider
{
    private const string CodeVerifierKey = "pkce-code-verifier";
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly IApiClient _client;
    private readonly ILocalStorageService _localStorage;
    private readonly NavigationManager _navigation;
    private readonly HttpClient _http;

    public JwtAuthenticationService(PersistentComponentState state,
        ILocalStorageService localStorage,
        IApiClient client,
        NavigationManager navigation,
        HttpClient http)
    {
        _localStorage = localStorage;
        _client = client;
        _navigation = navigation;
        _http = http;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        string? cachedToken = await GetCachedAuthTokenAsync();
        if (string.IsNullOrWhiteSpace(cachedToken))
        {
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }

        var claimsIdentity = new ClaimsIdentity(GetClaimsFromJwt(cachedToken), "jwt");

        if (await GetCachedPermissionsAsync() is List<string> cachedPermissions)
        {
            claimsIdentity.AddClaims(cachedPermissions.Select(p => new Claim(AvanciraClaims.Permission, p)));
        }

        return new AuthenticationState(new ClaimsPrincipal(claimsIdentity));
    }

    public void NavigateToExternalLogin(string returnUrl)
    {
        var verifier = GenerateCodeVerifier();
        var challenge = CreateCodeChallenge(verifier);
        _localStorage.SetItemAsync(CodeVerifierKey, verifier).GetAwaiter().GetResult();

        var redirectUri = $"{_navigation.BaseUri}auth/callback";
        var authorizeUrl =
            $"{_http.BaseAddress}connect/authorize?client_id=mobile&response_type=code&scope=offline_access&redirect_uri={Uri.EscapeDataString(redirectUri)}&code_challenge={challenge}&code_challenge_method=S256&state={Uri.EscapeDataString(returnUrl)}";

        _navigation.NavigateTo(authorizeUrl, true);
    }

    public async Task<bool> CompleteLoginAsync(string code, string state)
    {
        var verifier = await _localStorage.GetItemAsync<string?>(CodeVerifierKey);
        if (string.IsNullOrEmpty(verifier))
        {
            return false;
        }

        // Ensure the verifier cannot be reused after the login completes
        await _localStorage.RemoveItemAsync(CodeVerifierKey);

        var redirectUri = $"{_navigation.BaseUri}auth/callback";
        var response = await _http.PostAsync("connect/token",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = "mobile",
                ["grant_type"] = "authorization_code",
                ["code"] = code,
                ["redirect_uri"] = redirectUri,
                ["code_verifier"] = verifier
            }));

        if (!response.IsSuccessStatusCode)
        {
            return false;
        }

        var payload = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(payload);
        var token = doc.RootElement.GetProperty("access_token").GetString();
        var refresh = doc.RootElement.TryGetProperty("refresh_token", out var rt) ? rt.GetString() : null;

        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        await CacheAuthToken(token);
        if (!string.IsNullOrEmpty(refresh))
        {
            await _localStorage.SetItemAsync(StorageConstants.Local.RefreshToken, refresh);
        }

        var permissions = await _client.GetUserPermissionsAsync();
        await CachePermissions(permissions);

        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        return true;
    }

    public async Task LogoutAsync()
    {
        await ClearCacheAsync();
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        _navigation.NavigateTo("/login");
    }

    public async Task ReLoginAsync(string returnUrl)
    {
        await LogoutAsync();
        NavigateToExternalLogin(returnUrl);
    }

    public async ValueTask<AccessTokenResult> RequestAccessToken(AccessTokenRequestOptions options) =>
        await RequestAccessToken();

    public async ValueTask<AccessTokenResult> RequestAccessToken()
    {
        await _semaphore.WaitAsync();
        try
        {
            var authState = await GetAuthenticationStateAsync();
            if (authState.User.Identity?.IsAuthenticated is not true)
            {
                return new AccessTokenResult(AccessTokenResultStatus.RequiresRedirect, new(), "/login", default);
            }

            string? token = await GetCachedAuthTokenAsync();
            var expTime = authState.User.GetExpiration();
            if (expTime - DateTime.UtcNow <= TimeSpan.FromMinutes(1))
            {
                (bool succeeded, string? newToken) = await TryRefreshTokenAsync();
                if (!succeeded)
                {
                    return new AccessTokenResult(AccessTokenResultStatus.RequiresRedirect, new(), "/login", default);
                }
                token = newToken;
            }

            return new AccessTokenResult(AccessTokenResultStatus.Success, new AccessToken { Value = token! }, string.Empty, default);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task<(bool Succeeded, string? Token)> TryRefreshTokenAsync()
    {
        var refreshToken = await _localStorage.GetItemAsync<string?>(StorageConstants.Local.RefreshToken);
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return (false, null);
        }

        var response = await _http.PostAsync("connect/token",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = "mobile",
                ["grant_type"] = "refresh_token",
                ["refresh_token"] = refreshToken
            }));

        if (!response.IsSuccessStatusCode)
        {
            return (false, null);
        }

        var payload = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(payload);
        var token = doc.RootElement.GetProperty("access_token").GetString();
        var newRefresh = doc.RootElement.TryGetProperty("refresh_token", out var rt) ? rt.GetString() : null;

        if (string.IsNullOrEmpty(token))
        {
            return (false, null);
        }

        await CacheAuthToken(token);
        if (!string.IsNullOrEmpty(newRefresh))
        {
            await _localStorage.SetItemAsync(StorageConstants.Local.RefreshToken, newRefresh);
        }

        return (true, token);
    }

    private async ValueTask CacheAuthToken(string? token) =>
        await _localStorage.SetItemAsync(StorageConstants.Local.AuthToken, token);

    private ValueTask CachePermissions(ICollection<string> permissions) =>
        _localStorage.SetItemAsync(StorageConstants.Local.Permissions, permissions);

    private async Task ClearCacheAsync()
    {
        await _localStorage.RemoveItemAsync(StorageConstants.Local.AuthToken);
        await _localStorage.RemoveItemAsync(StorageConstants.Local.RefreshToken);
        await _localStorage.RemoveItemAsync(StorageConstants.Local.Permissions);
    }

    private ValueTask<string?> GetCachedAuthTokenAsync() =>
        _localStorage.GetItemAsync<string?>(StorageConstants.Local.AuthToken);

    private ValueTask<ICollection<string>?> GetCachedPermissionsAsync() =>
        _localStorage.GetItemAsync<ICollection<string>>(StorageConstants.Local.Permissions);

    private static IEnumerable<Claim> GetClaimsFromJwt(string jwt)
    {
        var claims = new List<Claim>();
        string payload = jwt.Split('.')[1];
        byte[] jsonBytes = ParseBase64WithoutPadding(payload);
        var keyValuePairs = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonBytes);

        if (keyValuePairs is not null)
        {
            keyValuePairs.TryGetValue(ClaimTypes.Role, out object? roles);

            if (roles is not null)
            {
                string? rolesString = roles.ToString();
                if (!string.IsNullOrEmpty(rolesString))
                {
                    if (rolesString.Trim().StartsWith("["))
                    {
                        string[]? parsedRoles = JsonSerializer.Deserialize<string[]>(rolesString);
                        if (parsedRoles is not null)
                        {
                            claims.AddRange(parsedRoles.Select(role => new Claim(ClaimTypes.Role, role)));
                        }
                    }
                    else
                    {
                        claims.Add(new Claim(ClaimTypes.Role, rolesString));
                    }
                }
            }

            foreach (var kvp in keyValuePairs)
            {
                if (kvp.Key != ClaimTypes.Role && kvp.Value is not null)
                {
                    claims.Add(new Claim(kvp.Key, kvp.Value.ToString()!));
                }
            }
        }

        return claims;
    }

    private static byte[] ParseBase64WithoutPadding(string base64)
    {
        switch (base64.Length % 4)
        {
            case 2: base64 += "=="; break;
            case 3: base64 += "="; break;
        }
        return Convert.FromBase64String(base64);
    }

    private static string GenerateCodeVerifier()
    {
        var bytes = new byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Base64UrlEncode(bytes);
    }

    private static string CreateCodeChallenge(string verifier)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(verifier));
        return Base64UrlEncode(bytes);
    }

    private static string Base64UrlEncode(byte[] arg) =>
        Convert.ToBase64String(arg).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
