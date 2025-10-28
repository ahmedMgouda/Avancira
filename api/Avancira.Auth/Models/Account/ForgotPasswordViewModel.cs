using System.ComponentModel.DataAnnotations;

namespace Avancira.Auth.Models.Account;

public class ForgotPasswordViewModel
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address")]
    [Display(Name = "Email Address")]
    [StringLength(256, ErrorMessage = "Email cannot exceed 256 characters")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Remaining cooldown time in seconds before another request can be made
    /// </summary>
    public int RemainingCooldown { get; set; }
}

public class ForgotPasswordConfirmationViewModel
{
    /// <summary>
    /// Email address where reset link was sent
    /// Null if we don't want to reveal the email for security
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Remaining cooldown time in seconds before resend is allowed
    /// </summary>
    public int RemainingCooldown { get; set; }
}