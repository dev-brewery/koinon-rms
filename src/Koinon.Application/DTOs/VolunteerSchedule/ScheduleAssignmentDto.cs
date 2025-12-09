using Koinon.Domain.Enums;

namespace Koinon.Application.DTOs.VolunteerSchedule;

/// <summary>
/// DTO representing a volunteer's schedule assignment.
/// </summary>
public record ScheduleAssignmentDto
{
    public required string IdKey { get; init; }
    public required string MemberIdKey { get; init; }
    public required string MemberName { get; init; }
    public required string ScheduleIdKey { get; init; }
    public required string ScheduleName { get; init; }
    public required DateOnly AssignedDate { get; init; }
    public required VolunteerScheduleStatus Status { get; init; }
    public string? DeclineReason { get; init; }
    public DateTime? RespondedDateTime { get; init; }
    public string? Note { get; init; }
}
