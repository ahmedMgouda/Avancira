using Ardalis.Specification;
using Avancira.Domain.Messaging;
using System;

namespace Avancira.Application.Catalog.Chats
{
    public class ChatByParticipantsSpec : Specification<Chat>
    {
        public ChatByParticipantsSpec(string firstUserId, string secondUserId, Guid listingId)
        {
            Query.Where(c => c.ListingId == listingId &&
                              ((c.StudentId == firstUserId && c.TutorId == secondUserId) ||
                               (c.StudentId == secondUserId && c.TutorId == firstUserId)));
        }
    }
}
