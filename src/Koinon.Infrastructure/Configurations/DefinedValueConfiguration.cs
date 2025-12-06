using Koinon.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Koinon.Infrastructure.Configurations;

/// <summary>
/// Entity Framework Core configuration for the DefinedValue entity.
/// </summary>
public class DefinedValueConfiguration : IEntityTypeConfiguration<DefinedValue>
{
    public void Configure(EntityTypeBuilder<DefinedValue> builder)
    {
        builder.ToTable("defined_value");

        // Primary key
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        // Guid with unique constraint
        builder.Property(e => e.Guid)
            .HasColumnName("guid")
            .IsRequired();
        builder.HasIndex(e => e.Guid)
            .IsUnique()
            .HasDatabaseName("uix_defined_value_guid");

        // IdKey is computed, not stored
        builder.Ignore(e => e.IdKey);

        // Core properties
        builder.Property(e => e.DefinedTypeId)
            .HasColumnName("defined_type_id")
            .IsRequired();

        builder.Property(e => e.Value)
            .HasColumnName("value")
            .HasMaxLength(250)
            .IsRequired();

        builder.Property(e => e.Description)
            .HasColumnName("description")
            .HasMaxLength(1000);

        builder.Property(e => e.Order)
            .HasColumnName("order")
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(e => e.IsActive)
            .HasColumnName("is_active")
            .IsRequired()
            .HasDefaultValue(true);

        // Audit properties
        builder.Property(e => e.CreatedDateTime)
            .HasColumnName("created_date_time")
            .IsRequired();

        builder.Property(e => e.ModifiedDateTime)
            .HasColumnName("modified_date_time");

        builder.Property(e => e.CreatedByPersonAliasId)
            .HasColumnName("created_by_person_alias_id");

        builder.Property(e => e.ModifiedByPersonAliasId)
            .HasColumnName("modified_by_person_alias_id");

        // Indexes
        builder.HasIndex(e => e.DefinedTypeId)
            .HasDatabaseName("ix_defined_value_defined_type_id");

        builder.HasIndex(e => e.Value)
            .HasDatabaseName("ix_defined_value_value");

        builder.HasIndex(e => e.IsActive)
            .HasDatabaseName("ix_defined_value_is_active");

        builder.HasIndex(e => e.Order)
            .HasDatabaseName("ix_defined_value_order");

        // Composite index for common queries (type + active + order)
        builder.HasIndex(e => new { e.DefinedTypeId, e.IsActive, e.Order })
            .HasDatabaseName("ix_defined_value_type_active_order");

        // Relationships configured from DefinedType side
    }
}
