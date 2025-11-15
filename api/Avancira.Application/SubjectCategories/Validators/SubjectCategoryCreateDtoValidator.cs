using Avancira.Application.SubjectCategories.Dtos;
using FluentValidation;

namespace Avancira.Application.SubjectCategories.Validators;

public class SubjectCategoryCreateDtoValidator : AbstractValidator<SubjectCategoryCreateDto>
{
    public SubjectCategoryCreateDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(100).WithMessage("Name cannot exceed 100 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(255).WithMessage("Description cannot exceed 255 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.Description));

        // NEW: Validate InsertPosition
        RuleFor(x => x.InsertPosition)
            .Must(position => position == null || 
                             position.ToLower() == "start" || 
                             position.ToLower() == "end" || 
                             position.ToLower() == "custom")
            .WithMessage("InsertPosition must be 'start', 'end', or 'custom'.");

        // NEW: Validate CustomPosition (required when InsertPosition is "custom")
        RuleFor(x => x.CustomPosition)
            .GreaterThan(0).WithMessage("CustomPosition must be greater than 0.")
            .When(x => x.InsertPosition?.ToLower() == "custom");

        // REMOVED: SortOrder validation (auto-assigned by backend)
    }
}
