using Avancira.Application.Messaging.Dtos;
using Avancira.Domain.Messaging;
using System.Collections.Generic;

namespace Avancira.Application.Messaging;

public interface IChatService
{
    // Create
    Chat GetOrCreateChat(string studentId, string tutorId);
    // Read
    List<ChatDto> GetUserChats(string userId);
    ChatDto GetChat(Guid chatId, string userId);
    // Update
    Task<bool> SendMessageAsync(SendMessageDto messageDto, string senderId);
}
