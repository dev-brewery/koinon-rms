namespace Koinon.Application.DTOs.VolunteerSchedule;

/// <summary>
/// DTO representing a volunteer's personal schedule view.
/// Assignments grouped by date for easier display.
/// </summary>
public record MyScheduleDto
{
    /// <summary>
    /// The date for this group of assignments.
    /// </summary>
    public required DateOnly Date { get; init; }

    /// <summary>
    /// List of assignments for this date.
    /// </summary>
    public required List<ScheduleAssignmentDto> Assignments { get; init; }
}
