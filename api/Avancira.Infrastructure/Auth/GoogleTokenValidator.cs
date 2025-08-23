using System.Security.Claims;
using Avancira.Application.Auth;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Avancira.Infrastructure.Auth;

public class GoogleTokenValidator : ExternalTokenValidatorBase
{
    private readonly GoogleOptions _options;
    private readonly IGoogleJsonWebSignatureValidator _validator;

    public override SocialProvider Provider => SocialProvider.Google;

    public GoogleTokenValidator(
        IOptions<GoogleOptions> googleOptions,
        IGoogleJsonWebSignatureValidator validator,
        ILogger<GoogleTokenValidator> logger)
        : base(logger)
    {
        _options = googleOptions.Value;
        _validator = validator;
    }

    public override async Task<ExternalAuthResult> ValidateAsync(string idToken)
    {
        try
        {
            var payload = await _validator.ValidateAsync(idToken, _options.ClientId);
            if (payload.EmailVerified != true)
            {
                return Fail(
                    ExternalAuthErrorType.UnverifiedEmail,
                    "Google",
                    "Google token validation failed: email not verified");
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Email, payload.Email ?? string.Empty),
                new Claim(ClaimTypes.Name, payload.Name ?? string.Empty)
            };
            var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "google"));
            var info = new ExternalLoginInfo(principal, "Google", payload.Subject, "Google");
            return ExternalAuthResult.Success(info);
        }
        catch (InvalidJwtException ex)
        {
            return Fail(ExternalAuthErrorType.InvalidToken, "Google", "Invalid Google token", ex);
        }
        catch (HttpRequestException ex)
        {
            return Fail(ExternalAuthErrorType.NetworkError, "Google", "Network error validating Google token", ex);
        }
        catch (Exception ex)
        {
            return Fail(ExternalAuthErrorType.Error, "Google", "Error validating Google token", ex);
        }
    }
}
