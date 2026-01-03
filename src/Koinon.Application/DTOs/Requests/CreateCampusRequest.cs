namespace Koinon.Application.DTOs.Requests;

/// <summary>
/// Request to create a new campus.
/// </summary>
public record CreateCampusRequest
{
    public required string Name { get; init; }
    public string? ShortCode { get; init; }
    public string? Description { get; init; }
    public string? Url { get; init; }
    public string? PhoneNumber { get; init; }
    public string? TimeZoneId { get; init; }
    public string? ServiceTimes { get; init; }
    public int Order { get; init; }
}
