using Avancira.Domain.Common;

namespace Avancira.Domain.Messaging;

public class MessageReaction : BaseEntity<Guid>
{
    public Guid MessageId { get; set; } = default!;
    public string UserId { get; set; } = default!;
    public string ReactionType { get; set; }
    public DateTimeOffset ReactedAt { get; set; }
    public Message Message { get; set; } = default!;

    public MessageReaction()
    {
        ReactionType = string.Empty;
        ReactedAt = DateTimeOffset.UtcNow;
    }

    public MessageReaction(Guid messageId, string userId, string reactionType)
    {
        MessageId = messageId;
        UserId = userId;
        ReactionType = reactionType;
        ReactedAt = DateTimeOffset.UtcNow;
    }
    public void UpdateReaction(string newReactionType)
    {
        if (ReactionType != newReactionType)
        {
            ReactionType = newReactionType;
            ReactedAt = DateTimeOffset.UtcNow;
        }
    }
}
