using Avancira.Domain.Catalog.Enums;
using System.Threading.Tasks;

public interface INotificationService
{
    Task NotifyAsync<T>(NotificationEvent eventType, T eventData);
    Task NotifyAsync(string userId, NotificationEvent eventName, string message, object? data = null);
}

