using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Avancira.Application.Messaging;
using Avancira.Domain.Messaging;

namespace Avancira.Infrastructure.Messaging;

public class SignalRNotificationChannel : INotificationChannel
{
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly ILogger<SignalRNotificationChannel> _logger;

    public SignalRNotificationChannel(
        IHubContext<NotificationHub> hubContext,
        ILogger<SignalRNotificationChannel> logger
    )
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task SendAsync(string userId, Notification notification, CancellationToken cancellationToken = default)
    {
        var connectionId = NotificationHub.GetConnectionId(userId);
        if (!string.IsNullOrEmpty(connectionId))
        {
            await _hubContext.Clients.Client(connectionId).SendAsync("ReceiveNotification", notification, cancellationToken);
            _logger.LogInformation("SignalR notification sent to user {UserId} with connection {ConnectionId}. Event: {EventName}", 
                userId, connectionId, notification.EventName);
        }
        else
        {
            _logger.LogWarning("SignalR: User {UserId} is not connected. Event: {EventName}", userId, notification.EventName);
        }
    }
}
