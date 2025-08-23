using System.Security.Claims;
using Avancira.Application.Auth;
using Avancira.Application.Options;
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
        catch (Exception ex)
        {
            return Fail(ExternalAuthErrorType.InvalidToken, "Google", "Google token validation failed", ex);
        }
    }
}
