using Avancira.Application.Lessons.Dtos;
using FluentValidation;

namespace Avancira.Application.Lessons.Validators;

public class LessonRescheduleDtoValidator : AbstractValidator<LessonRescheduleDto>
{
    public LessonRescheduleDtoValidator()
    {
        RuleFor(x => x.LessonId)
            .GreaterThan(0);

        RuleFor(x => x.DurationMinutes)
            .GreaterThan(0);

        RuleFor(x => x.NewScheduledAtUtc)
            .GreaterThan(DateTime.UtcNow);

        RuleFor(x => x.RequestedBy)
            .NotEmpty();
    }
}
