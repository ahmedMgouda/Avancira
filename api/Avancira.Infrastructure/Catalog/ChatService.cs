using Avancira.Application.Catalog.Dtos;
using Avancira.Domain.Catalog.Enums;
using Avancira.Domain.Messaging;
using Avancira.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            var chats = _dbContext.Chats
                .Where(c => c.StudentId == userId || c.TutorId == userId)
                .Include(c => c.Listing)
                    .ThenInclude(l => l.ListingCategories)
                        .ThenInclude(llc => llc.Category)
                //.Include(c => c.Tutor)
                //.Include(c => c.Student)
                .Include(c => c.Messages)
                    //.ThenInclude(m => m.Sender)
                .ToList();

            var result = chats.Select(c =>
            {
                // Get the latest message
                var latestMessage = c.Messages
                    .OrderByDescending(m => m.SentAt)
                    .FirstOrDefault();

                var lessonCategory = c.Listing?.ListingCategories
                    .Select(llc => llc.Category?.Name)
                    .FirstOrDefault(name => !string.IsNullOrEmpty(name));

                return new ChatDto
                {
                    Id = c.Id,
                    ListingId = c.ListingId,
                    TutorId = c.TutorId,
                    StudentId = c.StudentId,
                    RecipientId = c.StudentId == userId ? c.TutorId : c.StudentId,
                    Details = lessonCategory != null
                        ? $"{lessonCategory} {(c.StudentId == userId ? "Tutor" : "Student")}"
                        : "No lesson category",
                    //Name = c.StudentId == userId
                    //    ? c.Tutor?.FullName ?? "Unknown Tutor"
                    //    : c.Student?.FullName ?? "Unknown Student",
                    //ProfileImagePath = c.StudentId == userId
                    //    ? c.Tutor?.ProfileImageUrl ?? ""
                    //    : c.Student?.ProfileImageUrl ?? "",
                    LastMessage = latestMessage?.Content ?? "No messages yet",
                    //Timestamp = latestMessage?.SentAt ?? c.CreatedAt,
                    Messages = c.Messages.Select(m => new MessageDto
                    {
                        SentBy = m.SenderId == userId ? "me" : "contact",
                        SenderId = m.SenderId,
                        //SenderName = m.Sender?.FullName ?? "",
                        Content = m.Content,
                        //Timestamp = m.SentAt
                    }).ToList(),
                    MyRole = c.StudentId == userId ? UserRole.Student : UserRole.Tutor
                };
            }).ToList();
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
