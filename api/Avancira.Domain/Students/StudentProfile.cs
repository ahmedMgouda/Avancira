using Avancira.Domain.Common;
using Avancira.Domain.Common.Contracts;
using Avancira.Domain.Lessons;
using Avancira.Domain.Reviews;

namespace Avancira.Domain.Students;

public class StudentProfile : BaseEntity<string>, IAggregateRoot
{
    private StudentProfile()
    {
    }

    private StudentProfile(string userId)
    {
        Id = userId;
    }

    public string UserId => Id;
    public string? LearningGoal { get; private set; }
    public string? CurrentEducationLevel { get; private set; }
    public string? School { get; private set; }
    public string? Major { get; private set; }
    public LearningStyle? PreferredLearningStyle { get; private set; }
    public bool HasUsedTrialLesson { get; private set; }
    public DateTime? TrialLessonUsedAtUtc { get; private set; }

    public ICollection<Lesson> Lessons { get; private set; } = new HashSet<Lesson>();
    public ICollection<StudentReview> Reviews { get; private set; } = new HashSet<StudentReview>();

    public static StudentProfile Create(string userId) => new(userId);

    public void UpdateLearningPreferences(
        string? learningGoal,
        string? currentEducationLevel,
        string? school,
        string? major,
        LearningStyle? preferredLearningStyle)
    {
        LearningGoal = learningGoal;
        CurrentEducationLevel = currentEducationLevel;
        School = school;
        Major = major;
        PreferredLearningStyle = preferredLearningStyle;
    }

    public void MarkTrialLessonUsed()
    {
        HasUsedTrialLesson = true;
        TrialLessonUsedAtUtc = DateTime.UtcNow;
    }
}
