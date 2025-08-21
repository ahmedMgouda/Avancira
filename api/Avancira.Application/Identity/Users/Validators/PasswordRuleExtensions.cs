using Avancira.Application.Identity.Users.Constants;
using FluentValidation;

namespace Avancira.Application.Identity.Users.Validators;

public static class PasswordRuleExtensions
{
    public static IRuleBuilderOptions<T, string> ApplyPasswordRules<T>(this IRuleBuilder<T, string> ruleBuilder)
        => ruleBuilder
            .NotEmpty()
            .MinimumLength(8).WithMessage(UserErrorMessages.PasswordTooShort)
            .Matches("[A-Z]").WithMessage(UserErrorMessages.PasswordRequiresUppercase)
            .Matches("[a-z]").WithMessage(UserErrorMessages.PasswordRequiresLowercase)
            .Matches("[0-9]").WithMessage(UserErrorMessages.PasswordRequiresDigit)
            .Matches("[^a-zA-Z0-9]").WithMessage(UserErrorMessages.PasswordRequiresNonAlphanumeric);
}

