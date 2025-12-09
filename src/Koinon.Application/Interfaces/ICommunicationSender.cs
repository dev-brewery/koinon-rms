namespace Koinon.Application.Interfaces;

/// <summary>
/// Service for sending communications (Email and SMS) to recipients.
/// Processes pending communications and updates delivery status.
/// </summary>
public interface ICommunicationSender
{
    /// <summary>
    /// Sends a communication to all its recipients.
    /// </summary>
    /// <param name="communicationId">The ID of the communication to send.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task representing the async operation.</returns>
    Task SendCommunicationAsync(int communicationId, CancellationToken cancellationToken = default);
}
