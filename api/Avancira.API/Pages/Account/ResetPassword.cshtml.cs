using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Linq;
using Avancira.Application.Identity.Users.Abstractions;
using Avancira.Application.Identity.Users.Dtos;
using Avancira.Domain.Common.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Avancira.API.Pages.Account;

[ValidateAntiForgeryToken]
public class ResetPasswordModel : PageModel
{
    private readonly IUserService _userService;
    public ResetPasswordModel(IUserService userService) => _userService = userService;

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        public string UserId { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;

        [Required, Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public void OnGet(string? userId, string? token)
    {
        Input.UserId = userId ?? string.Empty;
        Input.Token = token ?? string.Empty;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        var dto = new ResetPasswordDto
        {
            UserId = Input.UserId,
            Password = Input.Password,
            ConfirmPassword = Input.ConfirmPassword,
            Token = Input.Token
        };

        try
        {
            await _userService.ResetPasswordAsync(dto, CancellationToken.None);
            TempData["SuccessMessage"] = "Password reset successfully.";
            return RedirectToPage("/Account/Login");
        }
        catch (AvanciraException ex)
        {
            var errors = ex.ErrorMessages.Any() ? ex.ErrorMessages : new[] { ex.Message };
            foreach (var error in errors)
                ModelState.AddModelError(string.Empty, error);
            return Page();
        }
    }
}
