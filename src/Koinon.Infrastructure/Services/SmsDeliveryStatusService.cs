using Koinon.Application.Interfaces;
using Koinon.Domain.Enums;
using Koinon.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Koinon.Infrastructure.Services;

/// <summary>
/// Service for managing SMS delivery status updates from Twilio webhooks.
/// Handles correlation between sent messages and webhook callbacks.
/// </summary>
public class SmsDeliveryStatusService(
    KoinonDbContext context,
    ILogger<SmsDeliveryStatusService> logger) : ISmsDeliveryStatusService
{
    /// <summary>
    /// Updates a CommunicationRecipient with the ExternalMessageId after sending.
    /// Called by CommunicationSender after successful SMS send.
    /// </summary>
    public async Task SetExternalMessageIdAsync(
        int recipientId,
        string externalMessageId,
        CancellationToken cancellationToken = default)
    {
        var recipient = await context.CommunicationRecipients
            .FirstOrDefaultAsync(r => r.Id == recipientId, cancellationToken);

        if (recipient == null)
        {
            logger.LogWarning(
                "Recipient {RecipientId} not found when setting external message ID {MessageId}",
                recipientId,
                externalMessageId);
            return;
        }

        recipient.ExternalMessageId = externalMessageId;
        recipient.ModifiedDateTime = DateTime.UtcNow;

        await context.SaveChangesAsync(cancellationToken);

        logger.LogDebug(
            "Set external message ID {MessageId} for recipient {RecipientId}",
            externalMessageId,
            recipientId);
    }

    /// <summary>
    /// Updates delivery status from Twilio webhook callback.
    /// Maps Twilio status to our DeliveryStatus enum and updates recipient record.
    /// </summary>
    public async Task UpdateDeliveryStatusAsync(
        string externalMessageId,
        string status,
        int? errorCode,
        string? errorMessage,
        CancellationToken cancellationToken = default)
    {
        // Find recipient by external message ID (uses index for performance)
        var recipient = await context.CommunicationRecipients
            .FirstOrDefaultAsync(r => r.ExternalMessageId == externalMessageId, cancellationToken);

        if (recipient == null)
        {
            // This can happen if webhook arrives before DB save completes
            // Log as warning but don't throw - webhook will likely be retried
            logger.LogWarning(
                "Recipient not found for external message ID {MessageId} with status {Status}. " +
                "This may be a race condition with message send.",
                externalMessageId,
                status);
            return;
        }

        var previousStatus = recipient.Status;

        // Map Twilio status to our enum
        var newStatus = MapTwilioStatusToRecipientStatus(status);

        // Update recipient status
        recipient.Status = newStatus;
        recipient.ModifiedDateTime = DateTime.UtcNow;

        // Update delivered timestamp if transitioning to Delivered
        if (newStatus == CommunicationRecipientStatus.Delivered && recipient.DeliveredDateTime == null)
        {
            recipient.DeliveredDateTime = DateTime.UtcNow;
        }

        // Store error information if provided
        if (errorCode.HasValue)
        {
            recipient.ErrorCode = errorCode;
        }

        if (!string.IsNullOrEmpty(errorMessage))
        {
            recipient.ErrorMessage = errorMessage;
        }

        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Updated delivery status for recipient {RecipientId} (message {MessageId}): {PreviousStatus} -> {NewStatus}" +
            (errorCode.HasValue ? " (error code: {ErrorCode})" : ""),
            recipient.Id,
            externalMessageId,
            previousStatus,
            newStatus,
            errorCode);
    }

    /// <summary>
    /// Maps Twilio status string to our CommunicationRecipientStatus enum.
    /// </summary>
    /// <param name="twilioStatus">Twilio status (queued, sent, delivered, failed, undelivered)</param>
    /// <returns>Corresponding CommunicationRecipientStatus</returns>
    private static CommunicationRecipientStatus MapTwilioStatusToRecipientStatus(string twilioStatus)
    {
        return twilioStatus.ToLowerInvariant() switch
        {
            "queued" => CommunicationRecipientStatus.Pending,
            "sending" => CommunicationRecipientStatus.Pending,
            "sent" => CommunicationRecipientStatus.Pending, // Sent to carrier but not confirmed delivered
            "delivered" => CommunicationRecipientStatus.Delivered,
            "failed" => CommunicationRecipientStatus.Failed,
            "undelivered" => CommunicationRecipientStatus.Failed,
            _ => CommunicationRecipientStatus.Pending // Default for unknown statuses
        };
    }
}
