using Avancira.Application.Catalog.Dtos;
using Avancira.Domain.Messaging;
using System.Collections.Generic;

public interface IChatService
{
    // Create
    Chat GetOrCreateChat(string studentId, string tutorId, int listingId);
    // Read
    List<ChatDto> GetUserChats(string userId);
    // Update
    bool SendMessage(SendMessageDto messageDto, string senderId);
}

