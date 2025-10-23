namespace Avancira.Application.Lessons.Dtos;

public class LessonCompleteDto
{
    public int LessonId { get; set; }
    public TimeSpan? ActualDuration { get; set; }
    public string? SessionSummary { get; set; }
    public string? TutorNotes { get; set; }
}
