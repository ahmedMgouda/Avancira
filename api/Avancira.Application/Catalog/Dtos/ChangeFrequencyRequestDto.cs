using System.ComponentModel.DataAnnotations;
using Avancira.Domain.Catalog.Enums;

public class ChangeFrequencyRequestDto
{
    [Required]
    public SubscriptionBillingFrequency NewFrequency { get; set; }
}