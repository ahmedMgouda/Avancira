using Avancira.Application.SubjectCategories.Dtos;
using FluentValidation;

namespace Avancira.Application.SubjectCategories.Validators;

public class SubjectCategoryUpdateDtoValidator : AbstractValidator<SubjectCategoryUpdateDto>
{
    public SubjectCategoryUpdateDtoValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("Id must be greater than zero.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(100).WithMessage("Name cannot exceed 100 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(255).WithMessage("Description cannot exceed 255 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.Description));

        // REMOVED: SortOrder validation
        // SortOrder is not editable via update - use Reorder/Move endpoints
    }
}
