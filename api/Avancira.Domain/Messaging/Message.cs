using Avancira.Domain.Messaging.Events;
using Avancira.Domain.Common;
using Avancira.Shared.Exceptions;

namespace Avancira.Domain.Messaging
{
    public class Message : BaseEntity<Guid>
    {
        private const int MaxDeletionMinutes = 10;

        public Guid ChatId { get; private set; }

        public string SenderId { get; private set; }
        public string RecipientId { get; private set; }
        public string Content { get; private set; }
        public DateTimeOffset SentAt { get; private set; }
        public bool IsRead { get; private set; }
        public DateTimeOffset? ReadAt { get; private set; }
        public bool IsDelivered { get; private set; }
        public DateTimeOffset? DeliveredAt { get; private set; }
        public bool IsPinned { get; private set; }
        public string? FilePath { get; private set; }
        public DateTimeOffset? DeletedAt { get; private set; }
        public Chat Chat { get; private set; } = default!;
        public List<MessageReaction> Reactions { get; private set; }
        public Message(Guid chatId, string senderId, string recipientId, string content)
        {
            ChatId = chatId;
            SenderId = senderId;
            RecipientId = recipientId;
            Content = content;
            SentAt = DateTimeOffset.UtcNow;
            IsRead = false;
            IsDelivered = false;
            Reactions = new List<MessageReaction>();
            IsPinned = false;
            FilePath = null;
            DeletedAt = null;
        }

        public static Message Create(Guid chatId, string senderId, string recipientId, string content)
        {
            var message = new Message(chatId, senderId, recipientId, content);

            message.QueueDomainEvent(new MessageCreatedEvent(message));

            return message;
        }
        public bool CanBeDeleted()
        {
            return DateTimeOffset.UtcNow - SentAt <= TimeSpan.FromMinutes(MaxDeletionMinutes);
        }
        public void Delete()
        {
            if (CanBeDeleted())
            {
                DeletedAt = DateTimeOffset.UtcNow;

                QueueDomainEvent(new MessageDeletedEvent(this));
            }
            else
            {
                throw new AvanciraException("Message cannot be deleted after 10 minutes.");
            }
        }
        public bool IsDeleted() => DeletedAt.HasValue;
        public void MarkAsRead()
        {
            if (IsRead) return;

            IsRead = true;
            ReadAt = DateTimeOffset.UtcNow;
            QueueDomainEvent(new MessageReadEvent(this));
        }
        public void MarkAsDelivered()
        {
            if (IsDelivered) return;

            IsDelivered = true;
            DeliveredAt = DateTimeOffset.UtcNow;
            QueueDomainEvent(new MessageDeliveredEvent(this));
        }
        public void PinMessage()
        {
            if (IsPinned) return;

            IsPinned = true;
            QueueDomainEvent(new MessagePinnedEvent(this));
        }
        public void UnpinMessage()
        {
            if (!IsPinned) return;

            IsPinned = false;
            QueueDomainEvent(new MessageUnpinnedEvent(this));
        }
        public void RemoveReaction(string userId)
        {
            var reaction = Reactions.FirstOrDefault(r => r.UserId == userId);

            if (reaction != null)
            {
                Reactions.Remove(reaction);
                QueueDomainEvent(new MessageReactionRemovedEvent(reaction));
            }
        }
        public void AddReaction(string userId, string reactionType)
        {
            var existingReaction = Reactions.FirstOrDefault(r => r.UserId == userId);

            if (existingReaction != null)
            {
                existingReaction.UpdateReaction(reactionType);
                QueueDomainEvent(new MessageReactionUpdatedEvent(existingReaction));
            }
            else
            {
                var newReaction = new MessageReaction(this.Id, userId, reactionType);
                Reactions.Add(newReaction);
                QueueDomainEvent(new MessageReactionAddedEvent(newReaction));
            }
        }
        public void RemoveReaction(MessageReaction reaction)
        {
            Reactions.Remove(reaction);

            QueueDomainEvent(new MessageReactionRemovedEvent(reaction));
        }
    }
}
