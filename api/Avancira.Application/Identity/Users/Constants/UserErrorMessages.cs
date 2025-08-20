namespace Avancira.Application.Identity.Users.Constants;

public static class UserErrorMessages
{
    public const string InvalidPasswordResetRequest = "Invalid password reset request.";
    public const string InvalidPasswordResetToken = "Invalid password reset token.";
    public const string ErrorResettingPassword = "Error resetting password.";
    public const string PasswordResetUnavailable = "Password reset is temporarily unavailable.";

    public const string PasswordTooShort = "Password must be at least 8 characters long.";
    public const string PasswordRequiresUppercase = "Password must contain at least one uppercase letter.";
    public const string PasswordRequiresLowercase = "Password must contain at least one lowercase letter.";
    public const string PasswordRequiresDigit = "Password must contain at least one digit.";
    public const string PasswordRequiresNonAlphanumeric = "Password must contain at least one non-alphanumeric character.";
    public const string PasswordsDoNotMatch = "Passwords do not match.";
}

