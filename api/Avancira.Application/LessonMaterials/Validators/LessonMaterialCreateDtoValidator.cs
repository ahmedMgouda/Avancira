using Avancira.Application.LessonMaterials.Dtos;
using FluentValidation;

namespace Avancira.Application.LessonMaterials.Validators;

public class LessonMaterialCreateDtoValidator : AbstractValidator<LessonMaterialCreateDto>
{
    public LessonMaterialCreateDtoValidator()
    {
        RuleFor(x => x.LessonId)
            .GreaterThan(0);

        RuleFor(x => x.UploadedByUserId)
            .NotEmpty();

        RuleFor(x => x.FileName)
            .NotEmpty()
            .MaximumLength(255);

        RuleFor(x => x.FileType)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.FileUrl)
            .NotEmpty()
            .MaximumLength(2000);

        RuleFor(x => x.Description)
            .MaximumLength(500);

        RuleFor(x => x.FileSizeBytes)
            .GreaterThanOrEqualTo(0);
    }
}
