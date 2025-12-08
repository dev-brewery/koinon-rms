using Koinon.Domain.Enums;

namespace Koinon.Domain.Entities;

/// <summary>
/// Represents a follow-up task for a person, typically a first-time visitor or new attendee.
/// Tracks the status and assignment of follow-up contact attempts.
/// </summary>
public class FollowUp : Entity
{
    /// <summary>
    /// Foreign key to the person who needs follow-up.
    /// </summary>
    public int PersonId { get; set; }

    /// <summary>
    /// Navigation property to the person who needs follow-up.
    /// </summary>
    public virtual Person? Person { get; set; }

    /// <summary>
    /// Optional foreign key to the attendance record that triggered this follow-up.
    /// </summary>
    public int? AttendanceId { get; set; }

    /// <summary>
    /// Navigation property to the attendance record.
    /// </summary>
    public virtual Attendance? Attendance { get; set; }

    /// <summary>
    /// Current status of the follow-up task.
    /// </summary>
    public FollowUpStatus Status { get; set; } = FollowUpStatus.Pending;

    /// <summary>
    /// Notes about the follow-up interaction, attempts, or outcomes.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Optional foreign key to the person assigned to complete this follow-up.
    /// </summary>
    public int? AssignedToPersonId { get; set; }

    /// <summary>
    /// Navigation property to the person assigned to this follow-up task.
    /// </summary>
    public virtual Person? AssignedToPerson { get; set; }

    /// <summary>
    /// Date and time when contact was made with the person.
    /// </summary>
    public DateTime? ContactedDateTime { get; set; }

    /// <summary>
    /// Date and time when the follow-up was marked as complete.
    /// </summary>
    public DateTime? CompletedDateTime { get; set; }
}
