using Koinon.Domain.Enums;

namespace Koinon.Domain.Entities;

/// <summary>
/// Records a person's attendance at a specific occurrence of a group meeting.
/// Used for check-in tracking, worship service attendance, small group participation, etc.
/// </summary>
public class Attendance : Entity
{
    /// <summary>
    /// Foreign key to the AttendanceOccurrence this attendance is for.
    /// </summary>
    public required int OccurrenceId { get; set; }

    /// <summary>
    /// Navigation property to the AttendanceOccurrence.
    /// </summary>
    public virtual AttendanceOccurrence? Occurrence { get; set; }

    /// <summary>
    /// Optional foreign key to PersonAlias (the person who attended).
    /// Null for anonymous attendance.
    /// </summary>
    public int? PersonAliasId { get; set; }

    /// <summary>
    /// Navigation property to the PersonAlias.
    /// </summary>
    public virtual PersonAlias? PersonAlias { get; set; }

    /// <summary>
    /// Optional foreign key to the Device (kiosk) used for check-in.
    /// </summary>
    public int? DeviceId { get; set; }

    /// <summary>
    /// Optional foreign key to the AttendanceCode (security code for child check-in).
    /// </summary>
    public int? AttendanceCodeId { get; set; }

    /// <summary>
    /// Navigation property to the AttendanceCode.
    /// </summary>
    public virtual AttendanceCode? AttendanceCode { get; set; }

    /// <summary>
    /// Optional foreign key to a DefinedValue qualifier (e.g., "Volunteer", "Visitor").
    /// </summary>
    public int? QualifierValueId { get; set; }

    /// <summary>
    /// The date and time when attendance started (check-in time).
    /// </summary>
    public required DateTime StartDateTime { get; set; }

    /// <summary>
    /// The date and time when attendance ended (check-out time).
    /// </summary>
    public DateTime? EndDateTime { get; set; }

    /// <summary>
    /// RSVP status for scheduled attendance.
    /// </summary>
    public RSVP RSVP { get; set; } = RSVP.Unknown;

    /// <summary>
    /// Indicates whether the person actually attended (vs. just RSVP'd).
    /// </summary>
    public bool? DidAttend { get; set; }

    /// <summary>
    /// Optional notes about this attendance.
    /// </summary>
    public string? Note { get; set; }

    /// <summary>
    /// Optional foreign key to Campus (denormalized for reporting).
    /// </summary>
    public int? CampusId { get; set; }

    /// <summary>
    /// Date and time when this attendance was processed/finalized.
    /// </summary>
    public DateTime? ProcessedDateTime { get; set; }

    /// <summary>
    /// Indicates whether this is the person's first time attending this group.
    /// </summary>
    public bool IsFirstTime { get; set; }

    /// <summary>
    /// Date and time when the person was marked as present.
    /// </summary>
    public DateTime? PresentDateTime { get; set; }

    /// <summary>
    /// PersonAlias ID of who marked the person as present.
    /// </summary>
    public int? PresentByPersonAliasId { get; set; }

    /// <summary>
    /// PersonAlias ID of who checked the person out.
    /// </summary>
    public int? CheckedOutByPersonAliasId { get; set; }

    /// <summary>
    /// Indicates whether the person requested to attend (for scheduling).
    /// </summary>
    public bool RequestedToAttend { get; set; }

    /// <summary>
    /// Indicates whether the person was scheduled to attend.
    /// </summary>
    public bool ScheduledToAttend { get; set; }

    /// <summary>
    /// Optional foreign key to DefinedValue indicating why attendance was declined.
    /// </summary>
    public int? DeclineReasonValueId { get; set; }

    /// <summary>
    /// PersonAlias ID of who scheduled this attendance.
    /// </summary>
    public int? ScheduledByPersonAliasId { get; set; }

    /// <summary>
    /// Indicates whether a schedule confirmation was sent.
    /// </summary>
    public bool ScheduleConfirmationSent { get; set; }
}
