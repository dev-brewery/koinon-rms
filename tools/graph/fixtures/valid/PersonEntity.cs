using Koinon.Domain.Enums;

namespace Koinon.Domain.Entities;

/// <summary>
/// Represents a person entity for testing graph generation.
/// Follows project conventions: inherits from Entity, has standard properties.
/// </summary>
public class Person : Entity
{
    /// <summary>
    /// Indicates whether this is a system-generated account.
    /// </summary>
    public bool IsSystem { get; set; }

    /// <summary>
    /// Person's first name (required).
    /// </summary>
    public required string FirstName { get; set; }

    /// <summary>
    /// Person's nickname.
    /// </summary>
    public string? NickName { get; set; }

    /// <summary>
    /// Person's last name (required).
    /// </summary>
    public required string LastName { get; set; }

    /// <summary>
    /// Person's email address.
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Person's gender.
    /// </summary>
    public Gender Gender { get; set; } = Gender.Unknown;

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
    /// Computed birth date from components.
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
    /// Computed full name.
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
    /// Navigation property to phone numbers.
    /// </summary>
    public virtual ICollection<PhoneNumber> PhoneNumbers { get; set; } = new List<PhoneNumber>();
}
