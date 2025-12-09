namespace Koinon.Application.DTOs.Requests;

/// <summary>
/// Request to record attendance for a group meeting.
/// </summary>
public record RecordAttendanceRequest
{
    /// <summary>
    /// Date of the meeting occurrence.
    /// </summary>
    public required DateOnly OccurrenceDate { get; init; }

    /// <summary>
    /// List of person IdKeys who attended.
    /// </summary>
    public required IReadOnlyList<string> AttendedPersonIds { get; init; }

    /// <summary>
    /// Optional notes about the meeting.
    /// </summary>
    public string? Notes { get; init; }
}
