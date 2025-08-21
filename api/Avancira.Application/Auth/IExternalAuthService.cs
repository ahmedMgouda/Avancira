using Microsoft.AspNetCore.Identity;

namespace Avancira.Application.Auth;

public interface IExternalAuthService
{
    Task<ExternalAuthResult> ValidateGoogleTokenAsync(string idToken);
    Task<ExternalAuthResult> ValidateFacebookTokenAsync(string accessToken);
}
