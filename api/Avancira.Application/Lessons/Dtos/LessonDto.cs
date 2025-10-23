using System;
using Avancira.Domain.Lessons;

namespace Avancira.Application.Lessons.Dtos;

public class LessonDto
{
    public int Id { get; set; }
    public string StudentId { get; set; } = default!;
    public string TutorId { get; set; } = default!;
    public int ListingId { get; set; }
    public DateTime ScheduledAtUtc { get; set; }
    public TimeSpan Duration { get; set; }
    public LessonStatus Status { get; set; }
    public decimal FinalPrice { get; set; }
    public DateTime BookedAtUtc { get; set; }
    public DateTime? ConfirmedAtUtc { get; set; }
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public DateTime? CanceledAtUtc { get; set; }
    public string? CanceledBy { get; set; }
    public string? CancellationReason { get; set; }
    public string? MeetingUrl { get; set; }
    public string? MeetingId { get; set; }
    public string? MeetingPassword { get; set; }
    public string? TutorNotes { get; set; }
    public string? SessionSummary { get; set; }
    public TimeSpan? ActualDuration { get; set; }
    public int? RescheduledFromLessonId { get; set; }
    public int RescheduleCount { get; set; }
}
