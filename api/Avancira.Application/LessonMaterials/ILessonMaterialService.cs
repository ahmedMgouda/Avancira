using Avancira.Application.LessonMaterials.Dtos;

namespace Avancira.Application.LessonMaterials;

public interface ILessonMaterialService
{
    Task<IReadOnlyCollection<LessonMaterialDto>> GetByLessonIdAsync(int lessonId, CancellationToken cancellationToken = default);
    Task<LessonMaterialDto> CreateAsync(LessonMaterialCreateDto request, CancellationToken cancellationToken = default);
    Task<LessonMaterialDto> UpdateScanStatusAsync(LessonMaterialScanUpdateDto request, CancellationToken cancellationToken = default);
}
