using Avancira.Domain.Common;
using Avancira.Domain.Messaging.Events;

namespace Avancira.Domain.Messaging
{
    public class MessageReport : BaseEntity<Guid>
    {
        public Guid MessageId { get; private set; }
        public string UserId { get; private set; }
        public string ReportReason { get; private set; }
        public DateTimeOffset ReportedAt { get; private set; }

        public Message Message { get; private set; } = default!;

        private MessageReport(Guid messageId, string userId, string reportReason)
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
