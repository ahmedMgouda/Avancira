using Avancira.Admin.Shared.Notifications;

namespace Avancira.Admin.Infrastructure.Notifications;

public record ConnectionStateChanged(ConnectionState State, string? Message) : INotificationMessage;