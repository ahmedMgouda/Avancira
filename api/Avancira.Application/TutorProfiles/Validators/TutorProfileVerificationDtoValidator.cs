using Avancira.Application.TutorProfiles.Dtos;
using FluentValidation;

namespace Avancira.Application.TutorProfiles.Validators;

public class TutorProfileVerificationDtoValidator : AbstractValidator<TutorProfileVerificationDto>
{
    public TutorProfileVerificationDtoValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty();

        When(x => !x.Approve, () =>
        {
            RuleFor(x => x.AdminComment)
                .NotEmpty()
                .MaximumLength(500);
        });
    }
}
