namespace Koinon.Domain.Entities;

/// <summary>
/// Records an audit trail of each child checkout event.
/// Tracks who picked up the child, whether they were authorized, and any supervisor overrides.
/// </summary>
public class PickupLog : Entity
{
    /// <summary>
    /// Foreign key to the Attendance record being checked out.
    /// </summary>
    public required int AttendanceId { get; set; }

    /// <summary>
    /// Navigation property to the Attendance record.
    /// </summary>
    public virtual Attendance? Attendance { get; set; }

    /// <summary>
    /// Foreign key to the child Person record being picked up.
    /// </summary>
    public required int ChildPersonId { get; set; }

    /// <summary>
    /// Navigation property to the child Person.
    /// </summary>
    public virtual Person? ChildPerson { get; set; }

    /// <summary>
    /// Foreign key to the Person record who picked up the child, if they exist in the system.
    /// Null if the pickup person is not in the system (use PickupPersonName instead).
    /// </summary>
    public int? PickupPersonId { get; set; }

    /// <summary>
    /// Navigation property to the Person who picked up the child.
    /// </summary>
    public virtual Person? PickupPerson { get; set; }

    /// <summary>
    /// Name of the person who picked up the child when they are not in the system.
    /// Used when PickupPersonId is null.
    /// </summary>
    public string? PickupPersonName { get; set; }

    /// <summary>
    /// Indicates whether the pickup person was on the authorized pickup list.
    /// </summary>
    public bool WasAuthorized { get; set; }

    /// <summary>
    /// Foreign key to the AuthorizedPickup record if the pickup person was authorized.
    /// Null if the pickup person was not on the authorized list (requires supervisor override).
    /// </summary>
    public int? AuthorizedPickupId { get; set; }

    /// <summary>
    /// Navigation property to the AuthorizedPickup record.
    /// </summary>
    public virtual AuthorizedPickup? AuthorizedPickup { get; set; }

    /// <summary>
    /// Indicates whether a supervisor approved the pickup despite not being authorized.
    /// Always false if WasAuthorized is true.
    /// </summary>
    public bool SupervisorOverride { get; set; }

    /// <summary>
    /// Foreign key to the Person record of the supervisor who approved the override.
    /// Required when SupervisorOverride is true.
    /// </summary>
    public int? SupervisorPersonId { get; set; }

    /// <summary>
    /// Navigation property to the supervisor Person who approved the override.
    /// </summary>
    public virtual Person? SupervisorPerson { get; set; }

    /// <summary>
    /// Date and time when the checkout occurred.
    /// </summary>
    public required DateTime CheckoutDateTime { get; set; }

    /// <summary>
    /// Optional notes about the pickup (e.g., reason for override, special circumstances).
    /// </summary>
    public string? Notes { get; set; }
}
