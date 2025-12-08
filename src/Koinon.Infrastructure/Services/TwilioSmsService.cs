using System.Collections.Concurrent;
using Koinon.Application.Interfaces;
using Koinon.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Twilio;
using Twilio.Exceptions;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace Koinon.Infrastructure.Services;

/// <summary>
/// SMS service implementation using Twilio.
/// Provides rate limiting, error handling, and graceful degradation when not configured.
/// </summary>
public class TwilioSmsService(
    IOptions<TwilioOptions> options,
    ILogger<TwilioSmsService> logger) : ISmsService
{
    private readonly TwilioOptions _options = options.Value;

    // Rate limiting: max 10 messages per minute per from number
    private static readonly ConcurrentDictionary<string, Queue<DateTime>> _rateLimitTracking = new();
    private const int MaxMessagesPerMinute = 10;

    public bool IsConfigured => _options.IsValid;

    /// <summary>
    /// Sends an SMS message via Twilio with rate limiting and error handling.
    /// </summary>
    public async Task<SmsResult> SendSmsAsync(
        string toPhoneNumber,
        string message,
        CancellationToken cancellationToken = default)
    {
        // Validate configuration
        if (!IsConfigured)
        {
            logger.LogWarning(
                "SMS service not configured. Cannot send message to {PhoneNumber}",
                toPhoneNumber);
            return new SmsResult(
                Success: false,
                MessageId: null,
                ErrorMessage: "SMS service not configured");
        }

        // Validate inputs
        if (string.IsNullOrWhiteSpace(toPhoneNumber))
        {
            logger.LogWarning("Cannot send SMS: phone number is empty");
            return new SmsResult(
                Success: false,
                MessageId: null,
                ErrorMessage: "Phone number is required");
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            logger.LogWarning("Cannot send SMS: message body is empty");
            return new SmsResult(
                Success: false,
                MessageId: null,
                ErrorMessage: "Message body is required");
        }

        // Check rate limit
        if (!CheckRateLimit(_options.FromNumber!))
        {
            logger.LogWarning(
                "Rate limit exceeded for Twilio number {FromNumber}. Cannot send message to {PhoneNumber}",
                _options.FromNumber,
                toPhoneNumber);
            return new SmsResult(
                Success: false,
                MessageId: null,
                ErrorMessage: "Rate limit exceeded (max 10 messages per minute)");
        }

        try
        {
            // Initialize Twilio client
            TwilioClient.Init(_options.AccountSid, _options.AuthToken);

            // Send message
            // Note: Twilio SDK v7.13.8 does not support CancellationToken
            var messageResource = await MessageResource.CreateAsync(
                to: new PhoneNumber(toPhoneNumber),
                from: new PhoneNumber(_options.FromNumber!),
                body: message);

            logger.LogInformation(
                "SMS sent successfully to {PhoneNumber}. Message SID: {MessageSid}",
                toPhoneNumber,
                messageResource.Sid);

            return new SmsResult(
                Success: true,
                MessageId: messageResource.Sid,
                ErrorMessage: null);
        }
        catch (ApiException ex)
        {
            logger.LogError(
                ex,
                "Twilio API error sending SMS to {PhoneNumber}. Code: {ErrorCode}, Message: {ErrorMessage}",
                toPhoneNumber,
                ex.Code,
                ex.Message);

            return new SmsResult(
                Success: false,
                MessageId: null,
                ErrorMessage: $"Twilio error: {ex.Message}");
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Unexpected error sending SMS to {PhoneNumber}",
                toPhoneNumber);

            return new SmsResult(
                Success: false,
                MessageId: null,
                ErrorMessage: $"Failed to send SMS: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets the delivery status of a previously sent message.
    /// </summary>
    public async Task<SmsStatus> GetDeliveryStatusAsync(
        string messageId,
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            logger.LogWarning("SMS service not configured. Cannot check delivery status");
            return SmsStatus.Unknown;
        }

        if (string.IsNullOrWhiteSpace(messageId))
        {
            logger.LogWarning("Cannot get delivery status: message ID is empty");
            return SmsStatus.Unknown;
        }

        try
        {
            TwilioClient.Init(_options.AccountSid, _options.AuthToken);

            // Note: Twilio SDK v7.13.8 does not support CancellationToken
            var message = await MessageResource.FetchAsync(pathSid: messageId);

            return MapTwilioStatus(message.Status);
        }
        catch (ApiException ex)
        {
            logger.LogError(
                ex,
                "Twilio API error fetching message status for SID {MessageId}. Code: {ErrorCode}",
                messageId,
                ex.Code);
            return SmsStatus.Unknown;
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Unexpected error fetching message status for SID {MessageId}",
                messageId);
            return SmsStatus.Unknown;
        }
    }

    /// <summary>
    /// Checks if the current send request is within rate limits.
    /// </summary>
    private static bool CheckRateLimit(string fromNumber)
    {
        var now = DateTime.UtcNow;
        var queue = _rateLimitTracking.GetOrAdd(fromNumber, _ => new Queue<DateTime>());

        lock (queue)
        {
            // Remove timestamps older than 1 minute
            while (queue.Count > 0 && (now - queue.Peek()).TotalMinutes >= 1)
            {
                queue.Dequeue();
            }

            // Check if we're at the limit
            if (queue.Count >= MaxMessagesPerMinute)
            {
                return false;
            }

            // Add current timestamp
            queue.Enqueue(now);
            return true;
        }
    }

    /// <summary>
    /// Maps Twilio message status to our SmsStatus enum.
    /// </summary>
    private static SmsStatus MapTwilioStatus(MessageResource.StatusEnum? twilioStatus)
    {
        if (twilioStatus == null)
        {
            return SmsStatus.Unknown;
        }

        // Use if-else instead of switch for enum comparison
        if (twilioStatus == MessageResource.StatusEnum.Queued)
        {
            return SmsStatus.Pending;
        }

        if (twilioStatus == MessageResource.StatusEnum.Sending)
        {
            return SmsStatus.Pending;
        }

        if (twilioStatus == MessageResource.StatusEnum.Sent)
        {
            return SmsStatus.Sent;
        }

        if (twilioStatus == MessageResource.StatusEnum.Delivered)
        {
            return SmsStatus.Delivered;
        }

        if (twilioStatus == MessageResource.StatusEnum.Failed)
        {
            return SmsStatus.Failed;
        }

        if (twilioStatus == MessageResource.StatusEnum.Undelivered)
        {
            return SmsStatus.Failed;
        }

        if (twilioStatus == MessageResource.StatusEnum.Canceled)
        {
            return SmsStatus.Failed;
        }

        return SmsStatus.Unknown;
    }
}
