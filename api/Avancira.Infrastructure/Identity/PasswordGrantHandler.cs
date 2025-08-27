using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using OpenIddict.Server.AspNetCore;
using Avancira.Infrastructure.Identity.Users;
using static OpenIddict.Server.OpenIddictServerEvents;

namespace Avancira.Infrastructure.Identity;

public class PasswordGrantHandler : IOpenIddictServerHandler<HandleTokenRequestContext>
{
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;

    public PasswordGrantHandler(UserManager<User> userManager, SignInManager<User> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

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
            return;
        }

        var user = await _userManager.FindByEmailAsync(email);
        if (user is null)
        {
            return;
        }

        var result = await _signInManager.CheckPasswordSignInAsync(user, password, false);
        if (!result.Succeeded)
        {
            return;
        }

        var identity = new ClaimsIdentity(OpenIddictServerAspNetCoreDefaults.AuthenticationType);
        identity.SetClaim(OpenIddictConstants.Claims.Subject, user.Id);
        if (!string.IsNullOrEmpty(user.Email))
        {
            identity.SetClaim(OpenIddictConstants.Claims.Email, user.Email);
        }
        if (!string.IsNullOrEmpty(user.UserName))
        {
            identity.SetClaim(OpenIddictConstants.Claims.Name, user.UserName);
        }

        var principal = new ClaimsPrincipal(identity);
        principal.SetScopes(context.Request.GetScopes());

        context.Principal = principal;
        context.HandleRequest();
    }
}
