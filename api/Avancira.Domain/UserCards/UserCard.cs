using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avancira.Domain.Catalog.Enums;

namespace Avancira.Domain.UserCard
{
    public class UserCard
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }

        //[ForeignKey(nameof(UserCard.UserId))]
        //public virtual User? User { get; set; }

        [Required]
        [StringLength(255)]
        public string CardId { get; set; }

        [Required]
        [StringLength(4)]
        public string Last4 { get; set; }

        [Required]
        public long ExpMonth { get; set; }

        [Required]
        public long ExpYear { get; set; }

        [StringLength(10)]
        public string? Brand { get; set; }

        [Required]
        public UserCardType Type { get; set; }

        public UserCard()
        {
            UserId = string.Empty;
            CardId = string.Empty;
            Last4 = string.Empty;
        }
        public override string ToString()
        {
            return $"UserCard: {Id}, UserId: {UserId}, Last4: ****{Last4}, Expiration: {ExpMonth}/{ExpYear}, Type: {Type}";
        }
    }
}
