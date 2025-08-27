using System.Security.Claims;
using Avancira.Application.Identity;
using Microsoft.AspNetCore.Identity;

namespace Avancira.Infrastructure.Identity.Users;

public class UserAuthenticationService(
    UserManager<User> userManager,
    SignInManager<User> signInManager) : IUserAuthenticationService
{
    public async Task<IdentityUser?> ValidateCredentialsAsync(string email, string password)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return null;
        }

        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            return null;
        }

        var result = await signInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: false);
        if (!result.Succeeded)
        {
            return null;
        }

        return user;
    }

    public Task<ClaimsPrincipal> CreatePrincipalAsync(IdentityUser user)
        => signInManager.CreateUserPrincipalAsync((User)user);
}

