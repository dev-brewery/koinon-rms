namespace Koinon.Domain.Entities;

/// <summary>
/// Represents a JWT refresh token for a person.
/// Refresh tokens allow users to obtain new access tokens without re-authenticating.
/// </summary>
public class RefreshToken : Entity
{
    /// <summary>
    /// Foreign key to Person who owns this refresh token.
    /// </summary>
    public int PersonId { get; set; }

    /// <summary>
    /// The refresh token value (cryptographically secure random string).
    /// </summary>
    public required string Token { get; set; }

    /// <summary>
    /// When the refresh token expires (typically 7 days from creation).
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// When the refresh token was revoked (null if still valid).
    /// </summary>
    public DateTime? RevokedAt { get; set; }

    /// <summary>
    /// The token that replaced this one (used for token rotation).
    /// </summary>
    public string? ReplacedByToken { get; set; }

    /// <summary>
    /// IP address where the token was created.
    /// </summary>
    public string? CreatedByIp { get; set; }

    /// <summary>
    /// IP address where the token was revoked.
    /// </summary>
    public string? RevokedByIp { get; set; }

    /// <summary>
    /// Computed property indicating if the token is currently active (not expired or revoked).
    /// </summary>
    public bool IsActive => RevokedAt == null && DateTime.UtcNow < ExpiresAt;

    // Navigation properties

    /// <summary>
    /// Navigation property to the person who owns this refresh token.
    /// </summary>
    public virtual Person? Person { get; set; }
}
