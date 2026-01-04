namespace Koinon.Domain.Entities;

/// <summary>
/// Stores two-factor authentication configuration for a user.
/// Each person can have at most one 2FA configuration.
/// Stores encrypted TOTP secret and hashed recovery codes.
/// </summary>
public class TwoFactorConfig : Entity
{
    /// <summary>
    /// Foreign key to the Person who owns this 2FA configuration.
    /// This should be unique - one configuration per user.
    /// </summary>
    public required int PersonId { get; set; }

    /// <summary>
    /// Indicates whether two-factor authentication is currently enabled for this user.
    /// Defaults to false.
    /// </summary>
    public bool IsEnabled { get; set; } = false;

    /// <summary>
    /// Encrypted TOTP secret key used to generate time-based one-time passwords.
    /// Maximum length: 64 characters.
    /// This value should be encrypted at rest.
    /// </summary>
    public required string SecretKey { get; set; }

    /// <summary>
    /// JSON array of hashed recovery codes for account recovery when 2FA device is unavailable.
    /// Each code should be hashed before storage.
    /// Nullable when no recovery codes have been generated.
    /// </summary>
    public string? RecoveryCodes { get; set; }

    /// <summary>
    /// Timestamp when two-factor authentication was enabled for this user.
    /// Null if 2FA has never been enabled.
    /// </summary>
    public DateTime? EnabledAt { get; set; }

    /// <summary>
    /// Navigation property to the Person who owns this 2FA configuration.
    /// </summary>
    public virtual Person? Person { get; set; }
}
