using Avancira.Application.StudentProfiles;
using Avancira.Domain.Common;
using Avancira.Domain.Common.Contracts;
using Avancira.Domain.Lessons;
using Avancira.Domain.Reviews;

namespace Avancira.Domain.Students;

public class StudentProfile : BaseEntity<string>, IAggregateRoot
{
    private StudentProfile() { }

    private StudentProfile(string userId)
    {
        Id = userId;
        CreatedOnUtc = DateTime.UtcNow;

        CanBook = false;
        SubscriptionStatus = StudentSubscriptionStatus.None;
    }

    public string UserId => Id;

    public string? LearningGoal { get; private set; }
    public string? CurrentEducationLevel { get; private set; }
    public string? School { get; private set; }
    public string? Major { get; private set; }
    public LearningStyle? PreferredLearningStyle { get; private set; }

    public bool HasUsedTrialLesson { get; private set; }
    public DateTime? TrialLessonUsedAtUtc { get; private set; }

    public bool CanBook { get; private set; }
    public StudentSubscriptionStatus SubscriptionStatus { get; private set; }

    /// <summary>
    /// Value object representing the current subscription period.
    /// </summary>
    public SubscriptionPeriod? SubscriptionPeriod { get; private set; }

    public DateTime CreatedOnUtc { get; private set; }

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

    public void ActivateSubscription(DateTime startUtc, DateTime endUtc)
    {
        SubscriptionPeriod = new SubscriptionPeriod(startUtc, endUtc);
        SubscriptionStatus = StudentSubscriptionStatus.Active;
        CanBook = true;
    }

    public void StartTrial(DateTime startUtc, DateTime endUtc)
    {
        SubscriptionPeriod = new SubscriptionPeriod(startUtc, endUtc);
        SubscriptionStatus = StudentSubscriptionStatus.Trial;
        CanBook = true;
    }

    public void MarkPaymentPastDue()
    {
        SubscriptionStatus = StudentSubscriptionStatus.PastDue;
        CanBook = true;
    }

    public void SuspendSubscription()
    {
        SubscriptionStatus = StudentSubscriptionStatus.Suspended;
        CanBook = false;
    }

    public void CancelSubscription(DateTime endUtc)
    {
        SubscriptionPeriod = SubscriptionPeriod is not null
            ? new SubscriptionPeriod(SubscriptionPeriod.StartUtc, endUtc)
            : new SubscriptionPeriod(DateTime.UtcNow, endUtc);

        SubscriptionStatus = StudentSubscriptionStatus.Cancelled;
        CanBook = true;
    }

    public void ExpireSubscription()
    {
        SubscriptionStatus = StudentSubscriptionStatus.Expired;
        CanBook = false;
    }

    public void ResetSubscription()
    {
        SubscriptionStatus = StudentSubscriptionStatus.None;
        SubscriptionPeriod = null;
        CanBook = false;
    }
}
