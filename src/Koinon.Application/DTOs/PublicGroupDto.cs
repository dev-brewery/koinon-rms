namespace Koinon.Application.DTOs;

/// <summary>
/// DTO for publicly visible group information.
/// Excludes sensitive data like member details and addresses.
/// </summary>
public record PublicGroupDto
{
    public required string IdKey { get; init; }
    public required string Name { get; init; }
    public string? PublicDescription { get; init; }
    public string? GroupTypeName { get; init; }
    public string? CampusIdKey { get; init; }
    public string? CampusName { get; init; }
    public int MemberCount { get; init; }
    public int? Capacity { get; init; }
    public bool HasOpenings => Capacity == null || MemberCount < Capacity;

    // Schedule info
    public string? MeetingDay { get; init; }
    public TimeOnly? MeetingTime { get; init; }
    public string? MeetingScheduleSummary { get; init; }
}
