using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Avancira.Application.Catalog.Dtos
{
    public class PaymentHistoryDto
    {
        public decimal WalletBalance { get; set; }
        public decimal TotalAmountCollected { get; set; }
        public List<TransactionDto> Invoices { get; set; }
        public List<TransactionDto> Transactions { get; set; }

        public PaymentHistoryDto()
        {
            Invoices = new List<TransactionDto>();
            Transactions = new List<TransactionDto>();
        }
    }
}
