using Avancira.Application.Catalog.Dtos;
using Avancira.Domain.Messaging;
using System.Collections.Generic;

namespace Avancira.Application.Messaging;

public interface IChatService
{
    // Create
    Chat GetOrCreateChat(string studentId, string tutorId, Guid listingId);
    // Read
    List<ChatDto> GetUserChats(string userId);
    ChatDto GetChat(Guid chatId, string userId);
    // Update
    Task<bool> SendMessageAsync(SendMessageDto messageDto, string senderId);
}
