using Avancira.Application.Categories.Dtos;
using Avancira.Application.Storage.File.Validators;
using FluentValidation;

namespace Avancira.Application.Categories.Validators
{
    public class CategoryUpdateDtoValidator : AbstractValidator<CategoryUpdateDto>
    {
        public CategoryUpdateDtoValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty().WithMessage("Id is required.");

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name is required.")
                .MaximumLength(100).WithMessage("Name cannot exceed 100 characters.");

            When(x => x.Image != null && !x.DeleteCurrentImage, () =>
            {
                RuleFor(x => x.Image!)
                    .SetValidator(new FileUploadRequestValidator());
            });
        }

    }
}
