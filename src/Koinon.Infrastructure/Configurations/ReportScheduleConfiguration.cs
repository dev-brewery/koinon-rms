using Koinon.Domain.Entities;
using Koinon.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Koinon.Infrastructure.Configurations;

/// <summary>
/// Entity Framework Core configuration for the ReportSchedule entity.
/// </summary>
public class ReportScheduleConfiguration : IEntityTypeConfiguration<ReportSchedule>
{
    public void Configure(EntityTypeBuilder<ReportSchedule> builder)
    {
        // Table name
        builder.ToTable("report_schedule");

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
            .HasDatabaseName("uix_report_schedule_guid");

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
        builder.Property(e => e.ReportDefinitionId)
            .HasColumnName("report_definition_id")
            .IsRequired();

        builder.HasOne(e => e.ReportDefinition)
            .WithMany()
            .HasForeignKey(e => e.ReportDefinitionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Regular properties
        builder.Property(e => e.CronExpression)
            .HasColumnName("cron_expression")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.TimeZone)
            .HasColumnName("time_zone")
            .HasMaxLength(100)
            .IsRequired()
            .HasDefaultValue("America/New_York");

        builder.Property(e => e.Parameters)
            .HasColumnName("parameters")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(e => e.RecipientPersonAliasIds)
            .HasColumnName("recipient_person_alias_ids")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(e => e.OutputFormat)
            .HasColumnName("output_format")
            .HasConversion<int>()
            .IsRequired()
            .HasDefaultValue(ReportOutputFormat.Pdf);

        builder.Property(e => e.IsActive)
            .HasColumnName("is_active")
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(e => e.LastRunAt)
            .HasColumnName("last_run_at");

        builder.Property(e => e.NextRunAt)
            .HasColumnName("next_run_at");

        // Indexes
        builder.HasIndex(e => e.IsActive)
            .HasDatabaseName("ix_report_schedule_is_active");

        builder.HasIndex(e => e.NextRunAt)
            .HasDatabaseName("ix_report_schedule_next_run_at");
    }
}
