using Avancira.Application.Catalog.Dtos;
using Avancira.Application.Messaging;
using Avancira.Domain.Catalog.Enums;
using Avancira.Domain.Messaging;
using Avancira.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;

namespace Avancira.Infrastructure.Catalog
{
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

        public ChatDto GetChat(Guid chatId, string userId)
        {
            var x = (from chat in _dbContext.Chats
                     where chat.Id == chatId && (chat.StudentId == userId || chat.TutorId == userId)
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
                     }).FirstOrDefault();

            if (x == null)
            {
                return new ChatDto();
            }

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
                    Timestamp = m.SentAt.DateTime,
                }).ToList(),
                MyRole = isStudent ? UserRole.Student : UserRole.Tutor
            };
        }
        public async Task<bool> SendMessageAsync(SendMessageDto messageDto, string senderId)
        {
            var listing = _dbContext.Listings.FirstOrDefault(l => l.Id == messageDto.ListingId);
            if (listing == null)
            {
                _logger.LogWarning("Listing {ListingId} not found when sending message", messageDto.ListingId);
                return false;
            }

            var tutorId = listing.UserId ?? string.Empty;
            var studentId = senderId == tutorId ? (messageDto.RecipientId ?? string.Empty) : senderId;
            var recipientId = senderId == tutorId ? (messageDto.RecipientId ?? string.Empty) : tutorId;

            var chat = GetOrCreateChat(studentId, tutorId, messageDto.ListingId);

            chat.AddMessage(senderId, recipientId, messageDto.Content ?? string.Empty);

            _dbContext.Chats.Update(chat);
            await _dbContext.SaveChangesAsync();


            // Send notification to recipient about new message
            if (!string.IsNullOrEmpty(messageDto.RecipientId))
            {
                var sender = _dbContext.Users.FirstOrDefault(u => u.Id == senderId);
                var senderName = $"{sender?.FirstName} {sender?.LastName}".Trim();
                if (string.IsNullOrEmpty(senderName)) senderName = "Someone";

                listing = _dbContext.Listings.FirstOrDefault(l => l.Id == messageDto.ListingId);
                var lessonTitle = listing?.Name ?? "lesson";

                var message = $"{senderName} sent you a message about '{lessonTitle}'";
                
               await _notificationService.NotifyAsync(
                    messageDto.RecipientId,
                    Avancira.Domain.Catalog.Enums.NotificationEvent.NewMessage,
                    message,
                    new {
                        ChatId = chat.Id,
                        SenderId = senderId,
                        SenderName = senderName,
                        ListingId = messageDto.ListingId,
                        LessonTitle = lessonTitle,
                        MessagePreview = messageDto.Content?.Length > 50 
                            ? messageDto.Content.Substring(0, 50) + "..." 
                            : messageDto.Content
                    }
                );

                _logger.LogInformation("Message sent from {SenderId} to {RecipientId}, notification sent", senderId, messageDto.RecipientId);
            }

            return true;
        }
    }
}
