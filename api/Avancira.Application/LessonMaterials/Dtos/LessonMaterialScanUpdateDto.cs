using Avancira.Domain.Lessons;

namespace Avancira.Application.LessonMaterials.Dtos;

public class LessonMaterialScanUpdateDto
{
    public int MaterialId { get; set; }
    public ScanStatus ScanStatus { get; set; }
    public string? ScanResult { get; set; }
}
