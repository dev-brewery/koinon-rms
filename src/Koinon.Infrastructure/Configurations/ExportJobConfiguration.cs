using Koinon.Domain.Entities;
using Koinon.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Koinon.Infrastructure.Configurations;

/// <summary>
/// Entity Framework Core configuration for the ExportJob entity.
/// </summary>
public class ExportJobConfiguration : IEntityTypeConfiguration<ExportJob>
{
    public void Configure(EntityTypeBuilder<ExportJob> builder)
    {
        // Table name
        builder.ToTable("export_job");

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
            .HasDatabaseName("uix_export_job_guid");

        // IdKey is computed, not stored
        builder.Ignore(e => e.IdKey);

        // Audit fields
        builder.Property(e => e.CreatedDateTime)
            .HasColumnName("created_date_time")
            .IsRequired();
        builder.Property(e => e.ModifiedDateTime)
            .HasColumnName("modified_date_time");

        // PersonAlias audit fields (inherited from IAuditable)
        builder.Property(e => e.CreatedByPersonAliasId)
            .HasColumnName("created_by_person_alias_id");
        builder.Property(e => e.ModifiedByPersonAliasId)
            .HasColumnName("modified_by_person_alias_id");

        // Foreign keys
        builder.Property(e => e.OutputFileId)
            .HasColumnName("output_file_id");

        builder.HasOne(e => e.OutputFile)
            .WithMany()
            .HasForeignKey(e => e.OutputFileId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Property(e => e.RequestedByPersonAliasId)
            .HasColumnName("requested_by_person_alias_id");

        builder.HasOne(e => e.RequestedByPersonAlias)
            .WithMany()
            .HasForeignKey(e => e.RequestedByPersonAliasId)
            .OnDelete(DeleteBehavior.SetNull);

        // Regular properties
        builder.Property(e => e.ExportType)
            .HasColumnName("export_type")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(e => e.EntityType)
            .HasColumnName("entity_type")
            .HasMaxLength(100);

        builder.Property(e => e.Status)
            .HasColumnName("status")
            .HasConversion<int>()
            .IsRequired()
            .HasDefaultValue(ReportStatus.Pending);

        builder.Property(e => e.Parameters)
            .HasColumnName("parameters")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(e => e.OutputFormat)
            .HasColumnName("output_format")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(e => e.StartedAt)
            .HasColumnName("started_at");

        builder.Property(e => e.CompletedAt)
            .HasColumnName("completed_at");

        builder.Property(e => e.ErrorMessage)
            .HasColumnName("error_message")
            .HasMaxLength(2000);

        builder.Property(e => e.RecordCount)
            .HasColumnName("record_count");

        // Indexes
        builder.HasIndex(e => e.Status)
            .HasDatabaseName("ix_export_job_status");

        builder.HasIndex(e => e.ExportType)
            .HasDatabaseName("ix_export_job_export_type");
    }
}
