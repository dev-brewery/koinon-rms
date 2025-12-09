namespace Koinon.Application.DTOs;

/// <summary>
/// DTO for groups where the current user is a leader.
/// Includes summary information and leadership-specific metrics.
/// </summary>
public record MyGroupDto
{
    public required string IdKey { get; init; }
    public required Guid Guid { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required string GroupTypeName { get; init; }
    public required bool IsActive { get; init; }
    public required int MemberCount { get; init; }
    public int? GroupCapacity { get; init; }
    public DateTime? LastMeetingDate { get; init; }
    public CampusSummaryDto? Campus { get; init; }
    public required DateTime CreatedDateTime { get; init; }
    public DateTime? ModifiedDateTime { get; init; }
}
