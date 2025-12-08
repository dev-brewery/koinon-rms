using Koinon.Domain.Enums;

namespace Koinon.Application.DTOs;

/// <summary>
/// Parameters for searching publicly visible groups.
/// </summary>
public record PublicGroupSearchParameters
{
    public string? SearchTerm { get; init; }
    public string? GroupTypeIdKey { get; init; }
    public string? CampusIdKey { get; init; }
    public DayOfWeek? DayOfWeek { get; init; }
    public TimeRange? TimeOfDay { get; init; }
    public bool? HasOpenings { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
