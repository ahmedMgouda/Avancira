using Avancira.Domain.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Avancira.Infrastructure.Persistence.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.HasKey(n => n.Id);
        
        builder.Property(n => n.UserId)
            .IsRequired()
            .HasMaxLength(450);
        
        builder.Property(n => n.EventName)
            .IsRequired()
            .HasMaxLength(100);
        
        builder.Property(n => n.Message)
            .IsRequired()
            .HasMaxLength(1000);
        
        builder.Property(n => n.Data)
            .HasColumnType("text");
        
        builder.Property(n => n.IsRead)
            .IsRequired()
            .HasDefaultValue(false);
        
        builder.Property(n => n.ReadAt);
        
        builder.HasIndex(n => n.UserId);
        builder.HasIndex(n => n.EventName);
        builder.HasIndex(n => n.IsRead);
        builder.HasIndex(n => n.Created);
    }
}
