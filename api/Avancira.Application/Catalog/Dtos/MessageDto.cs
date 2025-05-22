using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Avancira.Application.Catalog.Dtos
{
    public class MessageDto
    {
        public string SentBy { get; set; } // "me" or "contact"
        public string SenderId { get; set; }
        public string SenderName { get; set; }
        public string Content { get; set; }
        public string? FilePath { get; set; }
        public DateTime Timestamp { get; set; }

        public MessageDto()
        {
            SentBy = string.Empty;
            SenderId = string.Empty;
            SenderName = string.Empty;
            Content = string.Empty;
            FilePath = string.Empty;
        }
    }
}
