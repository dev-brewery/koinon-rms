using Koinon.Domain.Enums;

namespace Koinon.Domain.Entities;

/// <summary>
/// Represents a comprehensive audit log entry for tracking changes to entities in the system.
/// Captures who made changes, what was changed, when changes occurred, and contextual information.
/// </summary>
public class AuditLog : Entity
{
    /// <summary>
    /// Type of action performed on the entity (Create, Update, Delete, View, Export, etc.).
    /// </summary>
    public required AuditAction ActionType { get; set; }

    /// <summary>
    /// Name of the entity type being audited (e.g., "Person", "Group", "GroupMember").
    /// </summary>
    public required string EntityType { get; set; }

    /// <summary>
    /// IdKey of the entity being audited (URL-safe Base64-encoded identifier).
    /// </summary>
    public required string EntityIdKey { get; set; }

    /// <summary>
    /// Foreign key to Person who performed the action.
    /// Nullable to support system-generated actions.
    /// </summary>
    public int? PersonId { get; set; }

    /// <summary>
    /// Date and time when the action occurred (UTC).
    /// </summary>
    public required DateTime Timestamp { get; set; }

    /// <summary>
    /// JSON string representation of entity values before the change.
    /// Null for Create actions.
    /// </summary>
    public string? OldValues { get; set; }

    /// <summary>
    /// JSON string representation of entity values after the change.
    /// Null for Delete actions.
    /// </summary>
    public string? NewValues { get; set; }

    /// <summary>
    /// IP address from which the action was performed.
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// User-Agent header from the request (browser/client information).
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// JSON array of property names that were changed in an Update action.
    /// Example: ["FirstName", "Email", "ConnectionStatusValueId"]
    /// </summary>
    public string? ChangedProperties { get; set; }

    /// <summary>
    /// Additional contextual information about the action (free-form notes).
    /// </summary>
    public string? AdditionalInfo { get; set; }

    // Navigation properties

    /// <summary>
    /// Navigation property to the Person who performed the action.
    /// </summary>
    public virtual Person? Person { get; set; }
}
