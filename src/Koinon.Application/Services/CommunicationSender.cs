using Koinon.Application.Interfaces;
using Koinon.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Koinon.Application.Services;

/// <summary>
/// Service for sending communications (Email and SMS) to recipients.
/// Processes pending communications and updates delivery status.
/// </summary>
public class CommunicationSender(
    IApplicationDbContext context,
    ISmsService smsService,
    IEmailSender emailSender,
    IMergeFieldService mergeFieldService,
    ISmsDeliveryStatusService smsDeliveryStatusService,
    ILogger<CommunicationSender> logger) : ICommunicationSender
{
    /// <summary>
    /// Sends a communication to all its recipients.
    /// Uses atomic update to prevent race conditions, then processes each recipient
    /// and updates delivery counts.
    /// </summary>
    public async Task SendCommunicationAsync(int communicationId, CancellationToken cancellationToken = default)
    {
        // BLOCKER FIX: Use atomic UPDATE with WHERE clause to prevent TOCTOU race condition
        // This approach works with PostgreSQL and uses fallback for InMemory tests
        int rowsAffected;
        var isInMemory = context.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory";

        if (!isInMemory)
        {
            // Production: Use raw SQL for atomic update (works with PostgreSQL)
            var sentDateTime = DateTime.UtcNow;
            rowsAffected = await context.Database.ExecuteSqlRawAsync(
                @"UPDATE communication
                  SET status = {0}, sent_date_time = {1}, modified_date_time = {2}
                  WHERE id = {3} AND status = {4}",
                (int)CommunicationStatus.Sent,
                sentDateTime,
                sentDateTime,
                communicationId,
                (int)CommunicationStatus.Pending);
        }
        else
        {
            // Tests: Use traditional check-and-update for InMemory database
            var tempComm = await context.Communications
                .FirstOrDefaultAsync(c => c.Id == communicationId, cancellationToken);

            if (tempComm != null && tempComm.Status == CommunicationStatus.Pending)
            {
                tempComm.Status = CommunicationStatus.Sent;
                tempComm.SentDateTime = DateTime.UtcNow;
                await context.SaveChangesAsync(cancellationToken);
                rowsAffected = 1;
            }
            else
            {
                rowsAffected = 0;
            }
        }

        if (rowsAffected == 0)
        {
            // Either doesn't exist or not Pending - another thread got it
            logger.LogDebug("Communication {CommunicationId} not found or already processed", communicationId);
            return;
        }

        // Now load with recipients for processing
        var communication = await context.Communications
            .Include(c => c.Recipients)
                .ThenInclude(cr => cr.Person)
            .FirstOrDefaultAsync(c => c.Id == communicationId, cancellationToken);

        if (communication == null)
        {
            // Should not happen, but handle defensively
            logger.LogWarning("Communication {CommunicationId} disappeared after status update", communicationId);
            return;
        }

        logger.LogInformation(
            "Starting send for Communication {CommunicationId} ({Type}) with {RecipientCount} recipients",
            communicationId,
            communication.CommunicationType,
            communication.Recipients.Count);

        // Process each recipient
        int deliveredCount = 0;
        int failedCount = 0;

        foreach (var recipient in communication.Recipients)
        {
            if (recipient.Status != CommunicationRecipientStatus.Pending)
            {
                // Skip recipients already processed
                continue;
            }

            bool success;
            string? errorMessage = null;
            string? messageId = null;

            try
            {
                // Personalize content with merge fields
                string personalizedBody = communication.Body;
                string? personalizedSubject = communication.Subject;

                if (recipient.Person != null)
                {
                    personalizedBody = mergeFieldService.ReplaceMergeFields(communication.Body, recipient.Person);

                    if (communication.CommunicationType == CommunicationType.Email && communication.Subject != null)
                    {
                        personalizedSubject = mergeFieldService.ReplaceMergeFields(communication.Subject, recipient.Person);
                    }

                    logger.LogDebug(
                        "Personalized content for recipient {RecipientId} (Person: {PersonId})",
                        recipient.Id,
                        recipient.PersonId);
                }
                else
                {
                    logger.LogWarning(
                        "Person not loaded for recipient {RecipientId} (PersonId: {PersonId}), skipping merge field replacement",
                        recipient.Id,
                        recipient.PersonId);
                }

                if (communication.CommunicationType == CommunicationType.Sms)
                {
                    // Capture message ID for webhook correlation
                    (success, messageId, errorMessage) = await SendSmsToRecipientAsync(
                        recipient.Address,
                        personalizedBody,
                        cancellationToken);
                }
                else // Email
                {
                    success = await SendEmailToRecipientAsync(
                        recipient.Address,
                        recipient.RecipientName,
                        communication.FromEmail ?? "noreply@koinon.app",
                        communication.FromName,
                        personalizedSubject ?? "",
                        personalizedBody,
                        communication.ReplyToEmail,
                        cancellationToken);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Unexpected error sending {Type} to recipient {RecipientId} ({Address})",
                    communication.CommunicationType,
                    recipient.Id,
                    recipient.Address);
                success = false;
                errorMessage = $"Unexpected error: {ex.Message}";
            }

            // Update recipient status
            if (success)
            {
                recipient.Status = CommunicationRecipientStatus.Delivered;
                recipient.DeliveredDateTime = DateTime.UtcNow;
                recipient.ErrorMessage = null;
                deliveredCount++;

                // Persist external message ID for SMS webhook correlation
                if (communication.CommunicationType == CommunicationType.Sms && !string.IsNullOrEmpty(messageId))
                {
                    await smsDeliveryStatusService.SetExternalMessageIdAsync(
                        recipient.Id,
                        messageId,
                        cancellationToken);

                    logger.LogDebug(
                        "Successfully sent {Type} to {Address} (MessageId: {MessageId})",
                        communication.CommunicationType,
                        recipient.Address,
                        messageId);
                }
                else
                {
                    logger.LogDebug(
                        "Successfully sent {Type} to {Address}",
                        communication.CommunicationType,
                        recipient.Address);
                }
            }
            else
            {
                recipient.Status = CommunicationRecipientStatus.Failed;
                recipient.ErrorMessage = errorMessage ?? "Delivery failed";
                failedCount++;

                logger.LogWarning(
                    "Failed to send {Type} to {Address}: {ErrorMessage}",
                    communication.CommunicationType,
                    recipient.Address,
                    recipient.ErrorMessage);
            }

            // CRITICAL FIX #1: Save after each recipient to preserve partial progress
            await context.SaveChangesAsync(cancellationToken);
        }

        // Update communication delivery counts
        communication.DeliveredCount = deliveredCount;
        communication.FailedCount = failedCount;

        // If all recipients failed, set communication status to Failed
        if (failedCount > 0 && deliveredCount == 0)
        {
            communication.Status = CommunicationStatus.Failed;
            logger.LogWarning(
                "All recipients failed for Communication {CommunicationId}. Marking as Failed",
                communicationId);
        }

        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Completed sending Communication {CommunicationId}. Delivered: {Delivered}, Failed: {Failed}",
            communicationId,
            deliveredCount,
            failedCount);
    }

    /// <summary>
    /// Sends an SMS to a single recipient.
    /// Returns success status, message ID, and error message if failed.
    /// </summary>
    private async Task<(bool Success, string? MessageId, string? ErrorMessage)> SendSmsToRecipientAsync(
        string phoneNumber,
        string message,
        CancellationToken cancellationToken)
    {
        var result = await smsService.SendSmsAsync(phoneNumber, message, cancellationToken);
        return (result.Success, result.MessageId, result.ErrorMessage);
    }

    /// <summary>
    /// Sends an email to a single recipient.
    /// </summary>
    private async Task<bool> SendEmailToRecipientAsync(
        string toAddress,
        string? toName,
        string fromAddress,
        string? fromName,
        string subject,
        string bodyHtml,
        string? replyToAddress,
        CancellationToken cancellationToken)
    {
        return await emailSender.SendEmailAsync(
            toAddress,
            toName,
            fromAddress,
            fromName,
            subject,
            bodyHtml,
            bodyText: null,
            attachments: null,
            replyToAddress: replyToAddress,
            ct: cancellationToken);
    }
}
