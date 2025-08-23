using Microsoft.AspNetCore.Identity;

namespace Avancira.Application.Auth;

public interface IExternalAuthService
{
    Task<ExternalAuthResult> ValidateTokenAsync(SocialProvider provider, string token);
    bool SupportsProvider(SocialProvider provider);
}
