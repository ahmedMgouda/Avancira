using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Avancira.Domain.Messaging;

namespace Avancira.Infrastructure.Persistence.Configurations.Messaging;

public class MessageReactionConfiguration : IEntityTypeConfiguration<MessageReaction>
{
    public void Configure(EntityTypeBuilder<MessageReaction> builder)
    {
        builder.HasKey(reaction => reaction.Id);

        builder.Property(reaction => reaction.MessageId)
            .IsRequired();

        builder.Property(reaction => reaction.UserId)
            .IsRequired();

        builder.Property(reaction => reaction.ReactionType)
            .IsRequired();

        builder.Property(reaction => reaction.ReactedAt)
            .IsRequired();

        builder.HasOne(reaction => reaction.Message)
            .WithMany(message => message.Reactions)
            .HasForeignKey(reaction => reaction.MessageId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(reaction => new { reaction.MessageId, reaction.UserId })
            .IsUnique()
            .HasDatabaseName("IX_MessageReaction_MessageId_UserId");
    }
}

