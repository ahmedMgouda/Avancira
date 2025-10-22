using Avancira.Application.Subjects.Dtos;
using FluentValidation;

namespace Avancira.Application.Subjects.Validators;

public class SubjectUpdateDtoValidator : AbstractValidator<SubjectUpdateDto>
{
    public SubjectUpdateDtoValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("Id must be greater than zero.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(100).WithMessage("Name cannot exceed 100 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(255).WithMessage("Description cannot exceed 255 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.Description));

        RuleFor(x => x.IconUrl)
            .MaximumLength(255).WithMessage("Icon URL cannot exceed 255 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.IconUrl));

        RuleFor(x => x.SortOrder)
            .GreaterThanOrEqualTo(0).WithMessage("Sort order cannot be negative.");

        RuleFor(x => x.CategoryId)
            .GreaterThan(0).WithMessage("CategoryId must be greater than zero.");
    }
}
