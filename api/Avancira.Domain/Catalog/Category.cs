using Avancira.Domain.Common;
using Avancira.Domain.Common.Contracts;

namespace Avancira.Domain.Catalog;
public class Category : BaseEntity<Guid>, IAggregateRoot
{
    public Category()
    {
        ListingCategories = new List<ListingCategory>();
    }
    public string Name { get; set; } = string.Empty;
    public bool DisplayInLandingPage { get; set; } = false;
    public Uri? ImageUrl { get; set; }
    public virtual ICollection<ListingCategory> ListingCategories { get; set; }
}
