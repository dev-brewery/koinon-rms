namespace Koinon.Application.Interfaces;

/// <summary>
/// Service interface for processing scheduled communications.
/// Handles transitioning scheduled communications to pending status
/// and filtering out recipients who have opted out.
/// </summary>
public interface IScheduledCommunicationProcessor
{
    /// <summary>
    /// Processes scheduled communications that are due for sending.
    /// Transitions communications from Scheduled to Pending status,
    /// checking opt-out preferences and filtering recipients.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The count of communications successfully processed.</returns>
    Task<int> ProcessScheduledCommunicationsAsync(CancellationToken ct = default);
}
