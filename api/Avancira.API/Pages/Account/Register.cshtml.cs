using System.ComponentModel.DataAnnotations;
using Avancira.Application.Identity.Users.Abstractions;
using Avancira.Application.Identity.Users.Dtos;
using Avancira.Domain.Common.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Avancira.API.Pages.Account;

[ValidateAntiForgeryToken]
public class RegisterModel : PageModel
{
    private readonly IUserService _userService;

    public RegisterModel(IUserService userService)
        => _userService = userService;

    [BindProperty]
    public InputModel Input { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string ReturnUrl { get; set; } = "/";

    public class InputModel
    {
        [Required]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        public string LastName { get; set; } = string.Empty;

        [Required]
        public string UserName { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;

        [Required, Compare(nameof(Password))]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Range(typeof(bool), "true", "true", ErrorMessage = "You must agree to the Privacy Policy & Terms.")]
        public bool AcceptTerms { get; set; }
    }

    public void OnGet(string? returnUrl = null)
    {
        ReturnUrl = returnUrl ?? "/";
    }

    public async Task<IActionResult> OnPostAsync()
    {
        ReturnUrl = string.IsNullOrEmpty(ReturnUrl) ? "/connect/authorize" : ReturnUrl;

        if (!ModelState.IsValid)
            return Page();

        var dto = new RegisterUserDto
        {
            FirstName = Input.FirstName,
            LastName = Input.LastName,
            Email = Input.Email,
            UserName = Input.UserName,
            Password = Input.Password,
            ConfirmPassword = Input.ConfirmPassword,
            AcceptTerms = Input.AcceptTerms
        };

        var origin = $"{Request.Scheme}://{Request.Host.Value}{Request.PathBase.Value}";
        try
        {
            await _userService.RegisterAsync(dto, origin, HttpContext.RequestAborted);
            return RedirectToPage("Login", new { returnUrl = ReturnUrl });
        }
        catch (AvanciraException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return Page();
        }
    }
}
