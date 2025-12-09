using Koinon.Domain.Enums;

namespace Koinon.Application.DTOs.GroupMeeting;

/// <summary>
/// DTO representing a user's RSVP for their dashboard.
/// </summary>
public class MyRsvpDto
{
    /// <summary>
    /// IdKey of the group.
    /// </summary>
    public required string GroupIdKey { get; set; }

    /// <summary>
    /// Name of the group.
    /// </summary>
    public required string GroupName { get; set; }

    /// <summary>
    /// Date of the meeting.
    /// </summary>
    public required DateOnly MeetingDate { get; set; }

    /// <summary>
    /// Current RSVP status.
    /// </summary>
    public RsvpStatus Status { get; set; }

    /// <summary>
    /// Optional note.
    /// </summary>
    public string? Note { get; set; }
}
