using Koinon.Domain.Enums;

namespace Koinon.Domain.Entities;

/// <summary>
/// Represents a communication (email or SMS) sent to a group of recipients.
/// </summary>
public class Communication : Entity
{
    /// <summary>
    /// The type of communication (Email or SMS).
    /// </summary>
    public required CommunicationType CommunicationType { get; set; }

    /// <summary>
    /// The overall status of the communication.
    /// </summary>
    public CommunicationStatus Status { get; set; } = CommunicationStatus.Draft;

    /// <summary>
    /// The subject line for email communications.
    /// </summary>
    public string? Subject { get; set; }

    /// <summary>
    /// The body content of the communication (HTML for email, plain text for SMS).
    /// </summary>
    public required string Body { get; set; }

    /// <summary>
    /// The sender's email address (for email communications).
    /// </summary>
    public string? FromEmail { get; set; }

    /// <summary>
    /// The sender's display name (for email communications).
    /// </summary>
    public string? FromName { get; set; }

    /// <summary>
    /// Reply-to email address (optional).
    /// </summary>
    public string? ReplyToEmail { get; set; }

    /// <summary>
    /// Date and time when the communication was sent.
    /// </summary>
    public DateTime? SentDateTime { get; set; }

    /// <summary>
    /// Total number of recipients for this communication.
    /// </summary>
    public int RecipientCount { get; set; }

    /// <summary>
    /// Number of recipients who successfully received the communication.
    /// </summary>
    public int DeliveredCount { get; set; }

    /// <summary>
    /// Number of recipients for whom delivery failed.
    /// </summary>
    public int FailedCount { get; set; }

    /// <summary>
    /// Number of recipients who opened the communication (email only).
    /// </summary>
    public int OpenedCount { get; set; }

    /// <summary>
    /// Number of recipients who clicked links in the communication (email only).
    /// </summary>
    public int ClickedCount { get; set; }

    /// <summary>
    /// Optional notes about this communication.
    /// </summary>
    public string? Note { get; set; }

    // Navigation Properties

    /// <summary>
    /// Collection of individual recipients for this communication.
    /// </summary>
    public virtual ICollection<CommunicationRecipient> Recipients { get; set; } = new List<CommunicationRecipient>();
}
