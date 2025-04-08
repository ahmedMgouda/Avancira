using Avancira.Application.Storage.File.Dtos;
using FluentValidation;

namespace Avancira.Application.Storage.File.Validators;
public class FileUploadRequestValidator : AbstractValidator<FileUploadDto>
{
    public FileUploadRequestValidator()
    {
        RuleFor(p => p.Name)
            .NotEmpty()
            .MaximumLength(150);

        RuleFor(p => p.Extension)
            .NotEmpty()
            .MaximumLength(5);

        RuleFor(p => p.Data)
            .NotEmpty();
    }
}
