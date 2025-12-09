namespace Koinon.Application.DTOs;

/// <summary>
/// DTO for current user's involvement summary (groups and attendance).
/// </summary>
public record MyInvolvementDto
{
    /// <summary>
    /// Groups the user belongs to (excluding family).
    /// </summary>
    public required IReadOnlyList<MyInvolvementGroupDto> Groups { get; init; }

    /// <summary>
    /// Recent attendance summary count (last 30 days).
    /// </summary>
    public int RecentAttendanceCount { get; init; }

    /// <summary>
    /// Total group memberships count.
    /// </summary>
    public int TotalGroupsCount { get; init; }
}

/// <summary>
/// DTO for a group in the user's involvement list.
/// </summary>
public record MyInvolvementGroupDto
{
    public required string IdKey { get; init; }
    public required string GroupName { get; init; }
    public string? Description { get; init; }
    public required string GroupTypeName { get; init; }

    /// <summary>
    /// User's role in this group.
    /// </summary>
    public required string Role { get; init; }

    /// <summary>
    /// Whether the user is a leader of this group.
    /// </summary>
    public bool IsLeader { get; init; }

    /// <summary>
    /// Last attendance date for this group (if available).
    /// </summary>
    public DateTime? LastAttendanceDate { get; init; }

    /// <summary>
    /// Date when the user joined this group.
    /// </summary>
    public DateTime? JoinedDate { get; init; }

    /// <summary>
    /// Campus associated with this group.
    /// </summary>
    public CampusSummaryDto? Campus { get; init; }
}
