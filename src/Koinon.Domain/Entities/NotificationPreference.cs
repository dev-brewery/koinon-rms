using Koinon.Domain.Enums;

namespace Koinon.Domain.Entities;

/// <summary>
/// Stores per-user preferences for which notification types are enabled.
/// If a preference doesn't exist for a notification type, it defaults to enabled.
/// </summary>
public class NotificationPreference : Entity
{
    /// <summary>
    /// Foreign key to the Person who owns this preference.
    /// </summary>
    public required int PersonId { get; set; }

    /// <summary>
    /// The notification type this preference applies to.
    /// </summary>
    public NotificationType NotificationType { get; set; }

    /// <summary>
    /// Whether this notification type is enabled for the user.
    /// When false, notifications of this type will not be created.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Navigation property to the Person who owns this preference.
    /// </summary>
    public virtual Person? Person { get; set; }
}
