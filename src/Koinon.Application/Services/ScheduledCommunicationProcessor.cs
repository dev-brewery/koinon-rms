using Koinon.Application.Interfaces;
using Koinon.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Koinon.Application.Services;

/// <summary>
/// Service for processing scheduled communications.
/// Checks for scheduled communications that are due, validates recipient opt-out preferences,
/// and transitions them to pending status for the CommunicationSender to process.
/// </summary>
public class ScheduledCommunicationProcessor(
    IApplicationDbContext context,
    ICommunicationPreferenceService communicationPreferenceService,
    ILogger<ScheduledCommunicationProcessor> logger) : IScheduledCommunicationProcessor
{
    /// <summary>
    /// Processes scheduled communications that are due for sending.
    /// For each communication:
    /// 1. Loads the communication with recipients
    /// 2. Checks opt-out preferences for all recipients
    /// 3. Marks opted-out recipients as Failed
    /// 4. Transitions the communication to Pending status (if any recipients remain)
    /// Uses atomic updates to prevent race conditions.
    /// </summary>
    public async Task<int> ProcessScheduledCommunicationsAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;

        logger.LogDebug("Checking for scheduled communications due before {Now}", now);

        // Find scheduled communications that are due
        // Query for IDs first to prevent holding locks during processing
        var scheduledCommunicationIds = await context.Communications
            .Where(c => c.Status == CommunicationStatus.Scheduled &&
                       c.ScheduledDateTime != null &&
                       c.ScheduledDateTime <= now)
            .OrderBy(c => c.ScheduledDateTime)
            .Select(c => c.Id)
            .ToListAsync(ct);

        if (scheduledCommunicationIds.Count == 0)
        {
            logger.LogDebug("No scheduled communications due for processing");
            return 0;
        }

        logger.LogInformation(
            "Found {Count} scheduled communication(s) due for processing",
            scheduledCommunicationIds.Count);

        int processedCount = 0;

        // Process each scheduled communication individually
        foreach (var communicationId in scheduledCommunicationIds)
        {
            if (ct.IsCancellationRequested)
            {
                logger.LogInformation(
                    "Processing canceled after {ProcessedCount} communications",
                    processedCount);
                break;
            }

            try
            {
                bool processed = await ProcessSingleScheduledCommunicationAsync(communicationId, ct);
                if (processed)
                {
                    processedCount++;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Failed to process scheduled communication {CommunicationId}",
                    communicationId);

                // Continue with next communication instead of stopping the whole process
            }
        }

        logger.LogInformation(
            "Completed processing {ProcessedCount} scheduled communication(s)",
            processedCount);

        return processedCount;
    }

    /// <summary>
    /// Processes a single scheduled communication.
    /// Returns true if successfully processed, false if already processed by another thread.
    /// </summary>
    private async Task<bool> ProcessSingleScheduledCommunicationAsync(
        int communicationId,
        CancellationToken ct)
    {
        // Load the communication with all recipients
        // Use AsTracking to enable updates
        var communication = await context.Communications
            .Include(c => c.Recipients)
            .FirstOrDefaultAsync(c => c.Id == communicationId, ct);

        if (communication == null)
        {
            logger.LogWarning(
                "Communication {CommunicationId} not found - may have been deleted",
                communicationId);
            return false;
        }

        // Double-check status in case another process already handled it
        if (communication.Status != CommunicationStatus.Scheduled)
        {
            logger.LogDebug(
                "Communication {CommunicationId} is no longer Scheduled (status: {Status}) - already processed",
                communicationId,
                communication.Status);
            return false;
        }

        logger.LogInformation(
            "Processing scheduled communication {CommunicationId} ({Type}) with {RecipientCount} recipient(s), scheduled for {ScheduledDateTime}",
            communicationId,
            communication.CommunicationType,
            communication.Recipients.Count,
            communication.ScheduledDateTime);

        // Get all pending recipient PersonIds
        var pendingRecipients = communication.Recipients
            .Where(r => r.Status == CommunicationRecipientStatus.Pending)
            .ToList();

        if (pendingRecipients.Count == 0)
        {
            logger.LogWarning(
                "Communication {CommunicationId} has no pending recipients - all already processed",
                communicationId);

            // Still transition to pending so it can be marked as complete
            communication.Status = CommunicationStatus.Pending;
            communication.ModifiedDateTime = DateTime.UtcNow;

            await context.SaveChangesAsync(ct);
            return true;
        }

        // Check opt-out preferences for all pending recipients
        var personIds = pendingRecipients.Select(r => r.PersonId).Distinct().ToList();

        logger.LogDebug(
            "Checking opt-out preferences for {PersonCount} unique person(s)",
            personIds.Count);

        Dictionary<int, bool> optOutStatuses;
        try
        {
            optOutStatuses = await communicationPreferenceService.IsOptedOutBatchAsync(
                personIds,
                communication.CommunicationType,
                ct);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to check opt-out preferences for communication {CommunicationId}",
                communicationId);
            throw;
        }

        // Mark opted-out recipients as Failed
        int optedOutCount = 0;
        foreach (var recipient in pendingRecipients)
        {
            if (optOutStatuses.TryGetValue(recipient.PersonId, out bool isOptedOut) && isOptedOut)
            {
                recipient.Status = CommunicationRecipientStatus.Failed;
                recipient.ErrorMessage = "Recipient opted out";
                optedOutCount++;

                logger.LogDebug(
                    "Recipient {RecipientId} (PersonId: {PersonId}) opted out of {Type} communications",
                    recipient.Id,
                    recipient.PersonId,
                    communication.CommunicationType);
            }
        }

        if (optedOutCount > 0)
        {
            logger.LogInformation(
                "Filtered out {OptedOutCount} opted-out recipient(s) from communication {CommunicationId}",
                optedOutCount,
                communicationId);
        }

        // Check if any recipients remain after filtering
        var remainingRecipients = communication.Recipients
            .Count(r => r.Status == CommunicationRecipientStatus.Pending);

        if (remainingRecipients == 0)
        {
            logger.LogWarning(
                "Communication {CommunicationId} has no remaining recipients after opt-out filtering - marking as Failed",
                communicationId);

            communication.Status = CommunicationStatus.Failed;
            communication.FailedCount = communication.Recipients.Count;
        }
        else
        {
            logger.LogInformation(
                "Communication {CommunicationId} has {RemainingCount} recipient(s) after opt-out filtering - transitioning to Pending",
                communicationId,
                remainingRecipients);

            // Transition to Pending so CommunicationSender can process it
            communication.Status = CommunicationStatus.Pending;
        }

        communication.ModifiedDateTime = DateTime.UtcNow;

        try
        {
            await context.SaveChangesAsync(ct);

            logger.LogInformation(
                "Successfully processed scheduled communication {CommunicationId} - transitioned to {Status}",
                communicationId,
                communication.Status);

            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            // Communication was modified by another process (e.g., user canceled it)
            // This is expected and safe to ignore - the user's action takes precedence
            logger.LogInformation(
                "Concurrency conflict when processing communication {CommunicationId} - likely modified by user action. Skipping.",
                communicationId);

            return false;
        }
    }
}
