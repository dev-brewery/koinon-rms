namespace Koinon.Domain.Enums;

/// <summary>
/// Represents the status of a communication for an individual recipient.
/// </summary>
public enum CommunicationRecipientStatus
{
    /// <summary>
    /// Communication is pending delivery to this recipient.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Communication has been delivered to this recipient.
    /// </summary>
    Delivered = 1,

    /// <summary>
    /// Communication failed to deliver to this recipient.
    /// </summary>
    Failed = 2,

    /// <summary>
    /// Recipient has opened the communication (email tracking).
    /// </summary>
    Opened = 3
}
