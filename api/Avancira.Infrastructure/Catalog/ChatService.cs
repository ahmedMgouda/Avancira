using Avancira.Application.Catalog.Dtos;
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
}
