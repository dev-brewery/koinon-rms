namespace Koinon.Application.DTOs.GroupMeeting;

/// <summary>
/// DTO representing a summary of RSVP responses for a group meeting.
/// </summary>
public class MeetingRsvpSummaryDto
{
    /// <summary>
    /// The date of the meeting.
    /// </summary>
    public required DateOnly MeetingDate { get; set; }

    /// <summary>
    /// Number of people who responded Attending.
    /// </summary>
    public int Attending { get; set; }

    /// <summary>
    /// Number of people who responded NotAttending.
    /// </summary>
    public int NotAttending { get; set; }

    /// <summary>
    /// Number of people who responded Maybe.
    /// </summary>
    public int Maybe { get; set; }

    /// <summary>
    /// Number of people who have not responded.
    /// </summary>
    public int NoResponse { get; set; }

    /// <summary>
    /// Total number of invitations sent (active group members).
    /// </summary>
    public int TotalInvited { get; set; }

    /// <summary>
    /// List of all RSVP responses.
    /// </summary>
    public List<RsvpDto> Rsvps { get; set; } = new();
}
