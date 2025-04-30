using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Avancira.Application.Catalog.Dtos
{
    public class TransactionDto
    {
        public int Id { get; set; }
        public string SenderId { get; set; }
        public string SenderName { get; set; }
        public string? RecipientId { get; set; }
        public string RecipientName { get; set; }
        public decimal Amount { get; set; }
        public decimal PlatformFee { get; set; }
        public decimal Net { get; set; }
        public string Status { get; set; }
        public string TransactionType { get; set; }
        public string Description { get; set; }
        public DateTime TransactionDate { get; set; }
        public string Date { get; set; }
        public string Type { get; set; }

        public TransactionDto()
        {
            SenderId = string.Empty;
            SenderName = string.Empty;
            RecipientName = string.Empty;
            Status = string.Empty;
            TransactionType = string.Empty;
            Description = string.Empty;
            Date = string.Empty;
            Type = string.Empty;
        }
    }
}
