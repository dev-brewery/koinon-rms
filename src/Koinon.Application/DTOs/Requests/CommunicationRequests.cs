namespace Koinon.Application.DTOs.Requests;

/// <summary>
/// Request to schedule a communication for future delivery.
/// </summary>
public record ScheduleCommunicationRequest
{
    /// <summary>
    /// The date and time when the communication should be sent.
    /// </summary>
    public required DateTime ScheduledDateTime { get; init; }
}
