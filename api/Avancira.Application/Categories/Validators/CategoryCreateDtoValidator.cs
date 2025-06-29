using Avancira.Application.Categories.Dtos;
using Avancira.Application.Storage.File.Validators;
using FluentValidation;

namespace Avancira.Application.Categories.Validators
{
    public class CategoryCreateDtoValidator : AbstractValidator<CategoryCreateDto>
    {
        public CategoryCreateDtoValidator()
        {
            RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(100).WithMessage("Name cannot exceed 100 characters.");

            When(x => x.Image != null, () =>
            {
                RuleFor(x => x.Image!)
                    .SetValidator(new FileUploadRequestValidator());
            });
        }
    }
}


