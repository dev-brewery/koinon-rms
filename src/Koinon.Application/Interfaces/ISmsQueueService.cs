using Koinon.Application.DTOs.Communications;

namespace Koinon.Application.Interfaces;

/// <summary>
/// Service for queuing SMS/MMS messages for background processing via Hangfire.
/// Provides asynchronous message delivery with retry support and webhook status tracking.
/// </summary>
public interface ISmsQueueService
{
    /// <summary>
    /// Queues an SMS message for background delivery.
    /// </summary>
    /// <param name="toPhoneNumber">The destination phone number in E.164 format (e.g., +15551234567)</param>
    /// <param name="message">The message body to send</param>
    /// <returns>Hangfire job ID for tracking the queued message</returns>
    string QueueSmsAsync(string toPhoneNumber, string message);

    /// <summary>
    /// Queues an MMS message with media attachments for background delivery.
    /// </summary>
    /// <param name="toPhoneNumber">The destination phone number in E.164 format (e.g., +15551234567)</param>
    /// <param name="message">The message body to send</param>
    /// <param name="mediaUrls">URLs of media files to attach (images, videos, etc.)</param>
    /// <returns>Hangfire job ID for tracking the queued message</returns>
    string QueueMmsAsync(string toPhoneNumber, string message, IEnumerable<string> mediaUrls);

    /// <summary>
    /// Processes a queued SMS/MMS message. Called by Hangfire background job.
    /// Note: Hangfire injects CancellationToken at execution time.
    /// </summary>
    /// <param name="dto">Message details to send</param>
    /// <param name="cancellationToken">Cancellation token injected by Hangfire</param>
    /// <returns>Task representing the send operation</returns>
    Task ProcessQueuedMessageAsync(QueuedSmsDto dto, CancellationToken cancellationToken);
}
