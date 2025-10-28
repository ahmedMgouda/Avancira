using Avancira.Application.Listings.Dtos;
using FluentValidation;

namespace Avancira.Application.Listings.Validators;

public class ListingUpdateDtoValidator : AbstractValidator<ListingUpdateDto>
{
    public ListingUpdateDtoValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0);

        RuleFor(x => x.HourlyRate)
            .GreaterThan(0)
            .LessThan(10000);

        RuleFor(x => x.SortOrder)
            .GreaterThanOrEqualTo(0);
    }
}
