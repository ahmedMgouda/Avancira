namespace Avancira.Application.Identity.Users.Constants;

/// <summary>
/// Password complexity rules shared with the frontend.
/// Keep in sync with Frontend.Angular/src/app/validators/password-rules.ts
/// </summary>
public static class PasswordRules
{
    public const int MinLength = 8;
    public const string UppercasePattern = "[A-Z]";
    public const string LowercasePattern = "[a-z]";
    public const string DigitPattern = "[0-9]";
    public const string NonAlphanumericPattern = "[^a-zA-Z0-9]";
}

