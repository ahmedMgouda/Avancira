using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Avancira.Infrastructure.Identity.Users;

[Owned]
public class Address
{
    [MaxLength(255)]
    public string Street { get; set; } = string.Empty;

    [MaxLength(100)]
    public string City { get; set; } = string.Empty;

    [MaxLength(100)]
    public string State { get; set; } = string.Empty;

    [MaxLength(20)]
    public string PostalCode { get; set; } = string.Empty;
}
