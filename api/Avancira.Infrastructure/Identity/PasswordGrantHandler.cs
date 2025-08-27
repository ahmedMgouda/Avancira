using OpenIddict.Abstractions;
using OpenIddict.Server;
using Avancira.Application.Identity;
using static OpenIddict.Server.OpenIddictServerEvents;

namespace Avancira.Infrastructure.Identity;

public class PasswordGrantHandler : IOpenIddictServerHandler<HandleTokenRequestContext>
{
    private readonly IUserAuthenticationService _userAuthenticationService;

    public PasswordGrantHandler(IUserAuthenticationService userAuthenticationService)
        => _userAuthenticationService = userAuthenticationService;

    public async ValueTask HandleAsync(HandleTokenRequestContext context)
    {
        if (!context.Request.IsPasswordGrantType())
        {
            return;
        }

        var email = context.Request.Username;
        var password = context.Request.Password;

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            context.Reject(OpenIddictConstants.Errors.InvalidGrant,
                "The username or password cannot be empty.");
            return;
        }

        var user = await _userAuthenticationService.ValidateCredentialsAsync(email, password);
        if (user is null)
        {
            context.Reject(OpenIddictConstants.Errors.InvalidGrant,
                "The username or password is invalid.");
            return;
        }

        var principal = await _userAuthenticationService.CreatePrincipalAsync(user);
        principal.SetScopes(context.Request.GetScopes());

        context.Principal = principal;
        context.HandleRequest();
    }
}
