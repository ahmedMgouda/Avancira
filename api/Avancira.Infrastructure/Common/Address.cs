using Avancira.Infrastructure.Identity.Users;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Avancira.Infrastructure.Catalog;

public class Address
{
    [Key]
    public int Id { get; set; }

    [MaxLength(1024)]
    public string FormattedAddress { get; set; }

    [MaxLength(255)]
    public string StreetAddress { get; set; }

    [MaxLength(255)]
    public string City { get; set; }

    [MaxLength(255)]
    public string State { get; set; }

    [MaxLength(255)]
    public string Country { get; set; }

    [MaxLength(20)]
    public string PostalCode { get; set; }

    public double Latitude { get; set; }

    public double Longitude { get; set; }

    public string UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }

    public Address()
    {
        UserId = string.Empty;
        FormattedAddress = string.Empty;
        StreetAddress = string.Empty;
        City = string.Empty;
        State = string.Empty;
        Country = string.Empty;
        PostalCode = string.Empty;
    }
}
