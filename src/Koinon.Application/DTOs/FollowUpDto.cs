using Koinon.Domain.Enums;

namespace Koinon.Application.DTOs;

/// <summary>
/// DTO representing a follow-up task for a first-time visitor or attendee.
/// </summary>
public record FollowUpDto
{
    public required string IdKey { get; init; }
    public required string PersonIdKey { get; init; }
    public required string PersonName { get; init; }
    public string? AttendanceIdKey { get; init; }
    public required FollowUpStatus Status { get; init; }
    public string? Notes { get; init; }
    public string? AssignedToIdKey { get; init; }
    public string? AssignedToName { get; init; }
    public DateTime? ContactedDateTime { get; init; }
    public DateTime? CompletedDateTime { get; init; }
    public required DateTime CreatedDateTime { get; init; }
}
