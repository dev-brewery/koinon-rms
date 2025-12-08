using Koinon.Domain.Enums;

namespace Koinon.Domain.Entities;

/// <summary>
/// Defines who is authorized to pick up a child during checkout.
/// Links a child to authorized pickup persons with authorization levels and relationship details.
/// </summary>
public class AuthorizedPickup : Entity
{
    /// <summary>
    /// Foreign key to the child Person record.
    /// </summary>
    public required int ChildPersonId { get; set; }

    /// <summary>
    /// Navigation property to the child Person.
    /// </summary>
    public virtual Person? ChildPerson { get; set; }

    /// <summary>
    /// Foreign key to the authorized Person record if they exist in the system.
    /// Null if the authorized person is not in the system (use Name/PhoneNumber instead).
    /// </summary>
    public int? AuthorizedPersonId { get; set; }

    /// <summary>
    /// Navigation property to the authorized Person.
    /// </summary>
    public virtual Person? AuthorizedPerson { get; set; }

    /// <summary>
    /// Name of the authorized person when they are not in the system.
    /// Used when AuthorizedPersonId is null.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Phone number of the authorized person when they are not in the system.
    /// Used when AuthorizedPersonId is null.
    /// </summary>
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// Relationship of the authorized person to the child.
    /// </summary>
    public PickupRelationship Relationship { get; set; } = PickupRelationship.Other;

    /// <summary>
    /// Level of authorization for this pickup person.
    /// </summary>
    public AuthorizationLevel AuthorizationLevel { get; set; } = AuthorizationLevel.Always;

    /// <summary>
    /// Optional URL to a photo of the authorized person for identification purposes.
    /// </summary>
    public string? PhotoUrl { get; set; }

    /// <summary>
    /// Sensitive notes about custody situations or special instructions.
    /// Should only be visible to authorized staff.
    /// </summary>
    public string? CustodyNotes { get; set; }

    /// <summary>
    /// Indicates whether this authorization is currently active.
    /// Set to false for soft deletion (e.g., when authorization is revoked).
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Collection of pickup log entries that used this authorization.
    /// </summary>
    public virtual ICollection<PickupLog> PickupLogs { get; set; } = new List<PickupLog>();
}
