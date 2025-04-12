using Avancira.Domain.Catalog;
using Avancira.Domain.Common;

namespace Avancira.Domain.Messaging;
public class Chat : BaseEntity<Guid>
{
    public string StudentId { get; set; } = default!;
    public string TutorId { get; set; } = default!;
    public int ListingId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public bool IsBlockedByStudent { get; set; }
    public bool IsBlockedByTutor { get; set; }
    public List<Message> Messages { get; set; } = new List<Message>();
    public Listing Listing { get; set; } = default!;

    public Chat()
    {
        CreatedAt = DateTimeOffset.UtcNow;
    }
}