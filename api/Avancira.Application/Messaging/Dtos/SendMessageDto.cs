using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Avancira.Application.Messaging.Dtos
{
    public class SendMessageDto
    {
        public string? RecipientId { get; set; }
        public string? Content { get; set; }

        public SendMessageDto()
        {
            Content = string.Empty;
        }
    }
}
