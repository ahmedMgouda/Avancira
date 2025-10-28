using Avancira.Domain.Common;
using Avancira.Domain.Common.Contracts;

namespace Avancira.Domain.Tutors;

public class TutorAvailability : BaseEntity<int>, IAggregateRoot
{
    private TutorAvailability()
    {
    }

    private TutorAvailability(
        string tutorId,
        DayOfWeek dayOfWeek,
        TimeSpan startTime,
        TimeSpan endTime)
    {
        TutorId = tutorId;
        DayOfWeek = dayOfWeek;
        StartTime = startTime;
        EndTime = endTime;
    }

    public string TutorId { get; private set; } = default!;
    public TutorProfile Tutor { get; private set; } = default!;
    public DayOfWeek DayOfWeek { get; private set; }
    public TimeSpan StartTime { get; private set; }
    public TimeSpan EndTime { get; private set; }

    public static TutorAvailability Create(
        string tutorId,
        DayOfWeek dayOfWeek,
        TimeSpan startTime,
        TimeSpan endTime) =>
        new(tutorId, dayOfWeek, startTime, endTime);

    public void Update(TimeSpan startTime, TimeSpan endTime)
    {
        StartTime = startTime;
        EndTime = endTime;
    }

    public void Update(DayOfWeek dayOfWeek, TimeSpan startTime, TimeSpan endTime)
    {
        DayOfWeek = dayOfWeek;
        StartTime = startTime;
        EndTime = endTime;
    }
}
