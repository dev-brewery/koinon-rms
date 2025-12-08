namespace Koinon.Application.DTOs;

/// <summary>
/// DTO representing a first-time visitor who checked in.
/// </summary>
public record FirstTimeVisitorDto
{
    public required string PersonIdKey { get; init; }
    public required string PersonName { get; init; }
    public string? Email { get; init; }
    public string? PhoneNumber { get; init; }
    public required DateTime CheckInDateTime { get; init; }
    public required string GroupName { get; init; }
    public required string GroupTypeName { get; init; }
    public string? CampusName { get; init; }
    public required bool HasFollowUp { get; init; }
}
