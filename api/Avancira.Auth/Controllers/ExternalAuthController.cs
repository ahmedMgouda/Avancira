using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.Facebook;
using Avancira.Infrastructure.Identity.Users;

namespace Avancira.Auth.Controllers;

[ApiController]
[Route("api/auth")]
public class ExternalAuthController : ControllerBase
{
    private static readonly HashSet<string> AllowedProviders =
        new(StringComparer.OrdinalIgnoreCase)
        {
            GoogleDefaults.AuthenticationScheme,
            FacebookDefaults.AuthenticationScheme
        };

    private readonly SignInManager<User> _signInManager;
    private readonly UserManager<User> _userManager;

    public ExternalAuthController(SignInManager<User> signInManager, UserManager<User> userManager)
    {
        _signInManager = signInManager;
        _userManager = userManager;
    }

    // STEP 1: Challenge the external provider
    [HttpGet("external-login")]
    [AllowAnonymous]
    public IActionResult ExternalLogin([FromQuery] string provider, [FromQuery] string? returnUrl = "/")
    {
        if (string.IsNullOrWhiteSpace(provider) || !AllowedProviders.Contains(provider))
            return BadRequest("Unknown provider.");

        var callbackUrl = Url.Action(nameof(ExternalCallback), "ExternalAuth", new { returnUrl }, Request.Scheme)!;
        var props = _signInManager.ConfigureExternalAuthenticationProperties(provider, callbackUrl);
        return Challenge(props, provider);
    }

    // STEP 2: Callback after Google/Facebook
    [HttpGet("external-callback")]
    [AllowAnonymous]
    public async Task<IActionResult> ExternalCallback([FromQuery] string? returnUrl = "/")
    {
        var info = await _signInManager.GetExternalLoginInfoAsync();
        if (info is null)
        {
            return Redirect(QueryHelpers.AddQueryString("/Account/Login", "returnUrl", returnUrl ?? "/"));
        }

        var signInResult = await _signInManager.ExternalLoginSignInAsync(
            info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: true);

        if (!signInResult.Succeeded)
        {
            var email = info.Principal.FindFirstValue(ClaimTypes.Email)
                        ?? info.Principal.FindFirstValue("email");

            if (string.IsNullOrWhiteSpace(email))
                return Forbid();

            var user = await _userManager.FindByEmailAsync(email);
            if (user is null)
            {
                user = new User { UserName = email, Email = email, EmailConfirmed = true };
                var createResult = await _userManager.CreateAsync(user);
                if (!createResult.Succeeded) return Forbid();

                await _userManager.AddLoginAsync(user, info);
            }
            else
            {
                var linkResult = await _userManager.AddLoginAsync(user, info);
                if (!linkResult.Succeeded && linkResult.Errors.Any(e => e.Code != "LoginAlreadyAssociated"))
                    return Forbid();
            }

            await _signInManager.SignInAsync(user, isPersistent: false);
        }

        return LocalRedirect(string.IsNullOrWhiteSpace(returnUrl) ? "/" : returnUrl);
    }
}
