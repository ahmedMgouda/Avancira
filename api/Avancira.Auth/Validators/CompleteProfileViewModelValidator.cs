using Avancira.Auth.Models.Account;
using FluentValidation;
using PhoneNumbers;

namespace Avancira.Auth.Validators;

public class CompleteProfileViewModelValidator : AbstractValidator<CompleteProfileViewModel>
{
    private static readonly PhoneNumberUtil PhoneUtil = PhoneNumberUtil.GetInstance();

    public CompleteProfileViewModelValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required.")
            .MaximumLength(100).WithMessage("First name cannot exceed 100 characters.");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required.")
            .MaximumLength(100).WithMessage("Last name cannot exceed 100 characters.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Invalid email format.");

        RuleFor(x => x.CountryCode)
            .NotEmpty().WithMessage("Country selection is required.")
            .Length(2).WithMessage("Invalid country code format.");

        RuleFor(x => x.TimeZoneId)
            .NotEmpty().WithMessage("Time zone selection is required.");

        RuleFor(x => x.Gender)
            .Matches("^(Male|Female|Other)?$")
            .When(x => !string.IsNullOrEmpty(x.Gender))
            .WithMessage("Invalid gender selection.");

        RuleFor(x => x.PhoneNumber)
            .Must(BeValidInternationalNumber)
            .When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber))
            .WithMessage("Please enter a valid international phone number (e.g. +14155552671).");
    }

    private bool BeValidInternationalNumber(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return true;

        try
        {
            var parsed = PhoneUtil.Parse(phone, null);
            return PhoneUtil.IsValidNumber(parsed);
        }
        catch
        {
            return false;
        }
    }
}
