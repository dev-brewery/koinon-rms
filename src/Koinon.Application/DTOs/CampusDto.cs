namespace Koinon.Application.DTOs;

/// <summary>
/// Full campus DTO with all fields.
/// </summary>
public record CampusDto
{
    public required string IdKey { get; init; }
    public required Guid Guid { get; init; }
    public required string Name { get; init; }
    public string? ShortCode { get; init; }
    public string? Description { get; init; }
    public bool IsActive { get; init; }
    public string? Url { get; init; }
    public string? PhoneNumber { get; init; }
    public string? TimeZoneId { get; init; }
    public string? ServiceTimes { get; init; }
    public int Order { get; init; }
    public required DateTime CreatedDateTime { get; init; }
    public DateTime? ModifiedDateTime { get; init; }
}
