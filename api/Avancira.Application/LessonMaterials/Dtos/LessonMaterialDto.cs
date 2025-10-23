using Avancira.Domain.Lessons;

namespace Avancira.Application.LessonMaterials.Dtos;

public class LessonMaterialDto
{
    public int Id { get; set; }
    public int LessonId { get; set; }
    public string UploadedByUserId { get; set; } = default!;
    public string FileName { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string FileUrl { get; set; } = string.Empty;
    public MaterialType MaterialType { get; set; }
    public string? Description { get; set; }
    public bool IsSharedWithStudent { get; set; }
    public DateTime UploadedAtUtc { get; set; }
    public ScanStatus ScanStatus { get; set; }
    public string? ScanResult { get; set; }
}
