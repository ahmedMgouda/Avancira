using Microsoft.AspNetCore.Identity;

namespace Avancira.Application.Auth;

public interface IExternalAuthService
{
    Task<ExternalAuthResult> ValidateTokenAsync(string provider, string token);
    bool SupportsProvider(string provider);
}
