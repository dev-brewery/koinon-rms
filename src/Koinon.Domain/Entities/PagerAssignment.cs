namespace Koinon.Domain.Entities;

/// <summary>
/// Records the assignment of a pager number to an attendance record during check-in.
/// Pager numbers are unique per campus per day and are used for parent notifications.
/// </summary>
public class PagerAssignment : Entity
{
    /// <summary>
    /// Foreign key to the attendance record this pager is assigned to.
    /// </summary>
    public required int AttendanceId { get; set; }

    /// <summary>
    /// Navigation property to the attendance record.
    /// </summary>
    public virtual Attendance? Attendance { get; set; }

    /// <summary>
    /// The numeric pager number assigned (displayed to parents as "P-XXX").
    /// Must be unique per campus per day.
    /// </summary>
    public required int PagerNumber { get; set; }

    /// <summary>
    /// Optional foreign key to the campus where this pager was assigned.
    /// Used to ensure pager number uniqueness per campus.
    /// </summary>
    public int? CampusId { get; set; }

    /// <summary>
    /// Navigation property to the campus.
    /// </summary>
    public virtual Campus? Campus { get; set; }

    /// <summary>
    /// Optional foreign key to the location (room) where the child was checked in.
    /// Helps staff quickly locate the child when paging parents.
    /// </summary>
    public int? LocationId { get; set; }

    /// <summary>
    /// Navigation property to the location.
    /// </summary>
    public virtual Location? Location { get; set; }

    /// <summary>
    /// Collection of messages sent for this pager assignment.
    /// </summary>
    public virtual ICollection<PagerMessage> Messages { get; set; } = new List<PagerMessage>();
}
