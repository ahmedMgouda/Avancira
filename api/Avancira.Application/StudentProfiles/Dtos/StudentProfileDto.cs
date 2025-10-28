using Avancira.Domain.Students;

namespace Avancira.Application.StudentProfiles.Dtos;

public class StudentProfileDto
{
    public string UserId { get; set; } = default!;
    public string? LearningGoal { get; set; }
    public string? CurrentEducationLevel { get; set; }
    public string? School { get; set; }
    public string? Major { get; set; }
    public LearningStyle? PreferredLearningStyle { get; set; }
    public bool HasUsedTrialLesson { get; set; }
    public DateTime? TrialLessonUsedAtUtc { get; set; }
}
