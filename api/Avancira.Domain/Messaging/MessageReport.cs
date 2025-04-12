using Avancira.Domain.Messaging;
using Avancira.Domain.Common;
using Avancira.Domain.Messaging.Events;

namespace Avancira.Domain.Messagings
{
    public class MessageReport : BaseEntity<Guid>
    {
        public Guid MessageId { get; set; }
        public string UserId { get; set; } = default!;
        public string ReportReason { get; set; }
        public DateTimeOffset ReportedAt { get; set; }

        public Message Message { get; set; } = default!;

        public MessageReport(Guid messageId, string userId, string reportReason)
        {
            MessageId = messageId;
            UserId = userId;
            ReportReason = reportReason;
            ReportedAt = DateTimeOffset.UtcNow;
        }

        public static MessageReport Create(Guid messageId, string userId, string reportReason)
        {
            var report = new MessageReport(messageId, userId, reportReason);
            report.QueueDomainEvent(new MessageReportedEvent(report));
            return report;
        }
    }
}
