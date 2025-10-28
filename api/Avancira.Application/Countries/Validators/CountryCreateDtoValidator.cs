using Avancira.Application.Countries.Dtos;
using FluentValidation;

namespace Avancira.Application.Countries.Validators;

public class CountryCreateDtoValidator : AbstractValidator<CountryCreateDto>
{
    public CountryCreateDtoValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty()
            .Length(2, 3)
            .Matches("^[A-Za-z]+$")
            .WithMessage("Country code must contain only letters.");

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.CurrencyCode)
            .MaximumLength(3);

        RuleFor(x => x.DialingCode)
            .MaximumLength(5);
    }
}
