using Koinon.Domain.Enums;

namespace Koinon.Domain.Entities;

/// <summary>
/// Records an SMS message sent to a parent via the pager system.
/// Tracks delivery status and provides audit trail for all parent notifications.
/// </summary>
public class PagerMessage : Entity
{
    /// <summary>
    /// Foreign key to the pager assignment this message is for.
    /// </summary>
    public required int PagerAssignmentId { get; set; }

    /// <summary>
    /// Navigation property to the pager assignment.
    /// </summary>
    public virtual PagerAssignment? PagerAssignment { get; set; }

    /// <summary>
    /// Foreign key to the person (staff member) who sent this page.
    /// </summary>
    public required int SentByPersonId { get; set; }

    /// <summary>
    /// Navigation property to the person who sent this page.
    /// </summary>
    public virtual Person? SentByPerson { get; set; }

    /// <summary>
    /// The type of message being sent (pickup, attention needed, etc.).
    /// </summary>
    public required PagerMessageType MessageType { get; set; }

    /// <summary>
    /// The actual text content of the SMS message sent to the parent.
    /// </summary>
    public required string MessageText { get; set; }

    /// <summary>
    /// The phone number the message was sent to.
    /// </summary>
    public required string PhoneNumber { get; set; }

    /// <summary>
    /// Twilio's unique identifier for this message (used for status callbacks).
    /// </summary>
    public string? TwilioMessageSid { get; set; }

    /// <summary>
    /// Current delivery status of the message.
    /// </summary>
    public PagerMessageStatus Status { get; set; } = PagerMessageStatus.Pending;

    /// <summary>
    /// Date and time when the message was sent to Twilio.
    /// </summary>
    public DateTime? SentDateTime { get; set; }

    /// <summary>
    /// Date and time when Twilio confirmed delivery to the recipient's phone.
    /// </summary>
    public DateTime? DeliveredDateTime { get; set; }

    /// <summary>
    /// Description of why message delivery failed (if Status is Failed).
    /// </summary>
    public string? FailureReason { get; set; }
}
