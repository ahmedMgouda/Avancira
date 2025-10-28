using System;
using Avancira.Application.Lessons.Dtos;
using FluentValidation;

namespace Avancira.Application.Lessons.Validators;

public class LessonCreateDtoValidator : AbstractValidator<LessonCreateDto>
{
    public LessonCreateDtoValidator()
    {
        RuleFor(x => x.StudentId)
            .NotEmpty();

        RuleFor(x => x.TutorId)
            .NotEmpty();

        RuleFor(x => x.ListingId)
            .GreaterThan(0);

        RuleFor(x => x.DurationMinutes)
            .GreaterThan(0);

        RuleFor(x => x.ScheduledAtUtc)
            .GreaterThan(DateTime.UtcNow)
            .WithMessage("Lesson must be scheduled in the future.");
    }
}
