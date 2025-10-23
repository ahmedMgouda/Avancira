using System;

namespace Avancira.Application.Lessons.Dtos;

public class LessonCreateDto
{
    public string StudentId { get; set; } = default!;
    public string TutorId { get; set; } = default!;
    public int ListingId { get; set; }
    public DateTime ScheduledAtUtc { get; set; }
    public int DurationMinutes { get; set; }
    public bool UseTrialLesson { get; set; }
    public bool InstantBooking { get; set; }
    public string? MeetingUrl { get; set; }
    public string? MeetingId { get; set; }
    public string? MeetingPassword { get; set; }
}
