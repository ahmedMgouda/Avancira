using Ardalis.Specification;
using Avancira.Domain.Messaging;

namespace Avancira.Application.Catalog.Chats;

public class ChatsByUserSpec : Specification<Chat>
{
    public ChatsByUserSpec(string userId)
    {
        Query.Where(c => c.StudentId == userId || c.TutorId == userId)
             .Include(c => c.Messages);
    }
}
