using System.Security.Claims;
using System.Text.Json;
using Avancira.Application.Auth;
using Avancira.Application.Options;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Avancira.Infrastructure.Auth;

public class ExternalAuthService : IExternalAuthService
{
    private readonly HttpClient _httpClient;
    private readonly GoogleOptions _googleOptions;

    public ExternalAuthService(IHttpClientFactory httpClientFactory, IOptions<GoogleOptions> googleOptions)
    {
        _httpClient = httpClientFactory.CreateClient();
        _googleOptions = googleOptions.Value;
    }

    public async Task<ExternalLoginInfo?> ValidateGoogleTokenAsync(string idToken)
    {
        var response = await _httpClient.GetAsync($"https://oauth2.googleapis.com/tokeninfo?id_token={idToken}");
        if (!response.IsSuccessStatusCode) return null;

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = doc.RootElement;
        if (root.GetProperty("aud").GetString() != _googleOptions.ClientId) return null;

        var email = root.GetProperty("email").GetString() ?? string.Empty;
        var sub = root.GetProperty("sub").GetString() ?? string.Empty;
        var name = root.TryGetProperty("name", out var nameEl) ? nameEl.GetString() : string.Empty;

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Name, name ?? string.Empty)
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "google"));
        return new ExternalLoginInfo(principal, "Google", sub, "Google");
    }

    public async Task<ExternalLoginInfo?> ValidateFacebookTokenAsync(string accessToken)
    {
        var response = await _httpClient.GetAsync($"https://graph.facebook.com/me?fields=id,name,email&access_token={accessToken}");
        if (!response.IsSuccessStatusCode) return null;

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = doc.RootElement;
        var id = root.GetProperty("id").GetString();
        if (string.IsNullOrEmpty(id)) return null;
        var email = root.TryGetProperty("email", out var emailEl) ? emailEl.GetString() : string.Empty;
        var name = root.TryGetProperty("name", out var nameEl) ? nameEl.GetString() : string.Empty;

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Email, email ?? string.Empty),
            new Claim(ClaimTypes.Name, name ?? string.Empty)
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "facebook"));
        return new ExternalLoginInfo(principal, "Facebook", id!, "Facebook");
    }
}
