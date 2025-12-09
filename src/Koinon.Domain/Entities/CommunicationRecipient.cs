using Koinon.Domain.Enums;

namespace Koinon.Domain.Entities;

/// <summary>
/// Represents an individual recipient of a communication.
/// Stores a snapshot of recipient information at the time of sending.
/// </summary>
public class CommunicationRecipient : Entity
{
    /// <summary>
    /// Foreign key to the parent Communication.
    /// </summary>
    public required int CommunicationId { get; set; }

    /// <summary>
    /// Foreign key to the Person who is the recipient.
    /// </summary>
    public required int PersonId { get; set; }

    /// <summary>
    /// The email address or phone number used for this recipient (snapshot at send time).
    /// </summary>
    public required string Address { get; set; }

    /// <summary>
    /// The recipient's name at the time of sending (snapshot).
    /// </summary>
    public string? RecipientName { get; set; }

    /// <summary>
    /// The delivery status for this recipient.
    /// </summary>
    public CommunicationRecipientStatus Status { get; set; } = CommunicationRecipientStatus.Pending;

    /// <summary>
    /// Date and time when the communication was delivered to this recipient.
    /// </summary>
    public DateTime? DeliveredDateTime { get; set; }

    /// <summary>
    /// Date and time when the recipient opened the communication (email only).
    /// </summary>
    public DateTime? OpenedDateTime { get; set; }

    /// <summary>
    /// Number of times the recipient opened the communication (email only).
    /// </summary>
    public int OpenCount { get; set; }

    /// <summary>
    /// Date and time when the recipient first clicked a link in the communication (email only).
    /// </summary>
    public DateTime? ClickedDateTime { get; set; }

    /// <summary>
    /// Number of times the recipient clicked links in the communication (email only).
    /// </summary>
    public int ClickCount { get; set; }

    /// <summary>
    /// Error message if delivery failed for this recipient.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Optional foreign key to the Group this recipient was targeted through.
    /// </summary>
    public int? GroupId { get; set; }

    // Navigation Properties

    /// <summary>
    /// The parent Communication.
    /// </summary>
    public virtual Communication? Communication { get; set; }

    /// <summary>
    /// The Person who is the recipient.
    /// </summary>
    public virtual Person? Person { get; set; }

    /// <summary>
    /// The Group this recipient was targeted through (if applicable).
    /// </summary>
    public virtual Group? Group { get; set; }
}
