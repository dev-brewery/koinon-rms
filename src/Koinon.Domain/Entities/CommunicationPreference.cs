using Koinon.Domain.Enums;

namespace Koinon.Domain.Entities;

/// <summary>
/// Represents a person's communication preference for a specific communication type.
/// Tracks opt-in/opt-out status for email and SMS communications.
/// </summary>
public class CommunicationPreference : Entity
{
    /// <summary>
    /// Foreign key to the Person who owns this preference.
    /// </summary>
    public required int PersonId { get; set; }

    /// <summary>
    /// The type of communication this preference applies to (Email or SMS).
    /// </summary>
    public required CommunicationType CommunicationType { get; set; }

    /// <summary>
    /// Indicates whether the person has opted out of this communication type.
    /// </summary>
    public required bool IsOptedOut { get; set; }

    /// <summary>
    /// Date and time when the person opted out (null if not opted out).
    /// </summary>
    public DateTime? OptOutDateTime { get; set; }

    /// <summary>
    /// Optional reason provided when the person opted out.
    /// </summary>
    public string? OptOutReason { get; set; }

    // Navigation Properties

    /// <summary>
    /// The Person who owns this communication preference.
    /// </summary>
    public virtual Person? Person { get; set; }
}
