using System.ComponentModel.DataAnnotations;
using Koinon.Domain.Enums;

namespace Koinon.Domain.Entities;

/// <summary>
/// Represents a volunteer's assignment to serve on a specific schedule date.
/// Tracks confirmation status and any decline reasons for scheduling management.
/// </summary>
public class VolunteerScheduleAssignment : Entity
{
    /// <summary>
    /// Foreign key to the GroupMember who is assigned to serve.
    /// </summary>
    public required int GroupMemberId { get; set; }

    /// <summary>
    /// Foreign key to the Schedule this assignment is for.
    /// </summary>
    public required int ScheduleId { get; set; }

    /// <summary>
    /// The specific date this volunteer is assigned to serve.
    /// </summary>
    public required DateOnly AssignedDate { get; set; }

    /// <summary>
    /// Current status of this assignment (Scheduled, Confirmed, Declined, NoResponse).
    /// </summary>
    public VolunteerScheduleStatus Status { get; set; } = VolunteerScheduleStatus.Scheduled;

    /// <summary>
    /// Optional reason provided if the volunteer declined this assignment.
    /// </summary>
    [MaxLength(500)]
    public string? DeclineReason { get; set; }

    /// <summary>
    /// Date and time when the volunteer responded to this assignment.
    /// </summary>
    public DateTime? RespondedDateTime { get; set; }

    /// <summary>
    /// Optional note about this assignment (e.g., special instructions, reminders).
    /// </summary>
    [MaxLength(500)]
    public string? Note { get; set; }

    // Navigation Properties

    /// <summary>
    /// The group member who is assigned to serve.
    /// </summary>
    public virtual GroupMember? GroupMember { get; set; }

    /// <summary>
    /// The schedule this assignment is for.
    /// </summary>
    public virtual Schedule? Schedule { get; set; }
}
