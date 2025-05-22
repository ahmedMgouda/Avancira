using Avancira.Application.Catalog.Dtos;
using Avancira.Domain.Messaging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public interface IChatService
{
    // Create
    Task<Chat> GetOrCreateChatAsync(string studentId, string tutorId, Guid listingId);
    // Read
    Task<List<ChatDto>> GetUserChatsAsync(string userId);
    // Update
    Task<bool> SendMessageAsync(SendMessageDto messageDto, string senderId);

    Task BlockUserAsync(Guid chatId, string userId);
    Task UnblockUserAsync(Guid chatId, string userId);
    Task<List<MessageDto>> SearchMessagesAsync(Guid chatId, string query, string userId);
    Task<List<string>> GetChatFilesAsync(Guid chatId);
}

