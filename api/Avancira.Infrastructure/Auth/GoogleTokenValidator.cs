using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Avancira.Application.Auth;
using Avancira.Application.Options;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Avancira.Infrastructure.Auth;

public class GoogleTokenValidator : IExternalTokenValidator
{
    private readonly GoogleOptions _options;
    private readonly ILogger<GoogleTokenValidator> _logger;

    public string Provider => "google";

    public GoogleTokenValidator(
        IOptions<GoogleOptions> googleOptions,
        ILogger<GoogleTokenValidator> logger)
    {
        _options = googleOptions.Value;
        _logger = logger;
    }

    public Task<ExternalAuthResult> ValidateAsync(string idToken)
    {
        var handler = new JwtSecurityTokenHandler();
        var parameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuers = new[] { "accounts.google.com", "https://accounts.google.com" },
            ValidateAudience = true,
            ValidAudience = _options.ClientId,
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
}
