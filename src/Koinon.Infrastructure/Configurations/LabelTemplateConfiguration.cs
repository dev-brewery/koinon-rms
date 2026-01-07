using Koinon.Domain.Entities;
using Koinon.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Koinon.Infrastructure.Configurations;

/// <summary>
/// Entity Framework Core configuration for the LabelTemplate entity.
/// </summary>
public class LabelTemplateConfiguration : IEntityTypeConfiguration<LabelTemplate>
{
    public void Configure(EntityTypeBuilder<LabelTemplate> builder)
    {
        // Table name
        builder.ToTable("label_template");

        // Primary key
        builder.HasKey(lt => lt.Id);
        builder.Property(lt => lt.Id).HasColumnName("id");

        // Guid with unique index
        builder.Property(lt => lt.Guid)
            .HasColumnName("guid")
            .IsRequired();
        builder.HasIndex(lt => lt.Guid)
            .IsUnique()
            .HasDatabaseName("uix_label_template_guid");

        // Ignore computed property
        builder.Ignore(lt => lt.IdKey);

        // Name
        builder.Property(lt => lt.Name)
            .HasColumnName("name")
            .HasMaxLength(100)
            .IsRequired();

        // Type (stored as integer)
        builder.Property(lt => lt.Type)
            .HasColumnName("type")
            .HasConversion<int>()
            .IsRequired();

        // Add index for querying by type
        builder.HasIndex(lt => lt.Type)
            .HasDatabaseName("ix_label_template_type");

        // Format
        builder.Property(lt => lt.Format)
            .HasColumnName("format")
            .HasMaxLength(50)
            .IsRequired();

        // Template (TEXT column for unlimited length)
        builder.Property(lt => lt.Template)
            .HasColumnName("template")
            .HasColumnType("text")
            .IsRequired();

        // Width in millimeters
        builder.Property(lt => lt.WidthMm)
            .HasColumnName("width_mm")
            .IsRequired();

        // Height in millimeters
        builder.Property(lt => lt.HeightMm)
            .HasColumnName("height_mm")
            .IsRequired();

        // IsActive flag
        builder.Property(lt => lt.IsActive)
            .HasColumnName("is_active")
            .IsRequired()
            .HasDefaultValue(true);

        // Add index for active templates
        builder.HasIndex(lt => lt.IsActive)
            .HasDatabaseName("ix_label_template_is_active")
            .HasFilter("is_active = true");

        // IsSystem flag
        builder.Property(lt => lt.IsSystem)
            .HasColumnName("is_system")
            .IsRequired()
            .HasDefaultValue(false);

        // Audit fields
        builder.Property(lt => lt.CreatedDateTime)
            .HasColumnName("created_date_time")
            .IsRequired();

        builder.Property(lt => lt.ModifiedDateTime)
            .HasColumnName("modified_date_time");

        builder.Property(lt => lt.CreatedByPersonAliasId)
            .HasColumnName("created_by_person_alias_id");

        builder.Property(lt => lt.ModifiedByPersonAliasId)
            .HasColumnName("modified_by_person_alias_id");
    }
}
