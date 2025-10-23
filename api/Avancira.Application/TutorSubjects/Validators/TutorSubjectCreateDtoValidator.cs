using Avancira.Application.TutorSubjects.Dtos;
using FluentValidation;

namespace Avancira.Application.TutorSubjects.Validators;

public class TutorSubjectCreateDtoValidator : AbstractValidator<TutorSubjectCreateDto>
{
    public TutorSubjectCreateDtoValidator()
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
