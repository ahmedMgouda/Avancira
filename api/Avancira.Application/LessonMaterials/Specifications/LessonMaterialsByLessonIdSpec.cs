using Ardalis.Specification;
using Avancira.Domain.Lessons;

namespace Avancira.Application.LessonMaterials.Specifications;

public sealed class LessonMaterialsByLessonIdSpec : Specification<LessonMaterial>
{
    public LessonMaterialsByLessonIdSpec(int lessonId)
    {
        Query
            .Where(m => m.LessonId == lessonId)
            .OrderBy(m => m.UploadedAtUtc);
    }
}
