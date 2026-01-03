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
    /// URL for Twilio to post status callbacks (delivery receipts, etc.).
    /// </summary>
    public string? WebhookUrl { get; set; }

    /// <summary>
    /// Optional monthly cost limit in cents. NOT ENFORCED - for informational tracking only.
    /// Future: May be enforced when cost tracking persistence is implemented.
    /// </summary>
    public int? MonthlyCostLimitCents { get; set; }

    /// <summary>
    /// Optional monthly SMS count limit. NOT ENFORCED - for informational tracking only.
    /// Future: May be enforced when usage tracking persistence is implemented.
    /// </summary>
    public int? MonthlySmsLimit { get; set; }

    /// <summary>
    /// Approximate cost per SMS segment in cents. For cost estimation only, not billing.
    /// Default: 1 (~$0.0075 per segment for US carriers).
    /// </summary>
    public int CostPerSmsCents { get; set; } = 1;

    /// <summary>
    /// Approximate cost per MMS in cents. For cost estimation only, not billing.
    /// Default: 2 (~$0.02 per MMS for US carriers).
    /// </summary>
    public int CostPerMmsCents { get; set; } = 2;

    /// <summary>
    /// Validates that all required options are configured.
    /// </summary>
    public bool IsValid =>
        !string.IsNullOrWhiteSpace(AccountSid) &&
        !string.IsNullOrWhiteSpace(AuthToken) &&
        !string.IsNullOrWhiteSpace(FromNumber);
}
