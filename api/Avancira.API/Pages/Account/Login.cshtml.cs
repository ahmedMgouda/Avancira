using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.Facebook;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Avancira.Infrastructure.Identity.Users;
using Microsoft.AspNetCore.Authentication;
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

        await HttpContext.SignInAsync(Cookies.IdentityExchange, principal);

        return LocalRedirect(ReturnUrl);
    }
}
