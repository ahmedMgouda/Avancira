using Avancira.Domain.Catalog;
using Avancira.Domain.Common;
using Avancira.Domain.Lessons.Events;
using Avancira.Domain.Transactions;
using Backend.Domain.Lessons;
using Backend.Domain.PromoCodes;

public class Lesson : AuditableEntity
{
    public DateTime Date { get; private set; }
    public TimeSpan Duration { get; private set; }
    public decimal HourlyRate { get; private set; } = default!;
    public decimal? OfferedPrice { get; private set; }
    public string StudentId { get; private set; } = default!;
    public Guid ListingId { get; private set; }
    public Guid TransactionId { get; private set; }
    public bool IsStudentInitiated { get; private set; }
    public LessonStatus Status { get; private set; } = LessonStatus.Proposed;
    public string? MeetingToken { get; private set; }
    public string? MeetingRoomName { get; private set; }
    public string? MeetingUrl { get; private set; }
    public Guid? PromoCodeId { get; private set; }
    public string? PromoCodeValue { get; private set; }
    public decimal? PromoDiscount { get; private set; }
    public virtual PromoCode? PromoCode { get; private set; }
    public virtual Listing Listing { get; private set; } = default!;
    public virtual Transaction Transaction { get; private set; } = default!;

    public decimal ActualPrice =>
        Math.Round((decimal)Duration.TotalHours * HourlyRate, 2);

    private Lesson() { }

    public static Lesson Create(
        DateTime date,
        TimeSpan duration,
        decimal hourlyRate,
        decimal offeredPrice,
        string studentId,
        Guid listingId,
        Guid transactionId,
        bool isStudentInitiated)
    {

        var lesson = new Lesson
        {
            Date = date,
            Duration = duration,
            HourlyRate = hourlyRate,
            OfferedPrice = offeredPrice,
            StudentId = studentId,
            ListingId = listingId,
            TransactionId = transactionId,
            IsStudentInitiated = isStudentInitiated,
            Status = LessonStatus.Proposed
        };

        lesson.QueueDomainEvent(new LessonCreatedEvent(lesson));

        return lesson;
    }

    public void ChangeStatus(LessonStatus newStatus)
    {
        var oldStatus = Status;
        Status = newStatus;

        if (oldStatus != newStatus)
        {
            QueueDomainEvent(new LessonStatusChangedEvent(this, oldStatus, newStatus));
        }
    }
}
