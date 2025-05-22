using Avancira.Application.Catalog.Dtos;
using Avancira.Application.Identity.Users.Abstractions;
using Avancira.Application.Persistence;
using Avancira.Domain.Catalog.Enums;
using Avancira.Domain.Messaging;
using Microsoft.Extensions.Logging;

namespace Avancira.Application.Catalog.Chats;

public class ChatService : IChatService
{
    private readonly IRepository<Chat> _chatRepository;
    private readonly IUserService _userService;
    private readonly IFileUploadService _fileUploadService;
    private readonly ILogger<ChatService> _logger;

    public ChatService(
        IRepository<Chat> chatRepository,
        IUserService userService,
        IFileUploadService fileUploadService,
        ILogger<ChatService> logger)
    {
        _chatRepository = chatRepository;
        _userService = userService;
        _fileUploadService = fileUploadService;
        _logger = logger;
    }

    public async Task<Chat> GetOrCreateChatAsync(string studentId, string tutorId, Guid listingId)
    {
        var spec = new ChatByParticipantsSpec(studentId, tutorId, listingId);
        var chat = await _chatRepository.FirstOrDefaultAsync(spec);
        if (chat == null)
        {
            chat = Chat.Create(studentId, tutorId, listingId, BlockStatus.NotBlocked);
            await _chatRepository.AddAsync(chat);
        }
        return chat;
    }

    public async Task<List<ChatDto>> GetUserChatsAsync(string userId)
    {
        var spec = new ChatsByUserSpec(userId);
        var chats = await _chatRepository.ListAsync(spec);
        var result = new List<ChatDto>();

        foreach (var chat in chats)
        {
            var isStudent = chat.StudentId == userId;
            var recipientId = isStudent ? chat.TutorId : chat.StudentId;
            UserDetailDto? recipient = null;
            try
            {
                recipient = await _userService.GetAsync(recipientId, default);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "failed to load user {RecipientId}", recipientId);
            }

            var messages = chat.Messages.OrderByDescending(m => m.SentAt).ToList();
            var latestMessage = messages.FirstOrDefault();

            var senderIds = messages.Select(m => m.SenderId).Distinct().ToList();
            var senders = new Dictionary<string, UserDetailDto>();
            foreach (var id in senderIds)
            {
                try
                {
                    senders[id] = await _userService.GetAsync(id, default);
                }
                catch { }
            }

            result.Add(new ChatDto
            {
                Id = chat.Id,
                ListingId = chat.ListingId,
                TutorId = chat.TutorId,
                StudentId = chat.StudentId,
                RecipientId = recipientId,
                Name = recipient != null ? $"{recipient.FirstName} {recipient.LastName}" : string.Empty,
                ProfileImagePath = recipient?.ImageUrl?.ToString() ?? string.Empty,
                LastMessage = latestMessage?.Content ?? "No messages yet",
                Timestamp = latestMessage?.SentAt.DateTime ?? chat.CreatedAt.DateTime,
                Messages = messages.Select(m => new MessageDto
                {
                    SentBy = m.SenderId == userId ? "me" : "contact",
                    SenderId = m.SenderId,
                    SenderName = senders.TryGetValue(m.SenderId, out var s) ? $"{s.FirstName} {s.LastName}" : string.Empty,
                    Content = m.Content,
                    FilePath = m.FilePath,
                    Timestamp = m.SentAt.DateTime
                }).ToList(),
                MyRole = isStudent ? UserRole.Student : UserRole.Tutor
            });
        }

        return result;
    }

    public async Task<bool> SendMessageAsync(SendMessageDto messageDto, string senderId)
    {
        var chat = await GetOrCreateChatAsync(senderId, messageDto.RecipientId ?? string.Empty, messageDto.ListingId);

        string? filePath = null;
        if (messageDto.File != null)
        {
            filePath = await _fileUploadService.SaveFileAsync(messageDto.File, "chat");
        }

        chat.AddMessage(senderId, messageDto.RecipientId ?? string.Empty, messageDto.Content ?? string.Empty, filePath);
        await _chatRepository.UpdateAsync(chat);
        return true;
    }

    public async Task BlockUserAsync(Guid chatId, string userId)
    {
        var chat = await _chatRepository.GetByIdAsync(chatId);
        if (chat == null) throw new Exception("Chat not found");
        chat.BlockUser(userId);
        await _chatRepository.UpdateAsync(chat);
    }

    public async Task UnblockUserAsync(Guid chatId, string userId)
    {
        var chat = await _chatRepository.GetByIdAsync(chatId);
        if (chat == null) throw new Exception("Chat not found");
        chat.UnblockUser(userId);
        await _chatRepository.UpdateAsync(chat);
    }

    public async Task<List<MessageDto>> SearchMessagesAsync(Guid chatId, string query, string userId)
    {
        var spec = new ChatWithMessagesSpec(chatId);
        var chat = await _chatRepository.FirstOrDefaultAsync(spec);
        if (chat == null) return new List<MessageDto>();

        return chat.Messages
            .Where(m => m.Content.Contains(query, StringComparison.OrdinalIgnoreCase))
            .OrderBy(m => m.SentAt)
            .Select(m => new MessageDto
            {
                SentBy = m.SenderId == userId ? "me" : "contact",
                SenderId = m.SenderId,
                SenderName = string.Empty,
                Content = m.Content,
                FilePath = m.FilePath,
                Timestamp = m.SentAt.DateTime
            }).ToList();
    }

    public async Task<List<string>> GetChatFilesAsync(Guid chatId)
    {
        var spec = new ChatWithMessagesSpec(chatId);
        var chat = await _chatRepository.FirstOrDefaultAsync(spec);
        if (chat == null) return new List<string>();

        return chat.Messages
            .Where(m => m.FilePath != null)
            .OrderBy(m => m.SentAt)
            .Select(m => m.FilePath!)
            .ToList();
    }
}
