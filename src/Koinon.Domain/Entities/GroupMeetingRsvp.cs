using System.ComponentModel.DataAnnotations;
using Koinon.Domain.Enums;

namespace Koinon.Domain.Entities;

/// <summary>
/// Represents an RSVP response for a group meeting.
/// Tracks whether a group member is planning to attend a specific meeting.
/// </summary>
public class GroupMeetingRsvp : Entity
{
    /// <summary>
    /// Foreign key to the group this RSVP is for.
    /// </summary>
    public required int GroupId { get; set; }

    /// <summary>
    /// The date of the meeting this RSVP is for.
    /// </summary>
    public required DateOnly MeetingDate { get; set; }

    /// <summary>
    /// Foreign key to the person responding to the RSVP.
    /// </summary>
    public required int PersonId { get; set; }

    /// <summary>
    /// The person's RSVP status.
    /// </summary>
    public RsvpStatus Status { get; set; } = RsvpStatus.NoResponse;

    /// <summary>
    /// Optional note from the person (e.g., "Running late" or "Bringing a friend").
    /// </summary>
    [MaxLength(500)]
    public string? Note { get; set; }

    /// <summary>
    /// Date and time when the person responded (null if NoResponse).
    /// </summary>
    public DateTime? RespondedDateTime { get; set; }

    // Navigation Properties

    /// <summary>
    /// The group this RSVP is for.
    /// </summary>
    public virtual Group? Group { get; set; }

    /// <summary>
    /// The person who responded to the RSVP.
    /// </summary>
    public virtual Person? Person { get; set; }
}
