using Avancira.Admin.Shared.Notifications;

namespace Avancira.Admin.Infrastructure.Notifications;

public interface INotificationPublisher
{
    Task PublishAsync(INotificationMessage notification);
}