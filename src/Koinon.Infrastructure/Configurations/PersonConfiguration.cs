using Koinon.Domain.Entities;
using Koinon.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Koinon.Infrastructure.Configurations;

/// <summary>
/// Entity Framework Core configuration for the Person entity.
/// </summary>
public class PersonConfiguration : IEntityTypeConfiguration<Person>
{
    public void Configure(EntityTypeBuilder<Person> builder)
    {
        // Table name
        builder.ToTable("person");

        // Primary key
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("id");

        // Guid with unique index
        builder.Property(p => p.Guid)
            .HasColumnName("guid")
            .IsRequired();
        builder.HasIndex(p => p.Guid)
            .IsUnique()
            .HasDatabaseName("uix_person_guid");

        // Ignore computed properties
        builder.Ignore(p => p.IdKey);
        builder.Ignore(p => p.FullName);
        builder.Ignore(p => p.FullNameReversed);
        builder.Ignore(p => p.BirthDate);

        // System flag
        builder.Property(p => p.IsSystem)
            .HasColumnName("is_system")
            .IsRequired();

        // Foreign keys to DefinedValue
        builder.Property(p => p.RecordTypeValueId)
            .HasColumnName("record_type_value_id");

        builder.Property(p => p.RecordStatusValueId)
            .HasColumnName("record_status_value_id");
        builder.HasIndex(p => p.RecordStatusValueId)
            .HasDatabaseName("ix_person_record_status_value_id");

        builder.Property(p => p.RecordStatusReasonValueId)
            .HasColumnName("record_status_reason_value_id");

        builder.Property(p => p.ConnectionStatusValueId)
            .HasColumnName("connection_status_value_id");
        builder.HasIndex(p => p.ConnectionStatusValueId)
            .HasDatabaseName("ix_person_connection_status_value_id");

        // Notes
        builder.Property(p => p.ReviewReasonNote)
            .HasColumnName("review_reason_note");

        builder.Property(p => p.InactiveReasonNote)
            .HasColumnName("inactive_reason_note");

        builder.Property(p => p.SystemNote)
            .HasColumnName("system_note");

        // Deceased flag
        builder.Property(p => p.IsDeceased)
            .HasColumnName("is_deceased")
            .IsRequired();

        // Title and Suffix
        builder.Property(p => p.TitleValueId)
            .HasColumnName("title_value_id");

        builder.Property(p => p.SuffixValueId)
            .HasColumnName("suffix_value_id");

        // Name fields
        builder.Property(p => p.FirstName)
            .HasColumnName("first_name")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(p => p.NickName)
            .HasColumnName("nick_name")
            .HasMaxLength(50);

        builder.Property(p => p.MiddleName)
            .HasColumnName("middle_name")
            .HasMaxLength(50);

        builder.Property(p => p.LastName)
            .HasColumnName("last_name")
            .HasMaxLength(50)
            .IsRequired();

        // Composite index on name fields for searching
        builder.HasIndex(p => new { p.LastName, p.FirstName })
            .HasDatabaseName("ix_person_last_name_first_name");

        // Photo
        builder.Property(p => p.PhotoId)
            .HasColumnName("photo_id");

        // Password hash
        builder.Property(p => p.PasswordHash)
            .HasColumnName("password_hash")
            .HasMaxLength(512); // Base64 encoded hash with salt

        // Supervisor PIN hash
        builder.Property(p => p.SupervisorPinHash)
            .HasColumnName("supervisor_pin_hash")
            .HasMaxLength(512); // Base64 encoded hash with salt

        // Birth date components
        builder.Property(p => p.BirthDay)
            .HasColumnName("birth_day");

        builder.Property(p => p.BirthMonth)
            .HasColumnName("birth_month");

        builder.Property(p => p.BirthYear)
            .HasColumnName("birth_year");

        // Gender
        builder.Property(p => p.Gender)
            .HasColumnName("gender")
            .HasConversion<int>()
            .IsRequired();

        // Marital status
        builder.Property(p => p.MaritalStatusValueId)
            .HasColumnName("marital_status_value_id");

        // Anniversary
        builder.Property(p => p.AnniversaryDate)
            .HasColumnName("anniversary_date")
            .HasColumnType("date");

        // Graduation year
        builder.Property(p => p.GraduationYear)
            .HasColumnName("graduation_year");

        // Giving group
        builder.Property(p => p.GivingGroupId)
            .HasColumnName("giving_group_id");

        // Email fields
        builder.Property(p => p.Email)
            .HasColumnName("email")
            .HasMaxLength(75);

        builder.HasIndex(p => p.Email)
            .HasDatabaseName("ix_person_email")
            .HasFilter("email IS NOT NULL");

        builder.Property(p => p.IsEmailActive)
            .HasColumnName("is_email_active")
            .IsRequired();

        builder.Property(p => p.EmailNote)
            .HasColumnName("email_note");

        builder.Property(p => p.EmailPreference)
            .HasColumnName("email_preference")
            .HasConversion<int>()
            .IsRequired();

        // Communication preference
        builder.Property(p => p.CommunicationPreference)
            .HasColumnName("communication_preference");

        // Primary family (denormalized)
        builder.Property(p => p.PrimaryFamilyId)
            .HasColumnName("primary_family_id");
        builder.HasIndex(p => p.PrimaryFamilyId)
            .HasDatabaseName("ix_person_primary_family_id");

        // Primary campus (denormalized)
        builder.Property(p => p.PrimaryCampusId)
            .HasColumnName("primary_campus_id");
        builder.HasIndex(p => p.PrimaryCampusId)
            .HasDatabaseName("ix_person_primary_campus_id");

        // Allergy and special needs fields
        builder.Property(p => p.Allergies)
            .HasColumnName("allergies")
            .HasMaxLength(500); // Limited for label printing compatibility

        builder.Property(p => p.HasCriticalAllergies)
            .HasColumnName("has_critical_allergies")
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(p => p.SpecialNeeds)
            .HasColumnName("special_needs")
            .HasMaxLength(2000); // Supervisor notes can be longer

        // Audit fields
        builder.Property(p => p.CreatedDateTime)
            .HasColumnName("created_date_time")
            .IsRequired();

        builder.Property(p => p.ModifiedDateTime)
            .HasColumnName("modified_date_time");

        builder.Property(p => p.CreatedByPersonAliasId)
            .HasColumnName("created_by_person_alias_id");

        builder.Property(p => p.ModifiedByPersonAliasId)
            .HasColumnName("modified_by_person_alias_id");

        // Navigation properties
        builder.HasMany<PhoneNumber>()
            .WithOne(pn => pn.Person)
            .HasForeignKey(pn => pn.PersonId)
            .OnDelete(DeleteBehavior.Cascade);

        // Note: Full-text search vector configuration requires PostgreSQL-specific features
        // Implementation deferred until PostgreSQL provider setup is complete
    }
}
