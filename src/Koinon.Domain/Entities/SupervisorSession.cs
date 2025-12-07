namespace Koinon.Domain.Entities;

/// <summary>
/// Represents a supervisor mode session for kiosk operations.
/// Sessions are time-limited and tracked for audit purposes.
/// </summary>
public class SupervisorSession : Entity
{
    /// <summary>
    /// Foreign key to the Person who is the supervisor.
    /// </summary>
    public int PersonId { get; set; }

    /// <summary>
    /// Unique session token (cryptographically random).
    /// </summary>
    public required string Token { get; set; }

    /// <summary>
    /// When this session expires (UTC).
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// When this session was ended (UTC), if manually logged out.
    /// Null if session expired naturally.
    /// </summary>
    public DateTime? EndedAt { get; set; }

    /// <summary>
    /// IP address of the kiosk that created this session.
    /// </summary>
    public string? CreatedByIp { get; set; }

    /// <summary>
    /// Whether this session is still active.
    /// </summary>
    public bool IsActive => EndedAt == null && ExpiresAt > DateTime.UtcNow;

    // Navigation properties

    /// <summary>
    /// The supervisor person.
    /// </summary>
    public virtual Person? Person { get; set; }
}
