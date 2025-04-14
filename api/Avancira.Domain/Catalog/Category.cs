using Avancira.Domain.Common;

namespace Avancira.Domain.Catalog;
public class Category : BaseEntity<Guid>
{
    public Category()
    {
        ListingCategories = new List<ListingCategory>();
    }
    public string Name { get; set; } = string.Empty;
    public bool DisplayInLandingPage { get; set; } = false;

    public string? ImageUrl { get; set; }
    public virtual ICollection<ListingCategory> ListingCategories { get; set; }
}
