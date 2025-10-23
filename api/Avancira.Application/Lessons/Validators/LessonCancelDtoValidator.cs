using Avancira.Application.Lessons.Dtos;
using FluentValidation;

namespace Avancira.Application.Lessons.Validators;

public class LessonCancelDtoValidator : AbstractValidator<LessonCancelDto>
{
    public LessonCancelDtoValidator()
    {
        RuleFor(x => x.LessonId)
            .GreaterThan(0);

        RuleFor(x => x.CanceledBy)
            .NotEmpty()
            .MaximumLength(50);

        RuleFor(x => x.Reason)
            .MaximumLength(500);
    }
}
