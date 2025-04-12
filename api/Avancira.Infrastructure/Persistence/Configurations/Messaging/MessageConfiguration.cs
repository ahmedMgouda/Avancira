using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Avancira.Domain.Messaging;

namespace Avancira.Infrastructure.Persistence.Configurations.Messaging;
public class MessageConfiguration : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> builder)
    {
        builder.HasKey(message => message.Id);

        builder.Property(message => message.ChatId)
            .IsRequired();

        builder.Property(message => message.SenderId)
            .IsRequired();

        builder.Property(message => message.RecipientId)
            .IsRequired();

        builder.Property(message => message.Content)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(message => message.SentAt)
            .IsRequired();

        builder.Property(message => message.IsRead)
            .IsRequired();

        builder.Property(message => message.IsDelivered)
            .IsRequired();

        builder.Property(message => message.IsPinned)
            .IsRequired();

        builder.Property(message => message.FilePath)
            .HasMaxLength(500);

        builder.Property(message => message.DeletedAt)
            .IsRequired(false);

        builder.HasOne(message => message.Chat)
            .WithMany(chat => chat.Messages)
            .HasForeignKey(message => message.ChatId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(message => message.Reactions)
            .WithOne(reaction => reaction.Message)
            .HasForeignKey(reaction => reaction.MessageId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(message => message.IsRead);
    }
}
