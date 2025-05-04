using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Avancira.Application.Catalog.Dtos
{
    public class SendMessageDto
    {
        public Guid ListingId { get; set; }
        public string? RecipientId { get; set; }
        public string? Content { get; set; }

        public SendMessageDto()
        {
            Content = string.Empty;
        }
    }
}
