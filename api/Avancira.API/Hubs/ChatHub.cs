using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Avancira.API.Hubs;

[Authorize]
public class ChatHub : Hub
{
    public const string HubUrl = "/hubs/chat";

    public Task JoinChat(string chatId)
        => Groups.AddToGroupAsync(Context.ConnectionId, chatId);
}
