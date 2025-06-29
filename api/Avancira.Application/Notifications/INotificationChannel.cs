using Avancira.Domain.Notifications;

namespace Avancira.Application.Messaging;

public interface INotificationChannel
{
    Task SendAsync(string userId, Notification notification, CancellationToken cancellationToken = default);
}
