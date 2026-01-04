namespace Koinon.Application.Interfaces;

/// <summary>
/// Service for managing SMS delivery status updates from external providers (Twilio).
/// Handles correlation between sent messages and webhook callbacks.
/// </summary>
public interface ISmsDeliveryStatusService
{
    /// <summary>
    /// Updates a CommunicationRecipient with the ExternalMessageId after sending.
    /// Called by CommunicationSender after successful SMS send.
    /// </summary>
    /// <param name="recipientId">The ID of the CommunicationRecipient</param>
    /// <param name="externalMessageId">The external message ID (e.g., Twilio MessageSid)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SetExternalMessageIdAsync(int recipientId, string externalMessageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates delivery status from Twilio webhook callback.
    /// Called by TwilioWebhookController when receiving status updates.
    /// </summary>
    /// <param name="externalMessageId">The external message ID (Twilio MessageSid)</param>
    /// <param name="status">The Twilio status (e.g., "queued", "sent", "delivered", "failed", "undelivered")</param>
    /// <param name="errorCode">Optional Twilio error code for failed deliveries</param>
    /// <param name="errorMessage">Optional error message for failed deliveries</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task UpdateDeliveryStatusAsync(
        string externalMessageId,
        string status,
        int? errorCode,
        string? errorMessage,
        CancellationToken cancellationToken = default);
}
