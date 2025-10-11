using System.ComponentModel.DataAnnotations;

namespace Avancira.Auth.Models.Account;

public class ForgotPasswordViewModel
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    public string? Message { get; set; }

    public int RemainingCooldown { get; set; }
}
