namespace Koinon.Domain.Entities;

/// <summary>
/// Junction entity linking Groups to Schedules.
/// Enables a group to be available during multiple schedules (e.g., Nursery available for both 9AM and 11AM services).
/// </summary>
public class GroupSchedule : Entity
{
    /// <summary>
    /// Foreign key to the Group.
    /// </summary>
    public required int GroupId { get; set; }

    /// <summary>
    /// Foreign key to the Schedule.
    /// </summary>
    public required int ScheduleId { get; set; }

    /// <summary>
    /// Optional foreign key to a Location (room assignment for this group at this schedule).
    /// For MVP, this can be null - location assignment is a future enhancement.
    /// </summary>
    public int? LocationId { get; set; }

    /// <summary>
    /// Display order for this schedule within the group.
    /// </summary>
    public int Order { get; set; }

    // Navigation Properties

    /// <summary>
    /// The Group this association belongs to.
    /// </summary>
    public virtual Group? Group { get; set; }

    /// <summary>
    /// The Schedule this association belongs to.
    /// </summary>
    public virtual Schedule? Schedule { get; set; }

    /// <summary>
    /// The Location this association uses (optional).
    /// </summary>
    public virtual Location? Location { get; set; }
}
