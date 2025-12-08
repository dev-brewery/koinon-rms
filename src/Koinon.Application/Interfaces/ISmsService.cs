namespace Koinon.Application.Interfaces;

/// <summary>
/// Service for sending SMS messages via external provider (Twilio).
/// Used for parent paging and other notification scenarios.
/// </summary>
public interface ISmsService
{
    /// <summary>
    /// Sends an SMS message and returns the provider message ID (Twilio SID).
    /// </summary>
    /// <param name="toPhoneNumber">The destination phone number in E.164 format (e.g., +15551234567)</param>
    /// <param name="message">The message body to send</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing success status, message ID, and error details if failed</returns>
    Task<SmsResult> SendSmsAsync(string toPhoneNumber, string message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the delivery status of a previously sent message.
    /// </summary>
    /// <param name="messageId">The provider message ID returned from SendSmsAsync</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Current delivery status of the message</returns>
    Task<SmsStatus> GetDeliveryStatusAsync(string messageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if SMS service is configured and operational.
    /// Returns false if credentials are missing or invalid.
    /// </summary>
    bool IsConfigured { get; }
}

/// <summary>
/// Result of an SMS send operation.
/// </summary>
/// <param name="Success">True if message was sent successfully</param>
/// <param name="MessageId">Provider message ID (Twilio SID) for tracking, null if failed</param>
/// <param name="ErrorMessage">Error description if failed, null if successful</param>
public record SmsResult(bool Success, string? MessageId, string? ErrorMessage);

/// <summary>
/// SMS delivery status from the provider.
/// </summary>
public enum SmsStatus
{
    /// <summary>Status could not be determined</summary>
    Unknown,

    /// <summary>Message queued for delivery</summary>
    Pending,

    /// <summary>Message sent to carrier</summary>
    Sent,

    /// <summary>Message delivered to recipient</summary>
    Delivered,

    /// <summary>Message delivery failed</summary>
    Failed
}
