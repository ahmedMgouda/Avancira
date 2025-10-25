using Avancira.Auth.Models.Account;
using FluentValidation;
using PhoneNumbers;

namespace Avancira.Auth.Validators;

public class RegisterViewModelValidator : AbstractValidator<RegisterViewModel>
{
    private static readonly PhoneNumberUtil PhoneUtil = PhoneNumberUtil.GetInstance();

    public RegisterViewModelValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required.")
            .MaximumLength(100).WithMessage("First name cannot exceed 100 characters.");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required.")
            .MaximumLength(100).WithMessage("Last name cannot exceed 100 characters.");

        RuleFor(x => x.Gender)
            .Matches("^(Male|Female|Other)?$")
            .When(x => !string.IsNullOrEmpty(x.Gender))
            .WithMessage("Invalid gender selection.");

        RuleFor(x => x.DateOfBirth)
            .NotNull().WithMessage("Date of birth is required.")
            .LessThan(DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("Date of birth must be in the past.");

        // === CONTACT ===
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email address is required.")
            .EmailAddress().WithMessage("Please enter a valid email address.");

        RuleFor(x => x.CountryCode)
            .NotEmpty().WithMessage("Country is required.")
            .Length(2).WithMessage("Invalid country code format.");

        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("Phone number is required.")
            .Must(BeValidInternationalNumber)
            .WithMessage("Please enter a valid international phone number (e.g. +14155552671).");

        RuleFor(x => x.TimeZoneId)
            .NotEmpty().WithMessage("Time zone is required.");

        // === AUTH ===
        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(6).WithMessage("Password must be at least 6 characters long.");

        RuleFor(x => x.ConfirmPassword)
            .Equal(x => x.Password).WithMessage("Passwords do not match.");

        RuleFor(x => x.AcceptTerms)
            .Equal(true).WithMessage("You must accept the terms and conditions.");
    }

    private bool BeValidInternationalNumber(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone)) return false;

        try
        {
            var parsed = PhoneUtil.Parse(phone, null); // Accepts +E.164 input
            return PhoneUtil.IsValidNumber(parsed);
        }
        catch
        {
            return false;
        }
    }
}
