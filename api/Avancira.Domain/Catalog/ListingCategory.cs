namespace Avancira.Domain.Catalog;
public class ListingCategory
{
    public Guid ListingId { get; set; }
    public Guid CategoryId { get; set; }
    public virtual Listing Listing { get; set; } = default!;
    public virtual Category Category { get; set; } = default!;
}
