using Microsoft.AspNetCore.Identity;

namespace Avancira.Application.Auth;

public interface IExternalAuthService
{
    Task<ExternalLoginInfo?> ValidateGoogleTokenAsync(string idToken);
    Task<ExternalLoginInfo?> ValidateFacebookTokenAsync(string accessToken);
}
