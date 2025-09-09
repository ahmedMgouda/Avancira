using System.Threading;
using System.ComponentModel.DataAnnotations;
using Avancira.Application.Identity.Users.Abstractions;
using Avancira.Application.Identity.Users.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Avancira.API.Pages.Account;

[ValidateAntiForgeryToken]
public class ForgotPasswordModel : PageModel
{
    private readonly IUserService _userService;
    public ForgotPasswordModel(IUserService userService) => _userService = userService;

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? Message { get; set; }

    public class InputModel
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;
    }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        await _userService.ForgotPasswordAsync(new ForgotPasswordDto { Email = Input.Email }, CancellationToken.None);
        Message = "If an account with that email exists, a password reset link has been sent.";
        return Page();
    }
}
