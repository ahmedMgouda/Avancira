using Avancira.Application.Identity.Users.Constants;
using Avancira.Application.Identity.Users.Dtos;
using FluentValidation;

namespace Avancira.Application.Identity.Users.Validators;
public class ResetPasswordValidator : AbstractValidator<ResetPasswordDto>
{
    public ResetPasswordValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty();

        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(8).WithMessage(UserErrorMessages.PasswordTooShort)
            .Matches("[A-Z]").WithMessage(UserErrorMessages.PasswordRequiresUppercase)
            .Matches("[a-z]").WithMessage(UserErrorMessages.PasswordRequiresLowercase)
            .Matches("[0-9]").WithMessage(UserErrorMessages.PasswordRequiresDigit)
            .Matches("[^a-zA-Z0-9]").WithMessage(UserErrorMessages.PasswordRequiresNonAlphanumeric);

        RuleFor(x => x.ConfirmPassword)
            .NotEmpty()
            .Equal(x => x.Password).WithMessage(UserErrorMessages.PasswordsDoNotMatch);

        RuleFor(x => x.Token).NotEmpty();
    }
}
