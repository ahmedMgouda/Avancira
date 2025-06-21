using Avancira.Domain.Catalog;

namespace Avancira.Domain.PromoCodes
{
    public class ListingPromoCode
    {
        public Guid ListingId { get; set; }
        public Guid PromoCodeId { get; set; }

        public virtual Listing Listing { get; set; } = default!;
        public virtual PromoCode PromoCode { get; set; } = default!;
    }
}
