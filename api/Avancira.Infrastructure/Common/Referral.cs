using Avancira.Infrastructure.Identity.Users;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Avancira.Infrastructure.Catalog
{
    public class Referral
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string ReferrerId { get; set; }
        [ForeignKey(nameof(Referral.ReferrerId))]
        public virtual User? Referrer { get; set; }

        [Required]
        public string ReferredId { get; set; }
        [ForeignKey(nameof(Referral.ReferredId))]
        public virtual User? Referred { get; set; }

        public DateTime CreatedAt { get; set; }

        public Referral()
        {
            ReferrerId = string.Empty;
            ReferredId = string.Empty;
        }
    }

}
