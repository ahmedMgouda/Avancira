using System.ComponentModel.DataAnnotations;

namespace Avancira.Auth.Models.Account;

public class ResetPasswordViewModel
{
    public string UserId { get; set; } = string.Empty;

    public string Token { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;

    [Required, Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
