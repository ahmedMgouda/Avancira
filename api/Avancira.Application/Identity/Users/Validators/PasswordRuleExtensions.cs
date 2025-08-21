using Avancira.Application.Identity.Users.Constants;
using FluentValidation;

namespace Avancira.Application.Identity.Users.Validators;

public static class PasswordRuleExtensions
{
    public static IRuleBuilderOptions<T, string> ApplyPasswordRules<T>(this IRuleBuilder<T, string> ruleBuilder)
        => ruleBuilder
            .NotEmpty()
            .MinimumLength(PasswordRules.MinLength).WithMessage(UserErrorMessages.PasswordTooShort)
            .Matches(PasswordRules.UppercasePattern).WithMessage(UserErrorMessages.PasswordRequiresUppercase)
            .Matches(PasswordRules.LowercasePattern).WithMessage(UserErrorMessages.PasswordRequiresLowercase)
            .Matches(PasswordRules.DigitPattern).WithMessage(UserErrorMessages.PasswordRequiresDigit)
            .Matches(PasswordRules.NonAlphanumericPattern).WithMessage(UserErrorMessages.PasswordRequiresNonAlphanumeric);
}

