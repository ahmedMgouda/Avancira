using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.Facebook;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Avancira.Infrastructure.Identity.Users;
using Microsoft.AspNetCore.Authentication;
using OpenIddict.Abstractions; // ✅ for OpenIddictConstants
using static Avancira.Infrastructure.Auth.AuthConstants;

namespace Avancira.API.Pages.Account;

[ValidateAntiForgeryToken]
public class LoginModel : PageModel
{
    private readonly SignInManager<User> _signInManager;

    private static readonly HashSet<string> AllowedProviders =
        new(StringComparer.OrdinalIgnoreCase)
        {
            GoogleDefaults.AuthenticationScheme,
            FacebookDefaults.AuthenticationScheme
        };

    public LoginModel(SignInManager<User> signInManager)
        => _signInManager = signInManager;

    [BindProperty]
    public InputModel Input { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string ReturnUrl { get; set; } = "/";

    public class InputModel
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }

    public IActionResult OnGet(string? returnUrl = null, string? provider = null)
    {
        ReturnUrl = returnUrl ?? "/";

        if (!string.IsNullOrWhiteSpace(provider) && AllowedProviders.Contains(provider))
            return Redirect($"/api/auth/external-login?provider={provider}&returnUrl={ReturnUrl}");

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        ReturnUrl = string.IsNullOrEmpty(ReturnUrl) ? "/connect/authorize" : ReturnUrl;

        if (!ModelState.IsValid)
            return Page();

        var user = await _signInManager.UserManager.FindByEmailAsync(Input.Email);
        if (user is null || !await _signInManager.UserManager.CheckPasswordAsync(user, Input.Password))
        {
            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            return Page();
        }

        var principal = await _signInManager.CreateUserPrincipalAsync(user);
        var identity = (ClaimsIdentity)principal.Identity!;

        // 🔑 Required for "openid"
        if (identity.FindFirst(OpenIddictConstants.Claims.Subject) is null)
            identity.AddClaim(new Claim(OpenIddictConstants.Claims.Subject, user.Id));

        // 🔑 For "profile"
        if (identity.FindFirst(OpenIddictConstants.Claims.Name) is null && !string.IsNullOrEmpty(user.UserName))
            identity.AddClaim(new Claim(OpenIddictConstants.Claims.Name, user.UserName));

        if (identity.FindFirst(OpenIddictConstants.Claims.GivenName) is null && !string.IsNullOrEmpty(user.FirstName))
            identity.AddClaim(new Claim(OpenIddictConstants.Claims.GivenName, user.FirstName));

        if (identity.FindFirst(OpenIddictConstants.Claims.FamilyName) is null && !string.IsNullOrEmpty(user.LastName))
            identity.AddClaim(new Claim(OpenIddictConstants.Claims.FamilyName, user.LastName));

        // 🔑 For "email"
        if (identity.FindFirst(OpenIddictConstants.Claims.Email) is null && !string.IsNullOrEmpty(user.Email))
            identity.AddClaim(new Claim(OpenIddictConstants.Claims.Email, user.Email));

        if (identity.FindFirst(OpenIddictConstants.Claims.EmailVerified) is null)
            identity.AddClaim(new Claim(OpenIddictConstants.Claims.EmailVerified, "true", ClaimValueTypes.Boolean));

        // 🔑 Optional: roles
        var roles = await _signInManager.UserManager.GetRolesAsync(user);
        foreach (var role in roles)
        {
            if (!identity.HasClaim(c => c.Type == OpenIddictConstants.Claims.Role && c.Value == role))
                identity.AddClaim(new Claim(OpenIddictConstants.Claims.Role, role));
        }

        // Debug: dump claims (remove in production)
        foreach (var c in identity.Claims)
            Console.WriteLine($"{c.Type} = {c.Value}");

        // ✅ Issue cookie for OpenIddict
        await HttpContext.SignInAsync(
            Cookies.IdentityExchange,
            new ClaimsPrincipal(identity));

        return LocalRedirect(ReturnUrl);
    }
}
