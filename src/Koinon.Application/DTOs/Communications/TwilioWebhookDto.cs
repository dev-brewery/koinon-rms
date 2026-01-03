namespace Koinon.Application.DTOs.Communications;

/// <summary>
/// DTO for Twilio status callback webhook payloads.
/// Maps to Twilio's status callback POST parameters.
/// See: https://www.twilio.com/docs/sms/api/message-resource#message-status-values
/// </summary>
public record TwilioWebhookDto
{
    /// <summary>
    /// Unique identifier for the message (Twilio SID).
    /// Example: "SM1234567890abcdef1234567890abcdef"
    /// </summary>
    public string? MessageSid { get; init; }

    /// <summary>
    /// Current status of the message.
    /// Possible values: "queued", "sending", "sent", "delivered", "undelivered", "failed"
    /// </summary>
    public string? MessageStatus { get; init; }

    /// <summary>
    /// Destination phone number in E.164 format.
    /// Example: "+15551234567"
    /// </summary>
    public string? To { get; init; }

    /// <summary>
    /// Sender phone number in E.164 format (Twilio number).
    /// Example: "+15559876543"
    /// </summary>
    public string? From { get; init; }

    /// <summary>
    /// Twilio error code if MessageStatus is "failed" or "undelivered".
    /// See: https://www.twilio.com/docs/api/errors
    /// </summary>
    public int? ErrorCode { get; init; }

    /// <summary>
    /// Human-readable error message if delivery failed.
    /// </summary>
    public string? ErrorMessage { get; init; }
}
