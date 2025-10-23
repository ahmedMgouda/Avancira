using Avancira.Application.Messaging;
using Avancira.Application.Messaging.Dtos;
using Avancira.Domain.Messaging;
using Avancira.Domain.Notifications;
using Avancira.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Avancira.Infrastructure.Catalog;

public class ChatService : IChatService
{
    private readonly AvanciraDbContext _dbContext;
    private readonly INotificationService _notificationService;
    private readonly ILogger<ChatService> _logger;

    public ChatService(
        AvanciraDbContext dbContext,
        INotificationService notificationService,
        ILogger<ChatService> logger)
    {
        _dbContext = dbContext;
        _notificationService = notificationService;
        _logger = logger;
    }

    public Chat GetOrCreateChat(string participantA, string participantB, Guid listingId)
    {
        var chat = _dbContext.Chats.FirstOrDefault(c =>
            (c.StudentId == participantA && c.TutorId == participantB) ||
            (c.StudentId == participantB && c.TutorId == participantA));

        if (chat == null)
        {
            chat = Chat.Create(participantA, participantB, listingId, BlockStatus.NotBlocked);
            _dbContext.Chats.Add(chat);
            _dbContext.SaveChanges();
        }

        return chat;
    }

    public List<ChatDto> GetUserChats(string userId)
    {
        var chats = _dbContext.Chats
            .Where(chat => chat.StudentId == userId || chat.TutorId == userId)
            .Select(chat => new { Chat = chat })
            .AsEnumerable()
            .Select(item => BuildChatDto(item.Chat, userId))
            .ToList();

        return chats;
    }

    public ChatDto GetChat(Guid chatId, string userId)
    {
        var chat = _dbContext.Chats
            .FirstOrDefault(chat => chat.Id == chatId && (chat.StudentId == userId || chat.TutorId == userId));

        return chat == null ? new ChatDto() : BuildChatDto(chat, userId);
    }

    public async Task<bool> SendMessageAsync(SendMessageDto messageDto, string senderId)
    {
        if (string.IsNullOrWhiteSpace(messageDto.RecipientId))
        {
            _logger.LogWarning("RecipientId missing when attempting to send a chat message");
            return false;
        }

        var chat = GetOrCreateChat(
            senderId,
            messageDto.RecipientId,
            messageDto.ListingId ?? Guid.Empty);

        chat.AddMessage(senderId, messageDto.RecipientId, messageDto.Content ?? string.Empty);

        _dbContext.Chats.Update(chat);
        await _dbContext.SaveChangesAsync();

        await NotifyRecipientAsync(chat, messageDto, senderId);

        _logger.LogInformation("Message sent from {SenderId} to {RecipientId}", senderId, messageDto.RecipientId);
        return true;
    }

    private ChatDto BuildChatDto(Chat chat, string viewerId)
    {
        var messages = _dbContext.Messages
            .Where(message => message.ChatId == chat.Id)
            .OrderBy(message => message.SentAt)
            .ToList();

        var participantIds = new[] { chat.StudentId, chat.TutorId }
            .Concat(messages.Select(message => message.SenderId))
            .Where(id => !string.IsNullOrEmpty(id))
            .Distinct()
            .ToList();

        var participants = _dbContext.Users
            .Where(user => participantIds.Contains(user.Id))
            .ToDictionary(user => user.Id, user => user);

        var isViewerStudent = chat.StudentId == viewerId;
        var recipientId = isViewerStudent ? chat.TutorId : chat.StudentId;
        var recipient = !string.IsNullOrEmpty(recipientId) && participants.TryGetValue(recipientId, out var value)
            ? value
            : null;

        return new ChatDto
        {
            Id = chat.Id,
            ListingId = chat.ListingId,
            TutorId = chat.TutorId,
            StudentId = chat.StudentId,
            RecipientId = recipientId,
            Details = "Conversation",
            Name = recipient != null ? $"{recipient.FirstName} {recipient.LastName}".Trim() : string.Empty,
            ImageUrl = recipient?.ImageUrl?.ToString() ?? string.Empty,
            LastMessage = messages.LastOrDefault()?.Content ?? "No messages yet",
            Timestamp = messages.LastOrDefault()?.SentAt.DateTime ?? chat.CreatedAt.DateTime,
            Messages = messages.Select(message => new MessageDto
            {
                SentBy = message.SenderId == viewerId ? "me" : "contact",
                SenderId = message.SenderId,
                SenderName = participants.TryGetValue(message.SenderId, out var sender)
                    ? $"{sender.FirstName} {sender.LastName}".Trim()
                    : string.Empty,
                Content = message.Content,
                Timestamp = message.SentAt.UtcDateTime
            }).ToList(),
            MyRole = isViewerStudent ? UserRole.Student : UserRole.Tutor
        };
    }

    private async Task NotifyRecipientAsync(Chat chat, SendMessageDto messageDto, string senderId)
    {
        var recipientId = messageDto.RecipientId;
        if (string.IsNullOrEmpty(recipientId))
        {
            return;
        }

        var sender = await _dbContext.Users.FirstOrDefaultAsync(user => user.Id == senderId);
        var senderName = sender == null
            ? "Someone"
            : $"{sender.FirstName} {sender.LastName}".Trim();

        var preview = messageDto.Content ?? string.Empty;
        if (preview.Length > 50)
        {
            preview = preview[..50] + "...";
        }

        await _notificationService.NotifyAsync(
            recipientId,
            NotificationEvent.NewMessage,
            $"{senderName} sent you a message",
            new
            {
                ChatId = chat.Id,
                SenderId = senderId,
                SenderName = senderName,
                ListingId = chat.ListingId,
                MessagePreview = preview,
                Timestamp = DateTimeOffset.UtcNow
            });
    }
}
