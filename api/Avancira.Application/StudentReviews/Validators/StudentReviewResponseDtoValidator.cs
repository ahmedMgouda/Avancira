using Avancira.Application.StudentReviews.Dtos;
using FluentValidation;

namespace Avancira.Application.StudentReviews.Validators;

public class StudentReviewResponseDtoValidator : AbstractValidator<StudentReviewResponseDto>
{
    public StudentReviewResponseDtoValidator()
    {
        RuleFor(x => x.ReviewId)
            .GreaterThan(0);

        RuleFor(x => x.TutorResponse)
            .NotEmpty()
            .MaximumLength(1000);
    }
}
