using Avancira.Domain.Lessons;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Avancira.Infrastructure.Persistence.Configurations.Lessons;

public class LessonMaterialConfiguration : IEntityTypeConfiguration<LessonMaterial>
{
    public void Configure(EntityTypeBuilder<LessonMaterial> builder)
    {
        builder.ToTable("LessonMaterials");

        builder.HasKey(material => material.Id);

        builder.Property(material => material.FileName)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(material => material.FileType)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(material => material.FileUrl)
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(material => material.Description)
            .HasMaxLength(500);

        builder.Property(material => material.UploadedByUserId)
            .HasMaxLength(450);

        builder.Property(material => material.ScanResult)
            .HasMaxLength(200);

        builder.Property(material => material.UploadedAtUtc)
            .HasColumnType("timestamp without time zone");
    }
}
