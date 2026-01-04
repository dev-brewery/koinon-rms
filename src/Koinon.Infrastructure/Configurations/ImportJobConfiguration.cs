using Koinon.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Koinon.Infrastructure.Configurations;

/// <summary>
/// Entity Framework Core configuration for the ImportJob entity.
/// </summary>
public class ImportJobConfiguration : IEntityTypeConfiguration<ImportJob>
{
    public void Configure(EntityTypeBuilder<ImportJob> builder)
    {
        // Table name
        builder.ToTable("import_job");

        // Primary key
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        // Guid with unique index
        builder.Property(e => e.Guid)
            .HasColumnName("guid")
            .IsRequired();
        builder.HasIndex(e => e.Guid)
            .IsUnique()
            .HasDatabaseName("uix_import_job_guid");

        // IdKey is computed, not stored
        builder.Ignore(e => e.IdKey);

        // Audit fields
        builder.Property(e => e.CreatedDateTime)
            .HasColumnName("created_date_time")
            .IsRequired();
        builder.Property(e => e.ModifiedDateTime)
            .HasColumnName("modified_date_time");
        builder.Property(e => e.CreatedByPersonAliasId)
            .HasColumnName("created_by_person_alias_id");
        builder.Property(e => e.ModifiedByPersonAliasId)
            .HasColumnName("modified_by_person_alias_id");

        // Foreign keys
        builder.Property(e => e.ImportTemplateId)
            .HasColumnName("import_template_id");

        builder.HasOne(e => e.ImportTemplate)
            .WithMany(t => t.ImportJobs)
            .HasForeignKey(e => e.ImportTemplateId)
            .OnDelete(DeleteBehavior.SetNull);

        // Regular properties
        builder.Property(e => e.ImportType)
            .HasColumnName("import_type")
            .IsRequired();

        builder.Property(e => e.Status)
            .HasColumnName("status")
            .IsRequired();

        builder.Property(e => e.FileName)
            .HasColumnName("file_name")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(e => e.TotalRows)
            .HasColumnName("total_rows")
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(e => e.ProcessedRows)
            .HasColumnName("processed_rows")
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(e => e.SuccessCount)
            .HasColumnName("success_count")
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(e => e.ErrorCount)
            .HasColumnName("error_count")
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(e => e.ErrorDetails)
            .HasColumnName("error_details")
            .HasColumnType("jsonb");

        builder.Property(e => e.StartedAt)
            .HasColumnName("started_at");

        builder.Property(e => e.CompletedAt)
            .HasColumnName("completed_at");

        builder.Property(e => e.StorageKey)
            .HasColumnName("storage_key")
            .HasMaxLength(500);

        builder.Property(e => e.BackgroundJobId)
            .HasColumnName("background_job_id")
            .HasMaxLength(100);

        // Indexes
        builder.HasIndex(e => e.Status)
            .HasDatabaseName("ix_import_job_status");

        builder.HasIndex(e => e.ImportType)
            .HasDatabaseName("ix_import_job_import_type");

        builder.HasIndex(e => e.CreatedDateTime)
            .HasDatabaseName("ix_import_job_created_date_time");

        builder.HasIndex(e => new { e.Status, e.CreatedDateTime })
            .HasDatabaseName("ix_import_job_status_created");
    }
}
