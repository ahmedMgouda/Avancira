using System.ComponentModel.DataAnnotations;

namespace Avancira.Domain.Catalog;
public class Category
{
    public Category()
    {
        ListingCategories = new List<ListingCategory>();
    }

    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    public bool DisplayInLandingPage { get; set; } = false;

    [Required]
    public string? ImageUrl { get; set; }

    public virtual ICollection<ListingCategory> ListingCategories { get; set; }
}
