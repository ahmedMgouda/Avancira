using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using Avancira.Domain.Messaging;

namespace Avancira.Infrastructure.Persistence.Configurations.Messaging;

 public class ChatConfiguration : IEntityTypeConfiguration<Chat>
{
    public void Configure(EntityTypeBuilder<Chat> builder)
    {
        builder.HasKey(chat => chat.Id);

        builder.Property(chat => chat.StudentId)
            .IsRequired();

        builder.Property(chat => chat.TutorId)
            .IsRequired();

        builder.Property(chat => chat.ListingId)
            .IsRequired();

        builder.Property(chat => chat.CreatedAt)
            .IsRequired();

        builder.Property(chat => chat.BlockStatus)
            .IsRequired();

        builder.HasMany(chat => chat.Messages)
            .WithOne()
            .HasForeignKey(message => message.ChatId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(chat => chat.StudentId);
        builder.HasIndex(chat => chat.TutorId);
        builder.HasIndex(chat => chat.ListingId);
    }
}
