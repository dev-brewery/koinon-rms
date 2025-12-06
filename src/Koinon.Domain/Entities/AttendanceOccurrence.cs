namespace Koinon.Domain.Entities;

/// <summary>
/// Represents a specific occurrence of a group meeting at a location on a schedule.
/// Multiple people can have Attendance records linked to a single AttendanceOccurrence.
/// </summary>
public class AttendanceOccurrence : Entity
{
    /// <summary>
    /// Optional foreign key to the Group this occurrence is for.
    /// </summary>
    public int? GroupId { get; set; }

    /// <summary>
    /// Navigation property to the Group.
    /// </summary>
    public virtual Group? Group { get; set; }

    /// <summary>
    /// Optional foreign key to the Location where this occurrence took place.
    /// </summary>
    public int? LocationId { get; set; }

    /// <summary>
    /// Navigation property to the Location.
    /// </summary>
    public virtual Location? Location { get; set; }

    /// <summary>
    /// Optional foreign key to the Schedule this occurrence is based on.
    /// </summary>
    public int? ScheduleId { get; set; }

    /// <summary>
    /// Navigation property to the Schedule.
    /// </summary>
    public virtual Schedule? Schedule { get; set; }

    /// <summary>
    /// The date this occurrence took place.
    /// </summary>
    public required DateOnly OccurrenceDate { get; set; }

    /// <summary>
    /// Indicates whether this occurrence was cancelled (did not occur).
    /// </summary>
    public bool? DidNotOccur { get; set; }

    /// <summary>
    /// The Sunday date of the week containing this occurrence (used for weekly reporting).
    /// </summary>
    public required DateOnly SundayDate { get; set; }

    /// <summary>
    /// Optional notes about this occurrence.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Count of anonymous attendees (headcount without individual names).
    /// </summary>
    public int? AnonymousAttendanceCount { get; set; }

    /// <summary>
    /// Optional foreign key to DefinedValue indicating the type of attendance (Worship Service, Small Group, etc.).
    /// </summary>
    public int? AttendanceTypeValueId { get; set; }

    /// <summary>
    /// Message shown when someone declines to attend this occurrence.
    /// </summary>
    public string? DeclineConfirmationMessage { get; set; }

    /// <summary>
    /// Indicates whether to show decline reasons for this occurrence.
    /// </summary>
    public bool ShowDeclineReasons { get; set; }

    /// <summary>
    /// Message shown when someone confirms attendance for this occurrence.
    /// </summary>
    public string? AcceptConfirmationMessage { get; set; }

    // Navigation Properties

    /// <summary>
    /// Collection of individual attendance records for this occurrence.
    /// </summary>
    public virtual ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();
}
