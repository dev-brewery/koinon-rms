using System.Collections.Concurrent;
using Koinon.Application.Interfaces;
using Koinon.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;
using Twilio;
using Twilio.Exceptions;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace Koinon.Infrastructure.Services;

/// <summary>
/// SMS service implementation using Twilio.
/// Provides rate limiting, error handling, and graceful degradation when not configured.
/// </summary>
public class TwilioSmsService : ISmsService
{
    private readonly TwilioOptions _options;
    private readonly ILogger<TwilioSmsService> _logger;
    private readonly ResiliencePipeline _retryPipeline;

    // Rate limiting: max 10 messages per minute per from number
    private static readonly ConcurrentDictionary<string, Queue<DateTime>> _rateLimitTracking = new();
    private const int MaxMessagesPerMinute = 10;

    public bool IsConfigured => _options.IsValid;

    public TwilioSmsService(
        IOptions<TwilioOptions> options,
        ILogger<TwilioSmsService> logger)
    {
        _options = options.Value;
        _logger = logger;
        _retryPipeline = CreateRetryPipeline();
    }

    /// <summary>
    /// Creates a Polly v8 resilience pipeline for transient Twilio API failures with exponential backoff.
    /// Retries on network failures and specific transient Twilio error codes:
    /// - 20003 (service unavailable)
    /// - 20429 (rate limited)
    /// - 52001-52006 (message sending temporarily unavailable)
    /// </summary>
    private ResiliencePipeline CreateRetryPipeline()
    {
        return new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                ShouldHandle = new PredicateBuilder()
                    .Handle<System.Net.Http.HttpRequestException>()
                    .Handle<ApiException>(ex => IsTransientTwilioError(ex)),
                MaxRetryAttempts = 3,
                BackoffType = DelayBackoffType.Exponential,
                Delay = TimeSpan.FromSeconds(1),
                UseJitter = true,
                OnRetry = args =>
                {
                    _logger.LogWarning(
                        args.Outcome.Exception,
                        "Twilio API attempt {RetryCount} failed. Retrying in {Delay}...",
                        args.AttemptNumber,
                        args.RetryDelay);
                    return ValueTask.CompletedTask;
                }
            })
            .Build();
    }

    /// <summary>
    /// Determines if a Twilio ApiException represents a transient error that should be retried.
    /// </summary>
    private static bool IsTransientTwilioError(ApiException ex)
    {
        // Transient error codes
        return ex.Code == 20003   // Service unavailable
            || ex.Code == 20429   // Rate limited
            || (ex.Code >= 52001 && ex.Code <= 52006); // Message sending temporarily unavailable
    }

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
            _logger.LogWarning(
                "SMS service not configured. Cannot send message to {PhoneNumber}",
                toPhoneNumber);
            return new SmsResult(
                Success: false,
                MessageId: null,
                ErrorMessage: "SMS service not configured",
                SegmentCount: 0);
        }

        // Validate inputs
        if (string.IsNullOrWhiteSpace(toPhoneNumber))
        {
            _logger.LogWarning("Cannot send SMS: phone number is empty");
            return new SmsResult(
                Success: false,
                MessageId: null,
                ErrorMessage: "Phone number is required",
                SegmentCount: 0);
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            _logger.LogWarning("Cannot send SMS: message body is empty");
            return new SmsResult(
                Success: false,
                MessageId: null,
                ErrorMessage: "Message body is required",
                SegmentCount: 0);
        }

        // Check rate limit
        if (!CheckRateLimit(_options.FromNumber!))
        {
            _logger.LogWarning(
                "Rate limit exceeded for Twilio number {FromNumber}. Cannot send message to {PhoneNumber}",
                _options.FromNumber,
                toPhoneNumber);
            return new SmsResult(
                Success: false,
                MessageId: null,
                ErrorMessage: "Rate limit exceeded (max 10 messages per minute)",
                SegmentCount: 0);
        }

        try
        {
            // Initialize Twilio client
            TwilioClient.Init(_options.AccountSid, _options.AuthToken);

            // Prepare status callback URL if configured
            var statusCallbackUri = !string.IsNullOrEmpty(_options.WebhookUrl)
                ? new Uri(_options.WebhookUrl)
                : null;

            // Send message with retry pipeline for transient failures
            var messageResource = await _retryPipeline.ExecuteAsync(async ct =>
            {
                // Note: Twilio SDK v7.13.8 does not support CancellationToken
                return await MessageResource.CreateAsync(
                    to: new PhoneNumber(toPhoneNumber),
                    from: new PhoneNumber(_options.FromNumber!),
                    body: message,
                    statusCallback: statusCallbackUri);
            }, cancellationToken);

            _logger.LogInformation(
                "SMS sent successfully to {PhoneNumber}. Message SID: {MessageSid}",
                toPhoneNumber,
                messageResource.Sid);

            // NumSegments is a string in Twilio SDK, parse it or default to 1
            var segmentCount = 1;
            if (int.TryParse(messageResource.NumSegments, out var parsedSegments))
            {
                segmentCount = parsedSegments;
            }

            return new SmsResult(
                Success: true,
                MessageId: messageResource.Sid,
                ErrorMessage: null,
                SegmentCount: segmentCount);
        }
        catch (ApiException ex)
        {
            _logger.LogError(
                ex,
                "Twilio API error sending SMS to {PhoneNumber}. Code: {ErrorCode}, Message: {ErrorMessage}",
                toPhoneNumber,
                ex.Code,
                ex.Message);

            return new SmsResult(
                Success: false,
                MessageId: null,
                ErrorMessage: $"Twilio error: {ex.Message}",
                SegmentCount: 0);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unexpected error sending SMS to {PhoneNumber}",
                toPhoneNumber);

            return new SmsResult(
                Success: false,
                MessageId: null,
                ErrorMessage: $"Failed to send SMS: {ex.Message}",
                SegmentCount: 0);
        }
    }

    /// <summary>
    /// Sends an MMS message with media attachments via Twilio.
    /// </summary>
    public async Task<SmsResult> SendMmsAsync(
        string toPhoneNumber,
        string message,
        IEnumerable<string> mediaUrls,
        CancellationToken cancellationToken = default)
    {
        // Validate configuration
        if (!IsConfigured)
        {
            _logger.LogWarning(
                "SMS service not configured. Cannot send MMS to {PhoneNumber}",
                toPhoneNumber);
            return new SmsResult(
                Success: false,
                MessageId: null,
                ErrorMessage: "SMS service not configured",
                SegmentCount: 0);
        }

        // Validate inputs
        if (string.IsNullOrWhiteSpace(toPhoneNumber))
        {
            _logger.LogWarning("Cannot send MMS: phone number is empty");
            return new SmsResult(
                Success: false,
                MessageId: null,
                ErrorMessage: "Phone number is required",
                SegmentCount: 0);
        }

        var mediaUrlsList = mediaUrls?.ToList() ?? new List<string>();
        if (mediaUrlsList.Count == 0)
        {
            _logger.LogWarning("Cannot send MMS: no media URLs provided");
            return new SmsResult(
                Success: false,
                MessageId: null,
                ErrorMessage: "At least one media URL is required for MMS",
                SegmentCount: 0);
        }

        // Check rate limit
        if (!CheckRateLimit(_options.FromNumber!))
        {
            _logger.LogWarning(
                "Rate limit exceeded for Twilio number {FromNumber}. Cannot send MMS to {PhoneNumber}",
                _options.FromNumber,
                toPhoneNumber);
            return new SmsResult(
                Success: false,
                MessageId: null,
                ErrorMessage: "Rate limit exceeded (max 10 messages per minute)",
                SegmentCount: 0);
        }

        try
        {
            // Initialize Twilio client
            TwilioClient.Init(_options.AccountSid, _options.AuthToken);

            // Validate and convert media URLs to Twilio Uri type
            var mediaUris = new List<Uri>();
            foreach (var url in mediaUrlsList)
            {
                if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
                {
                    return new SmsResult(
                        Success: false,
                        MessageId: null,
                        ErrorMessage: $"Invalid media URL: {url}",
                        SegmentCount: 0);
                }
                mediaUris.Add(uri);
            }

            // Prepare status callback URL if configured
            var statusCallbackUri = !string.IsNullOrEmpty(_options.WebhookUrl)
                ? new Uri(_options.WebhookUrl)
                : null;

            // Send message with media using retry pipeline for transient failures
            var messageResource = await _retryPipeline.ExecuteAsync(async ct =>
            {
                // Note: Twilio SDK v7.13.8 does not support CancellationToken
                return await MessageResource.CreateAsync(
                    to: new PhoneNumber(toPhoneNumber),
                    from: new PhoneNumber(_options.FromNumber!),
                    body: message,
                    mediaUrl: mediaUris,
                    statusCallback: statusCallbackUri);
            }, cancellationToken);

            _logger.LogInformation(
                "MMS sent successfully to {PhoneNumber} with {MediaCount} media attachments. Message SID: {MessageSid}",
                toPhoneNumber,
                mediaUrlsList.Count,
                messageResource.Sid);

            return new SmsResult(
                Success: true,
                MessageId: messageResource.Sid,
                ErrorMessage: null,
                SegmentCount: 1); // MMS is always 1 segment
        }
        catch (ApiException ex)
        {
            _logger.LogError(
                ex,
                "Twilio API error sending MMS to {PhoneNumber}. Code: {ErrorCode}, Message: {ErrorMessage}",
                toPhoneNumber,
                ex.Code,
                ex.Message);

            return new SmsResult(
                Success: false,
                MessageId: null,
                ErrorMessage: $"Twilio error: {ex.Message}",
                SegmentCount: 0);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unexpected error sending MMS to {PhoneNumber}",
                toPhoneNumber);

            return new SmsResult(
                Success: false,
                MessageId: null,
                ErrorMessage: $"Failed to send MMS: {ex.Message}",
                SegmentCount: 0);
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
            _logger.LogWarning("SMS service not configured. Cannot check delivery status");
            return SmsStatus.Unknown;
        }

        if (string.IsNullOrWhiteSpace(messageId))
        {
            _logger.LogWarning("Cannot get delivery status: message ID is empty");
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
            _logger.LogError(
                ex,
                "Twilio API error fetching message status for SID {MessageId}. Code: {ErrorCode}",
                messageId,
                ex.Code);
            return SmsStatus.Unknown;
        }
        catch (Exception ex)
        {
            _logger.LogError(
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
