using Koinon.Domain.Enums;

namespace Koinon.Application.DTOs.GroupMeeting;

/// <summary>
/// Request to update an RSVP response.
/// </summary>
public class UpdateRsvpRequest
{
    /// <summary>
    /// The person's RSVP status.
    /// </summary>
    public RsvpStatus Status { get; set; }

    /// <summary>
    /// Optional note from the person.
    /// </summary>
    public string? Note { get; set; }
}
