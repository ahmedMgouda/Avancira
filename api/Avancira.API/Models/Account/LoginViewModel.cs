using System.ComponentModel.DataAnnotations;

namespace Avancira.API.Models.Account;

public class LoginViewModel
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;

    public string ReturnUrl { get; set; } = "/";
    public bool RememberMe { get; internal set; }
}
