using Avancira.Application.Lessons.Dtos;
using FluentValidation;

namespace Avancira.Application.Lessons.Validators;

public class LessonCompleteDtoValidator : AbstractValidator<LessonCompleteDto>
{
    public LessonCompleteDtoValidator()
    {
        RuleFor(x => x.LessonId)
            .GreaterThan(0);

        RuleFor(x => x.SessionSummary)
            .MaximumLength(2000);

        RuleFor(x => x.TutorNotes)
            .MaximumLength(2000);
    }
}
