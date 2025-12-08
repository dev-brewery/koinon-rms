namespace Koinon.Domain.Enums;

/// <summary>
/// Delivery status of a pager message sent via SMS.
/// </summary>
public enum PagerMessageStatus
{
    /// <summary>
    /// Message has been created but not yet sent to SMS provider.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Message has been accepted by SMS provider (Twilio) for delivery.
    /// </summary>
    Sent = 1,

    /// <summary>
    /// Message was successfully delivered to the recipient's phone.
    /// </summary>
    Delivered = 2,

    /// <summary>
    /// Message delivery failed (invalid number, network error, etc.).
    /// </summary>
    Failed = 3
}
