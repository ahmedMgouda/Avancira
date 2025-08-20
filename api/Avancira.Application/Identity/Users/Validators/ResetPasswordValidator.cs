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
            .MinimumLength(8)
                .WithMessage("Password must be at least 8 characters long.")
            .Matches("[A-Z]")
                .WithMessage("Password must contain at least one uppercase letter.")
            .Matches("[a-z]")
                .WithMessage("Password must contain at least one lowercase letter.")
            .Matches("[0-9]")
                .WithMessage("Password must contain at least one digit.")
            .Matches("[^a-zA-Z0-9]")
                .WithMessage("Password must contain at least one non-alphanumeric character.");

        RuleFor(x => x.ConfirmPassword)
            .NotEmpty()
            .Equal(x => x.Password)
                .WithMessage("Passwords do not match.");

        RuleFor(x => x.Token).NotEmpty();
    }
}
