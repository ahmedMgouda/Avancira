using Avancira.Application.StudentProfiles.Dtos;
using FluentValidation;

namespace Avancira.Application.StudentProfiles.Validators;

public class StudentProfileUpdateDtoValidator : AbstractValidator<StudentProfileUpdateDto>
{
    public StudentProfileUpdateDtoValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty();

        RuleFor(x => x.LearningGoal)
            .MaximumLength(500);

        RuleFor(x => x.CurrentEducationLevel)
            .MaximumLength(100);

        RuleFor(x => x.School)
            .MaximumLength(100);

        RuleFor(x => x.Major)
            .MaximumLength(100);
    }
}
