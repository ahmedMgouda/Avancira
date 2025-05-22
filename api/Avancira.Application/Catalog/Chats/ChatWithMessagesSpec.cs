using Ardalis.Specification;
using Avancira.Domain.Messaging;

namespace Avancira.Application.Catalog.Chats;

public class ChatWithMessagesSpec : Specification<Chat>
{
    public ChatWithMessagesSpec(Guid chatId)
    {
        Query.Where(c => c.Id == chatId)
             .Include(c => c.Messages);
    }
}
