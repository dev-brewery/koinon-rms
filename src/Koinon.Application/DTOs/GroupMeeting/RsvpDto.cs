using Koinon.Domain.Enums;

namespace Koinon.Application.DTOs.GroupMeeting;

/// <summary>
/// DTO representing a single RSVP response for a group meeting.
/// </summary>
public class RsvpDto
{
    /// <summary>
    /// IdKey of the RSVP record.
    /// </summary>
    public required string IdKey { get; set; }

    /// <summary>
    /// IdKey of the person who responded.
    /// </summary>
    public required string PersonIdKey { get; set; }

    /// <summary>
    /// Full name of the person.
    /// </summary>
    public required string PersonName { get; set; }

    /// <summary>
    /// RSVP status.
    /// </summary>
    public RsvpStatus Status { get; set; }

    /// <summary>
    /// Optional note from the person.
    /// </summary>
    public string? Note { get; set; }

    /// <summary>
    /// When the person responded (null if no response yet).
    /// </summary>
    public DateTime? RespondedDateTime { get; set; }
}
