using System.Security.Claims;
using Avancira.Application.Auth;
using Avancira.Infrastructure.Identity.Users;
using Microsoft.AspNetCore.Identity;

namespace Avancira.Infrastructure.Auth;

public class ExternalUserService : IExternalUserService
{
    private readonly UserManager<User> _userManager;

    public ExternalUserService(UserManager<User> userManager)
    {
        _userManager = userManager;
    }

    public async Task<ExternalUserResult> EnsureUserAsync(ExternalLoginInfo info)
    {
        var user = await _userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);
        if (user is null)
        {
            var email = info.Principal.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrEmpty(email)) return ExternalUserResult.Unauthorized();

            var emailVerified = info.LoginProvider switch
            {
                "Google" => true,
                _ => bool.TryParse(info.Principal.FindFirstValue("email_verified"), out var verified) && verified
            };

            user = await _userManager.FindByEmailAsync(email);
            if (user is null)
            {
                user = new User
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = emailVerified,
                    FirstName = info.Principal.FindFirstValue(ClaimTypes.Name) ?? string.Empty
                };
                var createResult = await _userManager.CreateAsync(user);
                if (!createResult.Succeeded)
                {
                    var error = createResult.Errors.FirstOrDefault()?.Description ?? "Failed to create user.";
                    return ExternalUserResult.Problem(error);
                }
            }
            else if (emailVerified && !user.EmailConfirmed)
            {
                user.EmailConfirmed = true;
                await _userManager.UpdateAsync(user);
            }

            var loginResult = await _userManager.AddLoginAsync(user, info);
            if (!loginResult.Succeeded)
            {
                var error = loginResult.Errors.FirstOrDefault()?.Description ?? "Failed to add external login.";
                return ExternalUserResult.BadRequest(error);
            }
        }

        return ExternalUserResult.Success(user.Id);
    }
}

