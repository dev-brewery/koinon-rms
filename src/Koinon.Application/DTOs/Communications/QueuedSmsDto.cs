namespace Koinon.Application.DTOs.Communications;

/// <summary>
/// DTO for queued SMS/MMS messages processed by Hangfire background jobs.
/// Contains all information needed to send a message via ISmsService.
/// </summary>
/// <param name="ToPhoneNumber">The destination phone number in E.164 format (e.g., +15551234567)</param>
/// <param name="Message">The message body to send</param>
/// <param name="MediaUrls">Optional URLs of media files to attach for MMS (null for SMS)</param>
public record QueuedSmsDto(
    string ToPhoneNumber,
    string Message,
    IEnumerable<string>? MediaUrls = null);
