using Koinon.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Koinon.Infrastructure.Configurations;

/// <summary>
/// Entity Framework Core configuration for the PersonDuplicateIgnore entity.
/// </summary>
public class PersonDuplicateIgnoreConfiguration : IEntityTypeConfiguration<PersonDuplicateIgnore>
{
    public void Configure(EntityTypeBuilder<PersonDuplicateIgnore> builder)
    {
        // Table name
        builder.ToTable("person_duplicate_ignore");

        // Primary key
        builder.HasKey(pdi => pdi.Id);
        builder.Property(pdi => pdi.Id).HasColumnName("id");

        // Guid with unique index
        builder.Property(pdi => pdi.Guid)
            .HasColumnName("guid")
            .IsRequired();
        builder.HasIndex(pdi => pdi.Guid)
            .IsUnique()
            .HasDatabaseName("uix_person_duplicate_ignore_guid");

        // Ignore computed property
        builder.Ignore(pdi => pdi.IdKey);

        // Foreign key: PersonId1
        builder.Property(pdi => pdi.PersonId1)
            .HasColumnName("person_id_1")
            .IsRequired();

        builder.HasOne(pdi => pdi.Person1)
            .WithMany()
            .HasForeignKey(pdi => pdi.PersonId1)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(pdi => pdi.PersonId1)
            .HasDatabaseName("ix_person_duplicate_ignore_person_id_1");

        // Foreign key: PersonId2
        builder.Property(pdi => pdi.PersonId2)
            .HasColumnName("person_id_2")
            .IsRequired();

        builder.HasOne(pdi => pdi.Person2)
            .WithMany()
            .HasForeignKey(pdi => pdi.PersonId2)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(pdi => pdi.PersonId2)
            .HasDatabaseName("ix_person_duplicate_ignore_person_id_2");

        // UNIQUE constraint on (PersonId1, PersonId2) to prevent duplicate pairs
        builder.HasIndex(pdi => new { pdi.PersonId1, pdi.PersonId2 })
            .IsUnique()
            .HasDatabaseName("uix_person_duplicate_ignore_person_ids");

        // Foreign key: MarkedByPersonId (nullable)
        builder.Property(pdi => pdi.MarkedByPersonId)
            .HasColumnName("marked_by_person_id");

        builder.HasOne(pdi => pdi.MarkedByPerson)
            .WithMany()
            .HasForeignKey(pdi => pdi.MarkedByPersonId)
            .OnDelete(DeleteBehavior.Restrict);

        // MarkedDateTime
        builder.Property(pdi => pdi.MarkedDateTime)
            .HasColumnName("marked_date_time")
            .IsRequired();

        // Reason (optional explanation field)
        builder.Property(pdi => pdi.Reason)
            .HasColumnName("reason");

        // Audit fields
        builder.Property(pdi => pdi.CreatedDateTime)
            .HasColumnName("created_date_time")
            .IsRequired();

        builder.Property(pdi => pdi.ModifiedDateTime)
            .HasColumnName("modified_date_time");

        builder.Property(pdi => pdi.CreatedByPersonAliasId)
            .HasColumnName("created_by_person_alias_id");

        builder.Property(pdi => pdi.ModifiedByPersonAliasId)
            .HasColumnName("modified_by_person_alias_id");
    }
}
