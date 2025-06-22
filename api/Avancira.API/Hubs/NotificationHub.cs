using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using System.Security.Claims;

namespace Avancira.API.Hubs
{
    public class NotificationHub : Hub
    {
        private static readonly ConcurrentDictionary<string, string> UserConnections = new();
        private readonly ILogger<NotificationHub> _logger;

        public NotificationHub(ILogger<NotificationHub> logger)
        {
            _logger = logger;
        }

        public override Task OnConnectedAsync()
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                UserConnections[userId] = Context.ConnectionId;
                _logger.LogInformation("User {UserId} connected with Connection ID: {ConnectionId}", userId, Context.ConnectionId);
            }

            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(System.Exception? exception)
        {
            var userId = UserConnections.FirstOrDefault(x => x.Value == Context.ConnectionId).Key;
            if (!string.IsNullOrEmpty(userId))
            {
                UserConnections.TryRemove(userId, out _);
                _logger.LogInformation("User {UserId} disconnected with Connection ID: {ConnectionId}", userId, Context.ConnectionId);

                if (exception != null)
                {
                    _logger.LogError(exception, "An error occurred during disconnection for User {UserId}", userId);
                }
            }

            return base.OnDisconnectedAsync(exception);
        }

        public static string GetConnectionId(string userId)
        {
            UserConnections.TryGetValue(userId, out var connectionId);
            return connectionId ?? string.Empty;
        }
    }
}
