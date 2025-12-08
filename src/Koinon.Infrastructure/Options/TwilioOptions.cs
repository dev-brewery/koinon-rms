namespace Koinon.Infrastructure.Options;

/// <summary>
/// Configuration options for Twilio SMS service.
/// Loaded from the "Twilio" configuration section.
/// </summary>
public class TwilioOptions
{
    /// <summary>
    /// Configuration section name in appsettings.json.
    /// </summary>
    public const string SectionName = "Twilio";

    /// <summary>
    /// Twilio Account SID - identifies your account.
    /// </summary>
    public string? AccountSid { get; set; }

    /// <summary>
    /// Twilio Auth Token - authenticates API requests.
    /// </summary>
    public string? AuthToken { get; set; }

    /// <summary>
    /// Twilio phone number to send from (E.164 format, e.g., +15551234567).
    /// </summary>
    public string? FromNumber { get; set; }

    /// <summary>
    /// Validates that all required options are configured.
    /// </summary>
    public bool IsValid =>
        !string.IsNullOrWhiteSpace(AccountSid) &&
        !string.IsNullOrWhiteSpace(AuthToken) &&
        !string.IsNullOrWhiteSpace(FromNumber);
}
