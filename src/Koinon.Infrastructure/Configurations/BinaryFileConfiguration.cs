using Koinon.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Koinon.Infrastructure.Configurations;

/// <summary>
/// EF Core configuration for the BinaryFile entity.
/// </summary>
public class BinaryFileConfiguration : IEntityTypeConfiguration<BinaryFile>
{
    public void Configure(EntityTypeBuilder<BinaryFile> builder)
    {
        builder.ToTable("binary_file");

        // Primary key
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasColumnName("id");

        // Standard entity properties
        builder.Property(e => e.Guid)
            .HasColumnName("guid")
            .IsRequired();

        builder.HasIndex(e => e.Guid)
            .IsUnique();

        builder.Property(e => e.CreatedDateTime)
            .HasColumnName("created_date_time")
            .IsRequired();

        builder.Property(e => e.ModifiedDateTime)
            .HasColumnName("modified_date_time");

        builder.Property(e => e.CreatedByPersonAliasId)
            .HasColumnName("created_by_person_alias_id");

        builder.Property(e => e.ModifiedByPersonAliasId)
            .HasColumnName("modified_by_person_alias_id");

        // File properties
        builder.Property(e => e.FileName)
            .HasColumnName("file_name")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(e => e.MimeType)
            .HasColumnName("mime_type")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.StorageKey)
            .HasColumnName("storage_key")
            .HasMaxLength(500)
            .IsRequired();

        builder.HasIndex(e => e.StorageKey)
            .IsUnique();

        builder.Property(e => e.FileSizeBytes)
            .HasColumnName("file_size_bytes")
            .IsRequired();

        builder.Property(e => e.Width)
            .HasColumnName("width");

        builder.Property(e => e.Height)
            .HasColumnName("height");

        builder.Property(e => e.BinaryFileTypeId)
            .HasColumnName("binary_file_type_id");

        builder.Property(e => e.Description)
            .HasColumnName("description")
            .HasMaxLength(1000);

        // Relationships
        builder.HasOne(e => e.BinaryFileType)
            .WithMany()
            .HasForeignKey(e => e.BinaryFileTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes for common queries
        builder.HasIndex(e => e.BinaryFileTypeId);
        builder.HasIndex(e => e.MimeType);
        builder.HasIndex(e => e.CreatedDateTime);

        // Ignore computed property
        builder.Ignore(e => e.IdKey);
    }
}
