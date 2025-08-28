using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Avancira.Infrastructure.Identity.Users;

namespace Avancira.API.Pages.Account;

public class LoginModel(SignInManager<User> signInManager) : PageModel
{
    private readonly SignInManager<User> _signInManager = signInManager;

    [BindProperty]
    public InputModel Input { get; set; } = new();

    [FromQuery]
    public string ReturnUrl { get; set; } = "/";

    public class InputModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }

    public void OnGet(string? returnUrl = null)
        => ReturnUrl = returnUrl ?? "/";

    [ValidateAntiForgeryToken]
    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        ReturnUrl = returnUrl ?? "/";
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var result = await _signInManager.PasswordSignInAsync(Input.Email, Input.Password, false, lockoutOnFailure: false);
        if (result.Succeeded)
        {
            return LocalRedirect(ReturnUrl);
        }

        ModelState.AddModelError(string.Empty, "Invalid login attempt.");
        return Page();
    }

}

