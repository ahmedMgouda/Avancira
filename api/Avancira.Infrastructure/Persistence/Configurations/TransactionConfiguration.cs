using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Avancira.Domain.Transactions;
using Avancira.Infrastructure.Identity.Users;

namespace Avancira.Infrastructure.Persistence.Configurations
{
    public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
    {
        public void Configure(EntityTypeBuilder<Transaction> builder)
        {
            builder.HasKey(t => t.Id);

            builder.Property(t => t.SenderId)
                .IsRequired();

            builder.Property(t => t.RecipientId)
                .IsRequired(false);

            builder.Property(t => t.Amount)
                .HasPrecision(18, 2)
                .IsRequired();

            builder.Property(t => t.PlatformFee)
                .HasPrecision(18, 2)
                .IsRequired();

            builder.Property(t => t.TransactionDate)
                .IsRequired();

            builder.Property(t => t.PaymentMethod)
                .IsRequired();

            builder.Property(t => t.PaymentType)
                .IsRequired();

            builder.Property(t => t.Status)
                .IsRequired()
                .HasDefaultValue(TransactionStatus.Created);

            builder.Property(t => t.IsRefunded)
                .IsRequired();

            builder.Property(t => t.RefundedAt)
                .IsRequired(false);

            builder.Property(t => t.RefundAmount)
                .HasPrecision(18, 2)
                .IsRequired(false);

            builder.Property(t => t.Description)
                .HasMaxLength(1000)
                .IsRequired();

            builder.Property(t => t.PayPalPaymentId)
                .HasMaxLength(255)
                .IsRequired(false);

            builder.Property(t => t.StripeCustomerId)
                .HasMaxLength(255)
                .IsRequired(false);

            builder.Property(t => t.StripeCardId)
                .HasMaxLength(255)
                .IsRequired(false);

            builder.HasOne<User>()
                .WithMany()
                .HasForeignKey(t => t.SenderId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<User>()
                .WithMany()
                .HasForeignKey(t => t.RecipientId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(t => t.SenderId);
            builder.HasIndex(t => t.RecipientId);
        }
    }
}
