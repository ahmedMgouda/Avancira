using Avancira.Application.LessonMaterials.Dtos;
using FluentValidation;

namespace Avancira.Application.LessonMaterials.Validators;

public class LessonMaterialScanUpdateDtoValidator : AbstractValidator<LessonMaterialScanUpdateDto>
{
    public LessonMaterialScanUpdateDtoValidator()
    {
        RuleFor(x => x.MaterialId)
            .GreaterThan(0);

        RuleFor(x => x.ScanResult)
            .MaximumLength(200);
    }
}
