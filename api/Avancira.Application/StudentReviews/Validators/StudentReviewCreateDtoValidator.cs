using Avancira.Application.StudentReviews.Dtos;
using FluentValidation;

namespace Avancira.Application.StudentReviews.Validators;

public class StudentReviewCreateDtoValidator : AbstractValidator<StudentReviewCreateDto>
{
    public StudentReviewCreateDtoValidator()
    {
        RuleFor(x => x.StudentId)
            .NotEmpty();

        RuleFor(x => x.LessonId)
            .GreaterThan(0);

        RuleFor(x => x.Rating)
            .InclusiveBetween(1, 5);

        RuleFor(x => x.Comment)
            .MaximumLength(1000);
    }
}
