namespace Avancira.Admin.Infrastructure.Auth;

public interface IAuthenticationService
{

    void NavigateToExternalLogin(string returnUrl);

    Task<bool> CompleteLoginAsync(string code, string state);

    Task LogoutAsync();

    Task ReLoginAsync(string returnUrl);
}
