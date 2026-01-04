namespace Koinon.Domain.Entities;

/// <summary>
/// Tracks active user sessions for security monitoring and activity tracking.
/// Records device information, location, and session activity.
/// </summary>
public class UserSession : Entity
{
    /// <summary>
    /// Foreign key to the Person who owns this session.
    /// </summary>
    public required int PersonId { get; set; }

    /// <summary>
    /// Foreign key to the RefreshToken associated with this session.
    /// Nullable for sessions that don't use refresh tokens.
    /// </summary>
    public int? RefreshTokenId { get; set; }

    /// <summary>
    /// Information about the device/browser used for this session.
    /// Maximum length: 256 characters.
    /// Example: "Chrome 120.0 on Windows 10"
    /// </summary>
    public string? DeviceInfo { get; set; }

    /// <summary>
    /// IP address from which the session was initiated.
    /// Maximum length: 45 characters to support IPv6 addresses.
    /// </summary>
    public required string IpAddress { get; set; }

    /// <summary>
    /// Geographic location derived from IP address (city, country).
    /// Maximum length: 128 characters.
    /// Example: "Seattle, WA, USA"
    /// </summary>
    public string? Location { get; set; }

    /// <summary>
    /// Timestamp of the last activity in this session.
    /// Updated on each request to track session freshness.
    /// </summary>
    public required DateTime LastActivityAt { get; set; }

    /// <summary>
    /// Indicates whether this session is currently active.
    /// Defaults to true; set to false when session expires or user logs out.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Navigation property to the Person who owns this session.
    /// </summary>
    public virtual Person? Person { get; set; }

    /// <summary>
    /// Navigation property to the RefreshToken associated with this session.
    /// </summary>
    public virtual RefreshToken? RefreshToken { get; set; }
}
