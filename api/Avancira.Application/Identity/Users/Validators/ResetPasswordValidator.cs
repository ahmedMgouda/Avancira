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
            .ApplyPasswordRules();

        RuleFor(x => x.ConfirmPassword)
            .NotEmpty()
            .Equal(x => x.Password).WithMessage(UserErrorMessages.PasswordsDoNotMatch);

        RuleFor(x => x.Token).NotEmpty();
    }
}
