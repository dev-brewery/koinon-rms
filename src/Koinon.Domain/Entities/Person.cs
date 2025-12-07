using Koinon.Domain.Enums;

namespace Koinon.Domain.Entities;

/// <summary>
/// Represents an individual person in the system.
/// This is the central entity for managing people in the church management system.
/// </summary>
public class Person : Entity
{
    /// <summary>
    /// Indicates whether this is a system-generated account (not editable by users).
    /// </summary>
    public bool IsSystem { get; set; }

    /// <summary>
    /// Foreign key to DefinedValue indicating the record type (Person vs Business).
    /// </summary>
    public int? RecordTypeValueId { get; set; }

    /// <summary>
    /// Foreign key to DefinedValue indicating the record status (Active, Inactive, Pending).
    /// </summary>
    public int? RecordStatusValueId { get; set; }

    /// <summary>
    /// Foreign key to DefinedValue indicating the reason for the current record status.
    /// </summary>
    public int? RecordStatusReasonValueId { get; set; }

    /// <summary>
    /// Foreign key to DefinedValue indicating the connection status (Member, Attendee, Visitor, Prospect).
    /// </summary>
    public int? ConnectionStatusValueId { get; set; }

    /// <summary>
    /// Internal note related to review or status reasons.
    /// </summary>
    public string? ReviewReasonNote { get; set; }

    /// <summary>
    /// Indicates whether the person is deceased.
    /// </summary>
    public bool IsDeceased { get; set; }

    /// <summary>
    /// Foreign key to DefinedValue for title (Mr., Mrs., Dr., etc.).
    /// </summary>
    public int? TitleValueId { get; set; }

    /// <summary>
    /// Person's legal first name (required).
    /// </summary>
    public required string FirstName { get; set; }

    /// <summary>
    /// Person's preferred name or nickname (used for display if present).
    /// </summary>
    public string? NickName { get; set; }

    /// <summary>
    /// Person's middle name.
    /// </summary>
    public string? MiddleName { get; set; }

    /// <summary>
    /// Person's last name (required).
    /// </summary>
    public required string LastName { get; set; }

    /// <summary>
    /// Foreign key to DefinedValue for suffix (Jr., Sr., III, etc.).
    /// </summary>
    public int? SuffixValueId { get; set; }

    /// <summary>
    /// Foreign key to BinaryFile for the person's photo (will be added in future work unit).
    /// </summary>
    public int? PhotoId { get; set; }

    /// <summary>
    /// Argon2id hash of the user's password. Format: Base64(salt[16] + hash[32])
    /// </summary>
    public string? PasswordHash { get; set; }

    /// <summary>
    /// Argon2id hash of the supervisor PIN (4-6 digits). Format: Base64(salt[16] + hash[32])
    /// Used for kiosk supervisor mode authentication.
    /// </summary>
    public string? SupervisorPinHash { get; set; }

    /// <summary>
    /// Day component of birth date (1-31).
    /// </summary>
    public int? BirthDay { get; set; }

    /// <summary>
    /// Month component of birth date (1-12).
    /// </summary>
    public int? BirthMonth { get; set; }

    /// <summary>
    /// Year component of birth date.
    /// </summary>
    public int? BirthYear { get; set; }

    /// <summary>
    /// Computed birth date from individual components.
    /// Returns null if any component is missing.
    /// </summary>
    public DateOnly? BirthDate
    {
        get
        {
            if (BirthYear.HasValue && BirthMonth.HasValue && BirthDay.HasValue)
            {
                try
                {
                    return new DateOnly(BirthYear.Value, BirthMonth.Value, BirthDay.Value);
                }
                catch
                {
                    return null;
                }
            }
            return null;
        }
    }

    /// <summary>
    /// Person's gender.
    /// </summary>
    public Gender Gender { get; set; } = Gender.Unknown;

    /// <summary>
    /// Foreign key to DefinedValue for marital status.
    /// </summary>
    public int? MaritalStatusValueId { get; set; }

    /// <summary>
    /// Wedding anniversary date.
    /// </summary>
    public DateOnly? AnniversaryDate { get; set; }

    /// <summary>
    /// Year of high school graduation (used for age-based group placement).
    /// </summary>
    public int? GraduationYear { get; set; }

    /// <summary>
    /// Foreign key to Group representing the giving unit (typically a family).
    /// </summary>
    public int? GivingGroupId { get; set; }

    /// <summary>
    /// Person's email address.
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Indicates whether the email address is active and verified.
    /// </summary>
    public bool IsEmailActive { get; set; } = true;

    /// <summary>
    /// Note related to email communication (e.g., reason for bounced emails).
    /// </summary>
    public string? EmailNote { get; set; }

    /// <summary>
    /// Person's email communication preference.
    /// </summary>
    public EmailPreference EmailPreference { get; set; } = EmailPreference.EmailAllowed;

    /// <summary>
    /// Communication preference (for future use with multiple channels).
    /// </summary>
    public int? CommunicationPreference { get; set; }

    /// <summary>
    /// Note explaining why the person's record is inactive.
    /// </summary>
    public string? InactiveReasonNote { get; set; }

    /// <summary>
    /// Internal system note (not visible to general users).
    /// </summary>
    public string? SystemNote { get; set; }

    /// <summary>
    /// Foreign key to Group representing the person's primary family (denormalized for performance).
    /// </summary>
    public int? PrimaryFamilyId { get; set; }

    /// <summary>
    /// Foreign key to Campus representing the person's primary campus (denormalized for performance).
    /// </summary>
    public int? PrimaryCampusId { get; set; }

    /// <summary>
    /// Computed full name using either NickName or FirstName, whichever is appropriate.
    /// </summary>
    public string FullName
    {
        get
        {
            var displayName = string.IsNullOrWhiteSpace(NickName) ? FirstName : NickName;
            return $"{displayName} {LastName}".Trim();
        }
    }

    /// <summary>
    /// Computed full name in reversed format (Last, First) for alphabetical sorting.
    /// </summary>
    public string FullNameReversed
    {
        get
        {
            var displayName = string.IsNullOrWhiteSpace(NickName) ? FirstName : NickName;
            return $"{LastName}, {displayName}".Trim();
        }
    }

    // Navigation properties

    /// <summary>
    /// Navigation property to the record status defined value.
    /// </summary>
    public virtual DefinedValue? RecordStatusValue { get; set; }

    /// <summary>
    /// Navigation property to the connection status defined value.
    /// </summary>
    public virtual DefinedValue? ConnectionStatusValue { get; set; }

    /// <summary>
    /// Navigation property to the primary family group (denormalized for performance).
    /// </summary>
    public virtual Group? PrimaryFamily { get; set; }

    /// <summary>
    /// Navigation property to the primary campus (denormalized for performance).
    /// </summary>
    public virtual Campus? PrimaryCampus { get; set; }

    /// <summary>
    /// Collection of all group memberships for this person.
    /// </summary>
    public virtual ICollection<GroupMember> GroupMemberships { get; set; } = new List<GroupMember>();

    /// <summary>
    /// Collection of phone numbers for this person.
    /// </summary>
    public virtual ICollection<PhoneNumber> PhoneNumbers { get; set; } = new List<PhoneNumber>();

    /// <summary>
    /// Collection of person aliases for this person.
    /// PersonAlias provides historical tracking of name changes and alternate identifiers.
    /// </summary>
    public virtual ICollection<PersonAlias> PersonAliases { get; set; } = new List<PersonAlias>();
}
