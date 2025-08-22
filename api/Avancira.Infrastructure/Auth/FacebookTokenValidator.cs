using System.Collections.Generic;
using System.Security.Claims;
using System.Text.Json;
using Avancira.Application.Auth;
using Avancira.Application.Options;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Avancira.Infrastructure.Auth;

public class FacebookTokenValidator : IExternalTokenValidator
{
    private readonly IFacebookClient _facebookClient;
    private readonly FacebookOptions _options;
    private readonly ILogger<FacebookTokenValidator> _logger;

    public string Provider => "facebook";

    public FacebookTokenValidator(
        IFacebookClient facebookClient,
        IOptions<FacebookOptions> facebookOptions,
        ILogger<FacebookTokenValidator> logger)
    {
        _facebookClient = facebookClient;
        _options = facebookOptions.Value;
        _logger = logger;
    }

    public async Task<ExternalAuthResult> ValidateAsync(string accessToken)
    {
        try
        {
            var appToken = $"{_options.AppId}|{_options.AppSecret}";
            var debugParams = new Dictionary<string, object>
            {
                ["input_token"] = accessToken,
                ["access_token"] = appToken
            };
            using var debugDoc = await _facebookClient.GetAsync("debug_token", debugParams);
            var data = debugDoc.RootElement.GetProperty("data");
            var appId = data.GetProperty("app_id").GetString();
            var isValid = data.GetProperty("is_valid").GetBoolean();
            var expiresAt = data.GetProperty("expires_at").GetInt64();
            if (appId != _options.AppId || !isValid || DateTimeOffset.FromUnixTimeSeconds(expiresAt) <= DateTimeOffset.UtcNow)
            {
                _logger.LogWarning("Facebook token invalid: app_id={AppId} is_valid={IsValid} exp={Exp}", appId, isValid, expiresAt);
                return ExternalAuthResult.Fail("Invalid Facebook token");
            }

            var profileParams = new Dictionary<string, object>
            {
                ["fields"] = "id,name,email",
                ["access_token"] = accessToken
            };
            using var profileDoc = await _facebookClient.GetAsync("me", profileParams);
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
