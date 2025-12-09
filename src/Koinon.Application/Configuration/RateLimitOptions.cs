namespace Koinon.Application.Configuration;

/// <summary>
/// Configuration options for pickup verification rate limiting.
/// Controls how many failed attempts are allowed before blocking requests.
/// </summary>
public class RateLimitOptions
{
    /// <summary>
    /// Configuration section name in appsettings.json
    /// </summary>
    public const string SectionName = "RateLimiting:PickupVerification";

    /// <summary>
    /// Maximum number of failed verification attempts allowed within the time window.
    /// Default: 5 attempts
    /// </summary>
    public int MaxAttempts { get; set; } = 5;

    /// <summary>
    /// Time window in minutes for tracking failed attempts.
    /// After this time expires, the counter resets.
    /// Default: 15 minutes
    /// </summary>
    public int WindowMinutes { get; set; } = 15;
}
