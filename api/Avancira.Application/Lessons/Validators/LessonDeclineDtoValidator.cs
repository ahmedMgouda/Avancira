using Avancira.Application.Lessons.Dtos;
using FluentValidation;

namespace Avancira.Application.Lessons.Validators;

public class LessonDeclineDtoValidator : AbstractValidator<LessonDeclineDto>
{
    public LessonDeclineDtoValidator()
    {
        RuleFor(x => x.LessonId)
            .GreaterThan(0);

        RuleFor(x => x.TutorId)
            .NotEmpty();

        RuleFor(x => x.Reason)
            .MaximumLength(500);
    }
}
