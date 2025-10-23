using Avancira.Domain.Common;
using Avancira.Domain.Common.Contracts;
using Avancira.Domain.Common.Exceptions;
using Avancira.Domain.Messaging.Events;

namespace Avancira.Domain.Messaging;

public class Chat : BaseEntity<Guid>, IAggregateRoot
{
    public string StudentId { get; private set; } = default!;
    public string TutorId { get; private set; } = default!;
    public DateTimeOffset CreatedAt { get; private set; }
    public BlockStatus BlockStatus { get; private set; }
    public List<Message> Messages { get; private set; } = new List<Message>();

    private Chat()
    {
    }

    private Chat(Guid id, string studentId, string tutorId, BlockStatus blockStatus)
    {
        Id = id;
        StudentId = studentId;
        TutorId = tutorId;
        BlockStatus = blockStatus;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public static Chat Create(string studentId, string tutorId, BlockStatus blockStatus) =>
        new(Guid.NewGuid(), studentId, tutorId, blockStatus);

    private Message GetMessageById(Guid messageId)
    {
        var message = Messages.Find(m => m.Id == messageId);
        if (message == null)
        {
            throw new AvanciraException("Message not found.");
        }

        return message;
    }

    public void AddMessage(string senderId, string recipientId, string content)
    {
        var message = Message.Create(Id, senderId, recipientId, content);
        Messages.Add(message);
    }

    public void DeleteMessage(Guid messageId)
    {
        var message = GetMessageById(messageId);
        message.Delete();
    }

    public void MarkMessageAsRead(Guid messageId)
    {
        var message = GetMessageById(messageId);
        message.MarkAsRead();
    }

    public void MarkMessageAsDelivered(Guid messageId)
    {
        var message = GetMessageById(messageId);
        message.MarkAsDelivered();
    }

    public void TogglePinMessage(Guid messageId)
    {
        var message = GetMessageById(messageId);
        message.TogglePin();
    }

    public void AddReactionToMessage(Guid messageId, string userId, string reactionType)
    {
        var message = GetMessageById(messageId);
        message.AddOrUpdateReaction(userId, reactionType);
    }

    public void RemoveReactionFromMessage(Guid messageId, string userId)
    {
        var message = GetMessageById(messageId);
        message.RemoveReaction(userId);
    }

    public void BlockUser(string userId)
    {
        if (userId == StudentId)
        {
            if (BlockStatus != BlockStatus.BlockedByStudent)
            {
                BlockStatus = BlockStatus.BlockedByStudent;
            }
        }
        else if (userId == TutorId)
        {
            if (BlockStatus != BlockStatus.BlockedByTutor)
            {
                BlockStatus = BlockStatus.BlockedByTutor;
            }
        }

        QueueDomainEvent(new UserBlockedEvent(this));
    }

    public void UnblockUser(string userId)
    {
        if (userId == StudentId && BlockStatus != BlockStatus.BlockedByStudent)
        {
            BlockStatus = BlockStatus.NotBlocked;
        }
        else if (userId == TutorId && BlockStatus != BlockStatus.BlockedByTutor)
        {
            BlockStatus = BlockStatus.NotBlocked;
        }

        QueueDomainEvent(new UserUnblockedEvent(this));
    }

    public void ReportMessage(Guid messageId, string userId, string reportReason)
    {
        var message = GetMessageById(messageId);
        message.ReportMessage(userId, reportReason);
    }
}
