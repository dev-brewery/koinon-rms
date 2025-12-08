namespace Koinon.Application.DTOs;

/// <summary>
/// DTO for group-schedule association.
/// </summary>
public record GroupScheduleDto
{
    public required string IdKey { get; init; }
    public required Guid Guid { get; init; }
    public required ScheduleSummaryDto Schedule { get; init; }
    public int Order { get; init; }
}
