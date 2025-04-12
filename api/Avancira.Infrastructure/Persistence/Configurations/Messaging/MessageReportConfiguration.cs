using Avancira.Domain.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Avancira.Infrastructure.Persistence.Configurations.Messaging;
public class MessageReportConfiguration : IEntityTypeConfiguration<MessageReport>
{
    public void Configure(EntityTypeBuilder<MessageReport> builder)
    {
        builder.HasKey(report => report.Id);

        builder.Property(report => report.MessageId)
            .IsRequired();

        builder.Property(report => report.UserId)
            .IsRequired();

        builder.Property(report => report.ReportReason)
            .IsRequired();

        builder.Property(report => report.ReportedAt)
            .IsRequired();

        builder.HasOne(report => report.Message)
            .WithMany()
            .HasForeignKey(report => report.MessageId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(report => new { report.MessageId, report.UserId })
            .IsUnique()
            .HasDatabaseName("IX_MessageReport_MessageId_UserId");
    }
}