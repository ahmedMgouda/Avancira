using System;

namespace Avancira.Application.Catalog.Dtos
{
    public class CreateChatDto
    {
        public Guid ListingId { get; set; }
        public string RecipientId { get; set; } = string.Empty;
    }
}
