using Koinon.Application.DTOs.Communications;
using Koinon.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Koinon.Infrastructure.Services;

/// <summary>
/// Service for queuing SMS/MMS messages for background processing via Hangfire.
/// Implements asynchronous message delivery with automatic retry on failure.
/// </summary>
public class SmsQueueService(
    IBackgroundJobService backgroundJobService,
    ISmsService smsService,
    ILogger<SmsQueueService> logger) : ISmsQueueService
{
    public string QueueSmsAsync(string toPhoneNumber, string message)
    {
        // Input validation
        if (string.IsNullOrWhiteSpace(toPhoneNumber))
        {
            throw new ArgumentException("Phone number cannot be null or empty.", nameof(toPhoneNumber));
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException("Message cannot be null or empty.", nameof(message));
        }

        var dto = new QueuedSmsDto(
            ToPhoneNumber: toPhoneNumber,
            Message: message,
            MediaUrls: null);

        // Note: Hangfire will inject the CancellationToken at execution time
        var jobId = backgroundJobService.Enqueue<ISmsQueueService>(
            service => service.ProcessQueuedMessageAsync(dto, CancellationToken.None));

        logger.LogInformation(
            "Queued SMS message for delivery. JobId={JobId} To={ToPhoneNumber}",
            jobId, toPhoneNumber);

        return jobId;
    }

    public string QueueMmsAsync(string toPhoneNumber, string message, IEnumerable<string> mediaUrls)
    {
        // Input validation
        if (string.IsNullOrWhiteSpace(toPhoneNumber))
        {
            throw new ArgumentException("Phone number cannot be null or empty.", nameof(toPhoneNumber));
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException("Message cannot be null or empty.", nameof(message));
        }

        var mediaUrlsList = mediaUrls?.ToList() ?? new List<string>();
        if (mediaUrlsList.Count == 0)
        {
            throw new ArgumentException("At least one media URL is required for MMS.", nameof(mediaUrls));
        }

        var dto = new QueuedSmsDto(
            ToPhoneNumber: toPhoneNumber,
            Message: message,
            MediaUrls: mediaUrlsList);

        // Note: Hangfire will inject the CancellationToken at execution time
        var jobId = backgroundJobService.Enqueue<ISmsQueueService>(
            service => service.ProcessQueuedMessageAsync(dto, CancellationToken.None));

        logger.LogInformation(
            "Queued MMS message for delivery. JobId={JobId} To={ToPhoneNumber} MediaCount={MediaCount}",
            jobId, toPhoneNumber, mediaUrlsList.Count);

        return jobId;
    }

    public async Task ProcessQueuedMessageAsync(QueuedSmsDto dto, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Processing queued message. To={ToPhoneNumber} HasMedia={HasMedia}",
            dto.ToPhoneNumber, dto.MediaUrls?.Any() == true);

        try
        {
            // Check if SMS service is configured
            if (!smsService.IsConfigured)
            {
                logger.LogError(
                    "SMS service is not configured. Cannot send message to {ToPhoneNumber}",
                    dto.ToPhoneNumber);
                throw new InvalidOperationException("SMS service is not configured.");
            }

            // Send SMS or MMS based on presence of media URLs
            var result = dto.MediaUrls?.Any() == true
                ? await smsService.SendMmsAsync(dto.ToPhoneNumber, dto.Message, dto.MediaUrls, cancellationToken)
                : await smsService.SendSmsAsync(dto.ToPhoneNumber, dto.Message, cancellationToken);

            if (result.Success)
            {
                logger.LogInformation(
                    "Successfully sent message. To={ToPhoneNumber} MessageId={MessageId} Segments={SegmentCount}",
                    dto.ToPhoneNumber, result.MessageId, result.SegmentCount);
            }
            else
            {
                logger.LogError(
                    "Failed to send message. To={ToPhoneNumber} Error={ErrorMessage}",
                    dto.ToPhoneNumber, result.ErrorMessage);

                // Throw exception to trigger Hangfire automatic retry
                throw new InvalidOperationException(
                    $"SMS delivery failed: {result.ErrorMessage}");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Exception occurred while processing queued message. To={ToPhoneNumber}",
                dto.ToPhoneNumber);

            // Re-throw to let Hangfire handle retry logic
            throw;
        }
    }
}
