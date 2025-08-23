using System.Collections.Generic;
using System.Security.Claims;
using System.Text.Json;
using System.Net.Http;
using Avancira.Application.Auth;
using Avancira.Application.Options;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Facebook;

namespace Avancira.Infrastructure.Auth;

public class FacebookTokenValidator : IExternalTokenValidator
{
    private readonly IFacebookClient _facebookClient;
    private readonly FacebookOptions _options;
    private readonly ILogger<FacebookTokenValidator> _logger;

    public SocialProvider Provider => SocialProvider.Facebook;

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
            var debug = debugDoc.Deserialize<FacebookDebugTokenResponse>();
            if (debug?.Data == null)
            {
                _logger.LogError("Malformed debug_token response from Facebook");
                return ExternalAuthResult.Fail("Malformed response from Facebook");
            }

            var appId = debug.Data.AppId;
            var isValid = debug.Data.IsValid;
            var expiresAt = debug.Data.ExpiresAt;
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
            var profile = profileDoc.Deserialize<FacebookMeResponse>();
            if (profile == null)
            {
                _logger.LogError("Malformed me response from Facebook");
                return ExternalAuthResult.Fail("Malformed response from Facebook");
            }

            var id = profile.Id ?? string.Empty;
            var email = profile.Email ?? string.Empty;
            var name = profile.Name ?? string.Empty;

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.Name, name)
            };
            var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "facebook"));
            var info = new ExternalLoginInfo(principal, "Facebook", id, "Facebook");
            return ExternalAuthResult.Success(info);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error validating Facebook token");
            return ExternalAuthResult.Fail("Network error validating Facebook token");
        }
        catch (FacebookApiException ex)
        {
            _logger.LogError(ex, "Network error validating Facebook token");
            return ExternalAuthResult.Fail("Network error validating Facebook token");
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Malformed JSON from Facebook");
            return ExternalAuthResult.Fail("Malformed response from Facebook");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating Facebook token");
            return ExternalAuthResult.Fail("Error validating Facebook token");
        }
    }
}
