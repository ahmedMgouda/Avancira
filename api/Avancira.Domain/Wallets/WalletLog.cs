using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Avancira.Domain.Wallets
{
    public class WalletLog
    {
        [Key]
        public int Id { get; set; }
        public int WalletId { get; set; }
        [ForeignKey(nameof(WalletId))]
        public Wallet? Wallet { get; set; }
        public decimal AmountChanged { get; set; }
        public decimal NewBalance { get; set; }
        public string Reason { get; set; }
        public DateTime CreatedAt { get; set; }

        public WalletLog()
        {
            Reason = string.Empty;
        }
    }
}
