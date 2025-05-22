using Avancira.Application.Catalog.Dtos;
using Avancira.Domain.Catalog.Enums;
using Avancira.Domain.Messaging;
using Avancira.Infrastructure.Persistence;
using Avancira.Application.Persistence;
using Avancira.Application.Catalog.Chats;
using Microsoft.AspNetCore.SignalR;
using Avancira.API.Hubs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

namespace Avancira.Infrastructure.Catalog
{
#if false
    public class ChatService : IChatService
    {
        private readonly AvanciraDbContext _dbContext;
        private readonly INotificationService _notificationService;
        private readonly ILogger<ChatService> _logger;

        public ChatService(AvanciraDbContext dbContext, INotificationService notificationService, ILogger<ChatService> logger)
        {
            _dbContext = dbContext;
            _notificationService = notificationService;
            _logger = logger;
        }

        public Chat GetOrCreateChat(string studentId, string tutorId, Guid listingId)
        {
            // Business logic to send messages
            var chat = _dbContext.Chats.FirstOrDefault(c => c.ListingId == listingId &&
                ((c.StudentId == studentId && c.TutorId == tutorId) || (c.StudentId == tutorId && c.TutorId == studentId)));

            if (chat == null)
            {
                chat = Chat.Create(studentId, tutorId, listingId, BlockStatus.NotBlocked);
                _dbContext.Chats.Add(chat);
                _dbContext.SaveChanges();
            }

            return chat;
        }

        // TODO: Pagination.
        public List<ChatDto> GetUserChats(string userId)
        {
            var result = (from chat in _dbContext.Chats
                          where chat.StudentId == userId || chat.TutorId == userId
                          join student in _dbContext.Users on chat.StudentId equals student.Id
                          join tutor in _dbContext.Users on chat.TutorId equals tutor.Id
                          join listing in _dbContext.Listings on chat.ListingId equals listing.Id into listingGroup
                          from listing in listingGroup.DefaultIfEmpty()
                          join listingCategory in _dbContext.ListingCategories on listing.Id equals listingCategory.ListingId into lcGroup
                          from lc in lcGroup.DefaultIfEmpty()
                          join category in _dbContext.Categories on lc.CategoryId equals category.Id into categoryGroup
                          from cat in categoryGroup.DefaultIfEmpty()
                          select new
                          {
                              Chat = chat,
                              Student = student,
                              Tutor = tutor,
                              Listing = listing,
                              CategoryName = cat.Name,
                          })
                .AsEnumerable() // switch to in-memory to get messages and senders
                .Select(x =>
                {
                    var messages = _dbContext.Messages
                        .Where(m => m.ChatId == x.Chat.Id)
                        .OrderByDescending(m => m.SentAt)
                        .ToList();

                    var senderIds = messages.Select(m => m.SenderId).Distinct().ToList();

                    var senders = _dbContext.Users
                        .Where(u => senderIds.Contains(u.Id))
                        .ToDictionary(u => u.Id, u => u);

                    var latestMessage = messages.FirstOrDefault();
                    var isStudent = x.Chat.StudentId == userId;
                    var recipient = isStudent ? x.Tutor : x.Student;

                    return new ChatDto
                    {
                        Id = x.Chat.Id,
                        ListingId = x.Chat.ListingId,
                        TutorId = x.Chat.TutorId,
                        StudentId = x.Chat.StudentId,
                        RecipientId = isStudent ? x.Tutor.Id : x.Student.Id,
                        Details = !string.IsNullOrEmpty(x.CategoryName)
                            ? $"{x.CategoryName} {(isStudent ? "Tutor" : "Student")}"
                            : "No lesson category",
                        Name = $"{recipient.FirstName} {recipient.LastName}",
                        ProfileImagePath = recipient.ImageUrl?.ToString() ?? "",
                        LastMessage = latestMessage?.Content ?? "No messages yet",
                        Timestamp = latestMessage?.SentAt.DateTime ?? x.Chat.CreatedAt.DateTime,
                        Messages = messages.Select(m => new MessageDto
                        {
                            SentBy = m.SenderId == userId ? "me" : "contact",
                            SenderId = m.SenderId,
                            SenderName = senders.TryGetValue(m.SenderId, out var s) ? $"{s.FirstName} {s.LastName}" : "",
                            Content = m.Content,
                            Timestamp = m.SentAt.DateTime,
                        }).ToList(),
                        MyRole = isStudent ? UserRole.Student : UserRole.Tutor
                    };
                })
                .ToList();

            return result;
        }


        public bool SendMessage(SendMessageDto messageDto, string senderId)
        {
            var chat = GetOrCreateChat(senderId, messageDto.RecipientId ?? string.Empty, messageDto.ListingId);

            chat.AddMessage(senderId, messageDto.RecipientId ?? string.Empty, messageDto.Content ?? string.Empty);

            _dbContext.Chats.Update(chat);
            _dbContext.SaveChanges();


            // Retrieve sender's name
            //var sender = _dbContext.Users.FirstOrDefault(u => u.Id == senderId);
            //var senderName = sender?.FullName ?? "Someone";
            //var eventData = new NewMessageEvent
            //{
            //    ChatId = chat.Id,
            //    SenderId = senderId,
            //    RecipientId = messageDto.RecipientId,
            //    ListingId = messageDto.ListingId,
            //    Content = messageDto.Content,
            //    Timestamp = message.SentAt,
            //    SenderName = senderName
            //};

            //_notificationService.NotifyAsync(NotificationEvent.NewMessage, eventData);
            return true;
        }
    }
#endif
}

    public class ChatService : IChatService
    {
        private readonly IRepository<Chat> _chatRepository;
        private readonly AvanciraDbContext _dbContext;
        private readonly ILogger<ChatService> _logger;
        private readonly IFileUploadService _fileUploadService;
        private readonly IHubContext<ChatHub> _hubContext;

        public ChatService(
            IRepository<Chat> chatRepository,
            AvanciraDbContext dbContext,
            ILogger<ChatService> logger,
            IFileUploadService fileUploadService,
            IHubContext<ChatHub> hubContext)
        {
            _chatRepository = chatRepository;
            _dbContext = dbContext;
            _logger = logger;
            _fileUploadService = fileUploadService;
            _hubContext = hubContext;
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

        // TODO: Pagination
        public async Task<List<ChatDto>> GetUserChatsAsync(string userId)
        {
            var result = (from chat in _dbContext.Chats
                          where chat.StudentId == userId || chat.TutorId == userId
                          join student in _dbContext.Users on chat.StudentId equals student.Id
                          join tutor in _dbContext.Users on chat.TutorId equals tutor.Id
                          join listing in _dbContext.Listings on chat.ListingId equals listing.Id into listingGroup
                          from listing in listingGroup.DefaultIfEmpty()
                          join listingCategory in _dbContext.ListingCategories on listing.Id equals listingCategory.ListingId into lcGroup
                          from lc in lcGroup.DefaultIfEmpty()
                          join category in _dbContext.Categories on lc.CategoryId equals category.Id into categoryGroup
                          from cat in categoryGroup.DefaultIfEmpty()
                          select new
                          {
                              Chat = chat,
                              Student = student,
                              Tutor = tutor,
                              Listing = listing,
                              CategoryName = cat.Name,
                          })
                .AsEnumerable()
                .Select(x =>
                {
                    var messages = _dbContext.Messages
                        .Where(m => m.ChatId == x.Chat.Id)
                        .OrderByDescending(m => m.SentAt)
                        .ToList();

                    var senderIds = messages.Select(m => m.SenderId).Distinct().ToList();

                    var senders = _dbContext.Users
                        .Where(u => senderIds.Contains(u.Id))
                        .ToDictionary(u => u.Id, u => u);

                    var latestMessage = messages.FirstOrDefault();
                    var isStudent = x.Chat.StudentId == userId;
                    var recipient = isStudent ? x.Tutor : x.Student;

                    return new ChatDto
                    {
                        Id = x.Chat.Id,
                        ListingId = x.Chat.ListingId,
                        TutorId = x.Chat.TutorId,
                        StudentId = x.Chat.StudentId,
                        RecipientId = isStudent ? x.Tutor.Id : x.Student.Id,
                        Details = !string.IsNullOrEmpty(x.CategoryName)
                            ? $"{x.CategoryName} {(isStudent ? "Tutor" : "Student")}"
                            : "No lesson category",
                        Name = $"{recipient.FirstName} {recipient.LastName}",
                        ProfileImagePath = recipient.ImageUrl?.ToString() ?? string.Empty,
                        LastMessage = latestMessage?.Content ?? "No messages yet",
                        Timestamp = latestMessage?.SentAt.DateTime ?? x.Chat.CreatedAt.DateTime,
                        Messages = messages.Select(m => new MessageDto
                        {
                            SentBy = m.SenderId == userId ? "me" : "contact",
                            SenderId = m.SenderId,
                            SenderName = senders.TryGetValue(m.SenderId, out var s) ? $"{s.FirstName} {s.LastName}" : string.Empty,
                            Content = m.Content,
                            FilePath = m.FilePath,
                            Timestamp = m.SentAt.DateTime,
                        }).ToList(),
                        MyRole = isStudent ? UserRole.Student : UserRole.Tutor
                    };
                })
                .ToList();

            return await Task.FromResult(result);
        }

        public async Task<bool> SendMessageAsync(SendMessageDto messageDto, string senderId)
        {
            var chat = await GetOrCreateChatAsync(senderId, messageDto.RecipientId ?? string.Empty, messageDto.ListingId);

            string? filePath = null;
            if (messageDto.File != null)
            {
                filePath = await _fileUploadService.SaveFileAsync(messageDto.File, "chat");
            }

            var message = chat.AddMessage(senderId, messageDto.RecipientId ?? string.Empty, messageDto.Content ?? string.Empty, filePath);

            await _chatRepository.UpdateAsync(chat);

            await _hubContext.Clients.Group(chat.Id.ToString()).SendAsync("NewMessage", new { chatId = chat.Id, messageId = message.Id });

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
            var messages = await _dbContext.Messages
                .Where(m => m.ChatId == chatId && m.Content.Contains(query))
                .OrderBy(m => m.SentAt)
                .ToListAsync();

            var senderIds = messages.Select(m => m.SenderId).Distinct().ToList();
            var senders = _dbContext.Users.Where(u => senderIds.Contains(u.Id)).ToDictionary(u => u.Id, u => u);

            return messages.Select(m => new MessageDto
            {
                SentBy = m.SenderId == userId ? "me" : "contact",
                SenderId = m.SenderId,
                SenderName = senders.TryGetValue(m.SenderId, out var s) ? $"{s.FirstName} {s.LastName}" : string.Empty,
                Content = m.Content,
                FilePath = m.FilePath,
                Timestamp = m.SentAt.DateTime
            }).ToList();
        }

        public async Task<List<string>> GetChatFilesAsync(Guid chatId)
        {
            return await _dbContext.Messages
                .Where(m => m.ChatId == chatId && m.FilePath != null)
                .OrderBy(m => m.SentAt)
                .Select(m => m.FilePath!)
                .ToListAsync();
        }
    }
