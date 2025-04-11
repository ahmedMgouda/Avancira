namespace Avancira.Domain.Catalog;
public class Category
{
    public Category()
    {
        ListingCategories = new List<ListingCategory>();
    }
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool DisplayInLandingPage { get; set; } = false;

    public string? ImageUrl { get; set; }
    public virtual ICollection<ListingCategory> ListingCategories { get; set; }
}
