using Avancira.Application.Identity.Users.Dtos;
using FluentValidation;

namespace Avancira.Application.Identity.Users.Validators;

public class RegisterUserValidator : AbstractValidator<RegisterUserDto>
{
    public RegisterUserValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty()
            .WithMessage("First name is required.")
            .MaximumLength(50)
            .WithMessage("First name must not exceed 50 characters.");

        RuleFor(x => x.LastName)
            .NotEmpty()
            .WithMessage("Last name is required.")
            .MaximumLength(50)
            .WithMessage("Last name must not exceed 50 characters.");

        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("Email is required.")
            .EmailAddress()
            .WithMessage("A valid email address is required.")
            .MaximumLength(256)
            .WithMessage("Email must not exceed 256 characters.");

        RuleFor(x => x.UserName)
            .NotEmpty()
            .WithMessage("Username is required.")
            .MinimumLength(3)
            .WithMessage("Username must be at least 3 characters long.")
            .MaximumLength(50)
            .WithMessage("Username must not exceed 50 characters.")
            .Matches("^[a-zA-Z0-9._-]+$")
            .WithMessage("Username can only contain letters, numbers, dots, underscores, and hyphens.");

        RuleFor(x => x.Password)
            .ApplyPasswordRules();

        RuleFor(x => x.ConfirmPassword)
            .NotEmpty()
            .WithMessage("Password confirmation is required.")
            .Equal(x => x.Password)
            .WithMessage("Password and confirmation password do not match.");

        RuleFor(x => x.PhoneNumber)
            .Matches(@"^\+?[1-9]\d{1,14}$")
            .WithMessage("Please enter a valid phone number.")
            .When(x => !string.IsNullOrEmpty(x.PhoneNumber));

        RuleFor(x => x.TimeZoneId)
            .MaximumLength(100)
            .WithMessage("Time zone ID must not exceed 100 characters.")
            .When(x => !string.IsNullOrEmpty(x.TimeZoneId));

        RuleFor(x => x.ReferralToken)
            .MaximumLength(255)
            .WithMessage("Referral token must not exceed 255 characters.")
            .When(x => !string.IsNullOrEmpty(x.ReferralToken));

        RuleFor(x => x.AcceptTerms)
            .Equal(true)
            .WithMessage("You must agree to the Privacy Policy & Terms.");
    }
}
