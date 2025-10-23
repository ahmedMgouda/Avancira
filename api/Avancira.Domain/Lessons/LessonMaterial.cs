using Avancira.Domain.Common;
using Avancira.Domain.Common.Contracts;

namespace Avancira.Domain.Lessons;

public class LessonMaterial : BaseEntity<int>, IAggregateRoot
{
    private LessonMaterial()
    {
    }

    private LessonMaterial(
        int lessonId,
        string uploadedByUserId,
        string fileName,
        string fileType,
        long fileSizeBytes,
        string fileUrl,
        MaterialType materialType,
        string? description,
        bool isSharedWithStudent)
    {
        LessonId = lessonId;
        UploadedByUserId = uploadedByUserId;
        FileName = fileName;
        FileType = fileType;
        FileSizeBytes = fileSizeBytes;
        FileUrl = fileUrl;
        MaterialType = materialType;
        Description = description;
        IsSharedWithStudent = isSharedWithStudent;
        UploadedAtUtc = DateTime.UtcNow;
        ScanStatus = ScanStatus.NotScanned;
    }

    public int LessonId { get; private set; }
    public Lesson Lesson { get; private set; } = default!;
    public string UploadedByUserId { get; private set; } = default!;
    public string FileName { get; private set; } = string.Empty;
    public string FileType { get; private set; } = string.Empty;
    public long FileSizeBytes { get; private set; }
    public string FileUrl { get; private set; } = string.Empty;
    public MaterialType MaterialType { get; private set; }
    public string? Description { get; private set; }
    public bool IsSharedWithStudent { get; private set; }
    public DateTime UploadedAtUtc { get; private set; }
    public ScanStatus ScanStatus { get; private set; }
    public string? ScanResult { get; private set; }

    public bool IsScanned => ScanStatus is ScanStatus.Clean or ScanStatus.Infected or ScanStatus.Error;
    public bool IsSafe => ScanStatus == ScanStatus.Clean;

    public static LessonMaterial Create(
        int lessonId,
        string uploadedByUserId,
        string fileName,
        string fileType,
        long fileSizeBytes,
        string fileUrl,
        MaterialType materialType,
        string? description,
        bool isSharedWithStudent) =>
        new(lessonId, uploadedByUserId, fileName, fileType, fileSizeBytes, fileUrl, materialType, description, isSharedWithStudent);

    public void ShareWithStudent(bool isShared)
    {
        IsSharedWithStudent = isShared;
    }

    public void UpdateScanStatus(ScanStatus status, string? result)
    {
        ScanStatus = status;
        ScanResult = result;
    }
}
