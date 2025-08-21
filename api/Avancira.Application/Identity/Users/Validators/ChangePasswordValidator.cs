using Avancira.Application.Identity.Users.Dtos;
using FluentValidation;

namespace Avancira.Application.Identity.Users.Validators;
public class ChangePasswordValidator : AbstractValidator<ChangePasswordDto>
{
    public ChangePasswordValidator()
    {
        RuleFor(p => p.Password)
            .NotEmpty();

        RuleFor(p => p.NewPassword)
            .ApplyPasswordRules();

        RuleFor(p => p.ConfirmNewPassword)
            .Equal(p => p.NewPassword)
                .WithMessage("passwords do not match.");
    }
}
