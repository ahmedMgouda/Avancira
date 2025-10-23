using Avancira.Application.TutorSubjects.Dtos;
using FluentValidation;

namespace Avancira.Application.TutorSubjects.Validators;

public class TutorSubjectUpdateDtoValidator : AbstractValidator<TutorSubjectUpdateDto>
{
    public TutorSubjectUpdateDtoValidator()
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
