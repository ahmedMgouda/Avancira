using System.Security.Claims;
using System.Text.Json;
using Avancira.Application.Auth;
using Facebook;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Avancira.Infrastructure.Auth;

public class FacebookTokenValidator : ExternalTokenValidatorBase
{
    private readonly IFacebookClient _facebookClient;
    private readonly FacebookOptions _options;

    public override SocialProvider Provider => SocialProvider.Facebook;

    public FacebookTokenValidator(
        IFacebookClient facebookClient,
        IOptions<FacebookOptions> facebookOptions,
        ILogger<FacebookTokenValidator> logger)
        : base(logger)
    {
        _facebookClient = facebookClient;
        _options = facebookOptions.Value;
    }

    public override async Task<ExternalAuthResult> ValidateAsync(string accessToken)
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
                return Fail(ExternalAuthErrorType.MalformedResponse, "Facebook", "Malformed debug_token response from Facebook");
            }

            bool? isEmailVerified = null;
            if (debugDoc.RootElement.TryGetProperty("data", out var dataElement) &&
                dataElement.TryGetProperty("is_email_verified", out var verifiedElement) &&
                verifiedElement.ValueKind is JsonValueKind.True or JsonValueKind.False)
            {
                isEmailVerified = verifiedElement.GetBoolean();
            }

            if (isEmailVerified == false)
            {
                return Fail(
                    ExternalAuthErrorType.UnverifiedEmail,
                    "Facebook",
                    "Facebook token validation failed: email not verified");
            }

            var appId = debug.Data.AppId;
            var isValid = debug.Data.IsValid;
            var expiresAt = debug.Data.ExpiresAt;
            if (appId != _options.AppId || !isValid || DateTimeOffset.FromUnixTimeSeconds(expiresAt) <= DateTimeOffset.UtcNow)
            {
                return Fail(
                    ExternalAuthErrorType.InvalidToken,
                    "Facebook",
                    $"Facebook token invalid: app_id={appId} is_valid={isValid} exp={expiresAt}");
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
                return Fail(ExternalAuthErrorType.MalformedResponse, "Facebook", "Malformed me response from Facebook");
            }

            var id = profile.Id ?? string.Empty;
            var email = profile.Email ?? string.Empty;
            var name = profile.Name ?? string.Empty;

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.Name, name)
            };
            if (isEmailVerified == true)
            {
                claims.Add(new Claim("email_verified", "true"));
            }
            // If is_email_verified is missing, Facebook does not provide an indicator; user email will require manual verification.
            var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "facebook"));
            var info = new ExternalLoginInfo(principal, "Facebook", id, "Facebook");
            return ExternalAuthResult.Success(info);
        }
        catch (HttpRequestException ex)
        {
            return Fail(ExternalAuthErrorType.NetworkError, "Facebook", "Network error validating Facebook token", ex);
        }
        catch (FacebookApiException ex)
        {
            return Fail(ExternalAuthErrorType.NetworkError, "Facebook", "Network error validating Facebook token", ex);
        }
        catch (JsonException ex)
        {
            return Fail(ExternalAuthErrorType.MalformedResponse, "Facebook", "Malformed JSON from Facebook", ex);
        }
        catch (Exception ex)
        {
            return Fail(ExternalAuthErrorType.Error, "Facebook", "Error validating Facebook token", ex);
        }
    }
}
