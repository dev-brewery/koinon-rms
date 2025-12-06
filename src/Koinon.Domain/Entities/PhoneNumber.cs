namespace Koinon.Domain.Entities;

/// <summary>
/// Represents a phone number associated with a person.
/// Supports various number types (Mobile, Home, Work), country codes, extensions,
/// and SMS messaging capabilities.
/// </summary>
public class PhoneNumber : Entity
{
    /// <summary>
    /// Foreign key to the Person who owns this phone number (required).
    /// </summary>
    public int PersonId { get; set; }

    /// <summary>
    /// The phone number itself (required).
    /// Stored as-is without formatting removal to preserve user input.
    /// Maximum 20 characters.
    /// </summary>
    public required string Number { get; set; }

    /// <summary>
    /// Normalized phone number with all non-digit characters removed.
    /// Used for fast searching and matching.
    /// Computed automatically from Number.
    /// </summary>
    public string NumberNormalized { get; set; } = string.Empty;

    /// <summary>
    /// Country calling code (e.g., "1" for US/Canada, "44" for UK).
    /// Maximum 3 characters.
    /// </summary>
    public string? CountryCode { get; set; }

    /// <summary>
    /// Phone extension for work or organizational numbers.
    /// Maximum 20 characters.
    /// </summary>
    public string? Extension { get; set; }

    /// <summary>
    /// Foreign key to DefinedValue indicating the phone number type
    /// (e.g., Mobile, Home, Work).
    /// </summary>
    public int? NumberTypeValueId { get; set; }

    /// <summary>
    /// Indicates whether this phone number can receive SMS text messages.
    /// Typically true for mobile numbers, false for landlines.
    /// </summary>
    public bool IsMessagingEnabled { get; set; }

    /// <summary>
    /// Indicates whether this phone number should be hidden from public directories
    /// and listings (similar to an unlisted phone number).
    /// </summary>
    public bool IsUnlisted { get; set; }

    /// <summary>
    /// Optional description or note about this phone number
    /// (e.g., "Primary contact", "Work direct line", "Emergency only").
    /// Maximum 500 characters.
    /// </summary>
    public string? Description { get; set; }

    // Navigation properties

    /// <summary>
    /// Navigation property to the Person who owns this phone number.
    /// </summary>
    public virtual Person? Person { get; set; }

    /// <summary>
    /// Navigation property to the DefinedValue representing the phone number type.
    /// </summary>
    public virtual DefinedValue? NumberTypeValue { get; set; }
}
