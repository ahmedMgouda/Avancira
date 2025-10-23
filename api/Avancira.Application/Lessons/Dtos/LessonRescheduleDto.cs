namespace Avancira.Application.Lessons.Dtos;

public class LessonRescheduleDto
{
    public int LessonId { get; set; }
    public DateTime NewScheduledAtUtc { get; set; }
    public int DurationMinutes { get; set; }
    public string RequestedBy { get; set; } = string.Empty;
}
