using Avancira.Domain.Catalog.Enums;
using Avancira.Domain.Common;
using Avancira.Domain.Common.Contracts;

namespace Avancira.Domain.Notifications;

public class Notification : AuditableEntity, IAggregateRoot
{
    public string UserId { get; set; } = default!;
    public NotificationEvent EventName { get; set; } = default!;
    public string Message { get; set; } = default!;
    public string? Data { get; set; }
    public bool IsRead { get; set; } = false;
    public DateTime? ReadAt { get; set; }
}
