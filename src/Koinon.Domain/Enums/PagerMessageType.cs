namespace Koinon.Domain.Enums;

/// <summary>
/// The type of message sent via the parent paging system.
/// </summary>
public enum PagerMessageType
{
    /// <summary>
    /// Standard message requesting parent to pick up their child.
    /// </summary>
    PickupNeeded = 0,

    /// <summary>
    /// Indicates the child needs attention but doesn't require immediate pickup
    /// (e.g., needs diaper change, is upset).
    /// </summary>
    NeedsAttention = 1,

    /// <summary>
    /// Notification that the service or event is ending soon.
    /// </summary>
    ServiceEnding = 2,

    /// <summary>
    /// Custom message text provided by staff.
    /// </summary>
    Custom = 3
}
