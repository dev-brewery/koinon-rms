namespace Koinon.Domain.Enums;

/// <summary>
/// Represents the overall status of a communication.
/// </summary>
public enum CommunicationStatus
{
    /// <summary>
    /// Communication is being drafted and has not been sent.
    /// </summary>
    Draft = 0,

    /// <summary>
    /// Communication is queued and waiting to be sent.
    /// </summary>
    Pending = 1,

    /// <summary>
    /// Communication has been sent to all recipients.
    /// </summary>
    Sent = 2,

    /// <summary>
    /// Communication failed to send.
    /// </summary>
    Failed = 3
}
