using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using Avancira.Application.Auth;
using Avancira.Application.Options;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Avancira.Infrastructure.Auth;

public class ExternalAuthService : IExternalAuthService
{
    private readonly HttpClient _httpClient;
    private readonly GoogleOptions _googleOptions;
    private readonly FacebookOptions _facebookOptions;
    private readonly ILogger<ExternalAuthService> _logger;

    public ExternalAuthService(
        IHttpClientFactory httpClientFactory,
        IOptions<GoogleOptions> googleOptions,
        IOptions<FacebookOptions> facebookOptions,
        ILogger<ExternalAuthService> logger)
    {
        _httpClient = httpClientFactory.CreateClient();
        _googleOptions = googleOptions.Value;
        _facebookOptions = facebookOptions.Value;
        _logger = logger;
    }

    public Task<ExternalAuthResult> ValidateGoogleTokenAsync(string idToken)
    {
        var handler = new JwtSecurityTokenHandler();
        var parameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuers = new[] { "accounts.google.com", "https://accounts.google.com" },
            ValidateAudience = true,
            ValidAudience = _googleOptions.ClientId,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(5),
            ValidateIssuerSigningKey = false
        };

        try
        {
            var principal = handler.ValidateToken(idToken, parameters, out _);
            var sub = principal.FindFirstValue("sub") ?? string.Empty;
            var email = principal.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
            var name = principal.FindFirstValue(ClaimTypes.Name) ?? string.Empty;

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.Name, name)
            };
            var identity = new ClaimsIdentity(claims, "google");
            var info = new ExternalLoginInfo(new ClaimsPrincipal(identity), "Google", sub, "Google");
            return Task.FromResult(ExternalAuthResult.Success(info));
        }
        catch (SecurityTokenException ex)
        {
            _logger.LogWarning(ex, "Google token validation failed");
            return Task.FromResult(ExternalAuthResult.Fail("Invalid Google token"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating Google token");
            return Task.FromResult(ExternalAuthResult.Fail("Error validating Google token"));
        }
    }

    public async Task<ExternalAuthResult> ValidateFacebookTokenAsync(string accessToken)
    {
        try
        {
            var appToken = $"{_facebookOptions.AppId}|{_facebookOptions.AppSecret}";
            var debugResponse = await _httpClient.GetAsync($"https://graph.facebook.com/debug_token?input_token={accessToken}&access_token={appToken}");
            if (!debugResponse.IsSuccessStatusCode)
            {
                _logger.LogWarning("Facebook debug_token request failed: {StatusCode}", debugResponse.StatusCode);
                return ExternalAuthResult.Fail("Invalid Facebook token");
            }

            using var debugDoc = JsonDocument.Parse(await debugResponse.Content.ReadAsStringAsync());
            var data = debugDoc.RootElement.GetProperty("data");
            var appId = data.GetProperty("app_id").GetString();
            var isValid = data.GetProperty("is_valid").GetBoolean();
            var expiresAt = data.GetProperty("expires_at").GetInt64();
            if (appId != _facebookOptions.AppId || !isValid || DateTimeOffset.FromUnixTimeSeconds(expiresAt) <= DateTimeOffset.UtcNow)
            {
                _logger.LogWarning("Facebook token invalid: app_id={AppId} is_valid={IsValid} exp={Exp}", appId, isValid, expiresAt);
                return ExternalAuthResult.Fail("Invalid Facebook token");
            }

            var profileResponse = await _httpClient.GetAsync($"https://graph.facebook.com/me?fields=id,name,email&access_token={accessToken}");
            if (!profileResponse.IsSuccessStatusCode)
            {
                _logger.LogWarning("Facebook profile request failed: {StatusCode}", profileResponse.StatusCode);
                return ExternalAuthResult.Fail("Unable to retrieve Facebook user info");
            }

            using var profileDoc = JsonDocument.Parse(await profileResponse.Content.ReadAsStringAsync());
            var root = profileDoc.RootElement;
            var id = root.GetProperty("id").GetString() ?? string.Empty;
            var email = root.TryGetProperty("email", out var emailEl) ? emailEl.GetString() ?? string.Empty : string.Empty;
            var name = root.TryGetProperty("name", out var nameEl) ? nameEl.GetString() ?? string.Empty : string.Empty;

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.Name, name)
            };
            var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "facebook"));
            var info = new ExternalLoginInfo(principal, "Facebook", id, "Facebook");
            return ExternalAuthResult.Success(info);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating Facebook token");
            return ExternalAuthResult.Fail("Error validating Facebook token");
        }
    }
}
