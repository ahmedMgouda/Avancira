using System;
using System.Collections.Generic;
using Avancira.Domain.Common;
using Avancira.Domain.Common.Contracts;
using Avancira.Domain.Reviews;
using Avancira.Domain.Students;
using Avancira.Domain.Tutors;

namespace Avancira.Domain.Lessons;

public class Lesson : BaseEntity<int>, IAggregateRoot
{
    private Lesson()
    {
    }

    private Lesson(
        string studentId,
        string tutorId,
        int listingId,
        DateTime scheduledAtUtc,
        TimeSpan duration,
        LessonStatus status,
        decimal finalPrice,
        bool instantBooking)
    {
        StudentId = studentId;
        TutorId = tutorId;
        ListingId = listingId;
        ScheduledAtUtc = scheduledAtUtc;
        Duration = duration;
        Status = instantBooking ? LessonStatus.Scheduled : status;
        FinalPrice = finalPrice;
        BookedAtUtc = DateTime.UtcNow;
    }

    public string StudentId { get; private set; } = default!;
    public StudentProfile Student { get; private set; } = default!;
    public string TutorId { get; private set; } = default!;
    public TutorProfile Tutor { get; private set; } = default!;
    public int ListingId { get; private set; }
    public Listing Listing { get; private set; } = default!;
    public DateTime ScheduledAtUtc { get; private set; }
    public TimeSpan Duration { get; private set; }
    public LessonStatus Status { get; private set; }
    public decimal FinalPrice { get; private set; }
    public DateTime BookedAtUtc { get; private set; }
    public DateTime? ConfirmedAtUtc { get; private set; }
    public DateTime? StartedAtUtc { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }
    public DateTime? CanceledAtUtc { get; private set; }
    public string? CanceledBy { get; private set; }
    public string? CancellationReason { get; private set; }
    public string? MeetingUrl { get; private set; }
    public string? MeetingId { get; private set; }
    public string? MeetingPassword { get; private set; }
    public string? TutorNotes { get; private set; }
    public string? SessionSummary { get; private set; }
    public TimeSpan? ActualDuration { get; private set; }
    public int? RescheduledFromLessonId { get; private set; }
    public Lesson? RescheduledFromLesson { get; private set; }
    public int RescheduleCount { get; private set; }
    public StudentReview? Review { get; private set; }

    public ICollection<LessonMaterial> Materials { get; private set; } = new HashSet<LessonMaterial>();

    public static Lesson Create(
        string studentId,
        string tutorId,
        int listingId,
        DateTime scheduledAtUtc,
        TimeSpan duration,
        LessonStatus status,
        decimal finalPrice,
        bool instantBooking) =>
        new(studentId, tutorId, listingId, scheduledAtUtc, duration, status, finalPrice, instantBooking);

    public void Confirm()
    {
        Status = LessonStatus.Scheduled;
        ConfirmedAtUtc = DateTime.UtcNow;
    }

    public void Start()
    {
        Status = LessonStatus.InProgress;
        StartedAtUtc = DateTime.UtcNow;
    }

    public void Complete(TimeSpan? actualDuration, string? sessionSummary, string? tutorNotes)
    {
        Status = LessonStatus.Completed;
        CompletedAtUtc = DateTime.UtcNow;
        ActualDuration = actualDuration;
        SessionSummary = sessionSummary;
        TutorNotes = tutorNotes;
    }

    public void Cancel(string canceledBy, string? reason)
    {
        Status = LessonStatus.Canceled;
        CanceledAtUtc = DateTime.UtcNow;
        CanceledBy = canceledBy;
        CancellationReason = reason;
    }

    public void RescheduleTo(Lesson newLesson)
    {
        Status = LessonStatus.Rescheduled;
        RescheduledFromLessonId = newLesson.Id;
        RescheduleCount++;
    }

    public void MarkRescheduled(int newLessonId)
    {
        Status = LessonStatus.Rescheduled;
        RescheduledFromLessonId = newLessonId;
        RescheduleCount++;
    }

    public void UpdateMeetingDetails(string? meetingUrl, string? meetingId, string? meetingPassword)
    {
        MeetingUrl = meetingUrl;
        MeetingId = meetingId;
        MeetingPassword = meetingPassword;
    }
}
