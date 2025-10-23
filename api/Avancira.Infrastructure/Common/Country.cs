using System;
using System.ComponentModel.DataAnnotations;

namespace Avancira.Infrastructure.Catalog;

[Obsolete("Country entity is deprecated and retained only for migration compatibility.")]
public class Country
{
    [Key]
    public int Id { get; set; }

    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(10)]
    public string Code { get; set; } = string.Empty;
}
