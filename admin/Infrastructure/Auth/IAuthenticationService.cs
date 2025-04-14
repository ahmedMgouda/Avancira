using Avancira.Admin.Infrastructure.Api;

namespace Avancira.Admin.Infrastructure.Auth;

public interface IAuthenticationService
{

    void NavigateToExternalLogin(string returnUrl);

    Task<bool> LoginAsync(TokenGenerationDto request);

    Task LogoutAsync();

    Task ReLoginAsync(string returnUrl);
}