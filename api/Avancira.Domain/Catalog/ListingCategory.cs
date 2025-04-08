namespace Avancira.Domain.Catalog;
public class ListingCategory
{
    public int ListingId { get; set; }
    public int LessonCategoryId { get; set; }
    //public virtual Listing Listing { get; set; } = default!;
    public virtual Category Category { get; set; } = default!;
}
