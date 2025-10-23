using Avancira.Application.TutorProfiles.Dtos;
using FluentValidation;

namespace Avancira.Application.TutorProfiles.Validators;

public class TutorProfileUpdateDtoValidator : AbstractValidator<TutorProfileUpdateDto>
{
    public TutorProfileUpdateDtoValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty();

        RuleFor(x => x.Headline)
            .NotEmpty()
            .Length(5, 200);

        RuleFor(x => x.Description)
            .NotEmpty()
            .Length(50, 2000);

        RuleFor(x => x.YearsOfExperience)
            .InclusiveBetween(0, 50);

        RuleFor(x => x.TeachingPhilosophy)
            .MaximumLength(500);

        RuleFor(x => x.Specializations)
            .MaximumLength(500);

        RuleFor(x => x.Languages)
            .MaximumLength(200);

        RuleFor(x => x.TrialLessonRate)
            .GreaterThan(0)
            .When(x => x.TrialLessonRate.HasValue);

        RuleFor(x => x.TrialLessonDurationMinutes)
            .GreaterThan(0)
            .When(x => x.TrialLessonDurationMinutes.HasValue);

        RuleFor(x => x.MinSessionDurationMinutes)
            .GreaterThan(0);

        RuleFor(x => x.MaxSessionDurationMinutes)
            .GreaterThan(0);

        RuleFor(x => x)
            .Must(dto => dto.MinSessionDurationMinutes <= dto.MaxSessionDurationMinutes)
            .WithMessage("Minimum session duration cannot exceed maximum session duration.");

        RuleForEach(x => x.Availabilities)
            .SetValidator(new TutorAvailabilityUpsertDtoValidator());
    }

    private class TutorAvailabilityUpsertDtoValidator : AbstractValidator<TutorAvailabilityUpsertDto>
    {
        public TutorAvailabilityUpsertDtoValidator()
        {
            RuleFor(x => x.DayOfWeek)
                .IsInEnum();

            RuleFor(x => x.StartTime)
                .LessThan(x => x.EndTime)
                .WithMessage("Availability start time must be earlier than end time.");
        }
    }
}
