using Avancira.Application.Listings.Dtos;
using FluentValidation;

namespace Avancira.Application.Listings.Validators;

public class ListingCreateDtoValidator : AbstractValidator<ListingCreateDto>
{
    public ListingCreateDtoValidator()
    {
        RuleFor(x => x.TutorId)
            .NotEmpty();

        RuleFor(x => x.SubjectId)
            .GreaterThan(0);

        RuleFor(x => x.HourlyRate)
            .GreaterThan(0)
            .LessThan(10000);

        RuleFor(x => x.SortOrder)
            .GreaterThanOrEqualTo(0);
    }
}
