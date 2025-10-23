using Avancira.Domain.Common;
using Avancira.Domain.Common.Contracts;
using Avancira.Domain.Lessons;
using Avancira.Domain.Students;

namespace Avancira.Domain.Reviews;

public class StudentReview : BaseEntity<int>, IAggregateRoot
{
    private StudentReview()
    {
    }

    private StudentReview(
        string studentId,
        int lessonId,
        int rating,
        string? comment,
        int? communicationRating,
        int? knowledgeRating,
        int? professionalismRating,
        int? valueRating)
    {
        StudentId = studentId;
        LessonId = lessonId;
        Rating = rating;
        Comment = comment;
        CommunicationRating = communicationRating;
        KnowledgeRating = knowledgeRating;
        ProfessionalismRating = professionalismRating;
        ValueRating = valueRating;
        CreatedOnUtc = DateTime.UtcNow;
    }

    public string StudentId { get; private set; } = default!;
    public StudentProfile Student { get; private set; } = default!;
    public int LessonId { get; private set; }
    public Lesson Lesson { get; private set; } = default!;
    public int Rating { get; private set; }
    public string? Comment { get; private set; }
    public int? CommunicationRating { get; private set; }
    public int? KnowledgeRating { get; private set; }
    public int? ProfessionalismRating { get; private set; }
    public int? ValueRating { get; private set; }
    public bool IsApproved { get; private set; } = true;
    public bool IsFlagged { get; private set; }
    public string? FlagReason { get; private set; }
    public string? ModeratedByAdminId { get; private set; }
    public DateTime? ModeratedAtUtc { get; private set; }
    public string? TutorResponse { get; private set; }
    public DateTime? TutorRespondedAtUtc { get; private set; }
    public int HelpfulVotes { get; private set; }
    public int NotHelpfulVotes { get; private set; }
    public bool IsEdited { get; private set; }
    public DateTime? EditedAtUtc { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }

    public static StudentReview Create(
        string studentId,
        int lessonId,
        int rating,
        string? comment,
        int? communicationRating,
        int? knowledgeRating,
        int? professionalismRating,
        int? valueRating) =>
        new(studentId, lessonId, rating, comment, communicationRating, knowledgeRating, professionalismRating, valueRating);

    public void Flag(string? reason)
    {
        IsFlagged = true;
        FlagReason = reason;
    }

    public void Moderate(string adminId, bool isApproved, string? reason)
    {
        ModeratedByAdminId = adminId;
        ModeratedAtUtc = DateTime.UtcNow;
        IsApproved = isApproved;
        FlagReason = reason;
    }

    public void Respond(string response)
    {
        TutorResponse = response;
        TutorRespondedAtUtc = DateTime.UtcNow;
    }

    public void UpdateComment(string? comment, int rating, int? communicationRating, int? knowledgeRating, int? professionalismRating, int? valueRating)
    {
        Comment = comment;
        Rating = rating;
        CommunicationRating = communicationRating;
        KnowledgeRating = knowledgeRating;
        ProfessionalismRating = professionalismRating;
        ValueRating = valueRating;
        IsEdited = true;
        EditedAtUtc = DateTime.UtcNow;
    }

    public void VoteHelpful(bool helpful)
    {
        if (helpful)
        {
            HelpfulVotes++;
        }
        else
        {
            NotHelpfulVotes++;
        }
    }
}
