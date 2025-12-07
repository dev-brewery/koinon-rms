namespace Koinon.Domain.Entities;

/// <summary>
/// Audit log for supervisor mode operations.
/// Tracks all supervisor authentication attempts and actions for security compliance.
/// </summary>
public class SupervisorAuditLog : Entity
{
    /// <summary>
    /// Foreign key to the Person who attempted the action.
    /// Null if authentication failed before person identification.
    /// </summary>
    public int? PersonId { get; set; }

    /// <summary>
    /// Foreign key to the SupervisorSession if authentication succeeded.
    /// Null for failed login attempts.
    /// </summary>
    public int? SupervisorSessionId { get; set; }

    /// <summary>
    /// Type of action performed.
    /// Examples: "Login", "LoginFailed", "Logout", "Reprint", "SessionExpired"
    /// </summary>
    public required string ActionType { get; set; }

    /// <summary>
    /// IP address of the kiosk that initiated the action.
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// Type of entity affected by the action (e.g., "Attendance").
    /// Null for authentication actions.
    /// </summary>
    public string? EntityType { get; set; }

    /// <summary>
    /// IdKey of the entity affected by the action.
    /// Null for authentication actions.
    /// </summary>
    public string? EntityIdKey { get; set; }

    /// <summary>
    /// Success status of the action.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Additional details about the action or failure reason.
    /// </summary>
    public string? Details { get; set; }

    // Navigation properties

    /// <summary>
    /// The person who attempted the action.
    /// </summary>
    public virtual Person? Person { get; set; }

    /// <summary>
    /// The supervisor session associated with this action.
    /// </summary>
    public virtual SupervisorSession? SupervisorSession { get; set; }
}
