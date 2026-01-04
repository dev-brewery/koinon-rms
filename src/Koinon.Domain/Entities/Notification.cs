using Koinon.Domain.Enums;

namespace Koinon.Domain.Entities;

/// <summary>
/// Represents an in-app notification for a user.
/// Notifications are real-time alerts about events like check-ins,
/// communication status updates, or system announcements.
/// </summary>
public class Notification : Entity
{
    /// <summary>
    /// Foreign key to the Person who receives this notification.
    /// </summary>
    public required int PersonId { get; set; }

    /// <summary>
    /// The type of notification (CheckinAlert, CommunicationStatus, etc.).
    /// </summary>
    public NotificationType NotificationType { get; set; }

    /// <summary>
    /// Short title for the notification.
    /// Maximum length: 200 characters.
    /// </summary>
    public required string Title { get; set; }

    /// <summary>
    /// Full message content of the notification.
    /// Maximum length: 1000 characters.
    /// </summary>
    public required string Message { get; set; }

    /// <summary>
    /// Whether the user has read this notification.
    /// </summary>
    public bool IsRead { get; set; }

    /// <summary>
    /// When the notification was marked as read.
    /// Null if not yet read.
    /// </summary>
    public DateTime? ReadDateTime { get; set; }

    /// <summary>
    /// Optional URL to navigate to when the notification is clicked.
    /// Maximum length: 500 characters.
    /// </summary>
    public string? ActionUrl { get; set; }

    /// <summary>
    /// Optional JSON metadata for additional context.
    /// Can contain entity references, additional details, etc.
    /// </summary>
    public string? MetadataJson { get; set; }

    /// <summary>
    /// Navigation property to the Person who receives this notification.
    /// </summary>
    public virtual Person? Person { get; set; }
}
