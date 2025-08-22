using System.Security.Claims;
using Avancira.Application.Auth;
using Avancira.Application.Options;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Google.Apis.Auth;

namespace Avancira.Infrastructure.Auth;

public class GoogleTokenValidator : IExternalTokenValidator
{
    private readonly GoogleOptions _options;
    private readonly IGoogleJsonWebSignatureValidator _validator;
    private readonly ILogger<GoogleTokenValidator> _logger;

    public string Provider => "google";

    public GoogleTokenValidator(
        IOptions<GoogleOptions> googleOptions,
        IGoogleJsonWebSignatureValidator validator,
        ILogger<GoogleTokenValidator> logger)
    {
        _options = googleOptions.Value;
        _validator = validator;
        _logger = logger;
    }

    public async Task<ExternalAuthResult> ValidateAsync(string idToken)
    {
        try
        {
            var payload = await _validator.ValidateAsync(idToken, _options.ClientId);
            if (payload.EmailVerified != true)
            {
                _logger.LogWarning("Google token validation failed: email not verified");
                return ExternalAuthResult.Fail("Unverified Google email");
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
            _logger.LogWarning(ex, "Google token validation failed");
            return ExternalAuthResult.Fail("Invalid Google token");
        }
    }
}
