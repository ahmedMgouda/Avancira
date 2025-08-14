using Avancira.Domain.Common;
using Avancira.Domain.Common.Contracts;
using Avancira.Infrastructure.Catalog;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Avancira.Infrastructure.Identity.Users;
public class User : IdentityUser<string>
{
    public User()
    {
        Id = Guid.NewGuid().ToString();
    }

    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateOnly? DateOfBirth { get; set; }
    public Uri? ImageUrl { get; set; }
    public bool IsActive { get; set; }
    public string? TimeZoneId { get; set; }
    public string? ObjectId { get; set; }



    [MaxLength(500)]
    public string? Bio { get; set; }
    public Address? Address { get; set; }
    public int? CountryId { get; set; }
    [ForeignKey(nameof(User.CountryId))]
    public virtual Country? Country { get; set; }



    [MaxLength(255)]
    public string? PayPalAccountId { get; set; } // For payouts
    [MaxLength(255)]
    public string? StripeCustomerId { get; set; } // For payments
    [MaxLength(255)]
    public string? StripeConnectedAccountId { get; set; } // For payouts
    [MaxLength(255)]
    public string? SkypeId { get; set; }
    [MaxLength(255)]
    public string? HangoutId { get; set; }

    [NotMapped]
    public string PaymentGateway
    {
        get
        {
            if (!string.IsNullOrEmpty(PayPalAccountId))
            {
                return "PayPal";
            }
            else
            {
                return "Stripe";
            }
        }
    }
}
