namespace Koinon.Application.DTOs.Requests;

/// <summary>
/// Request to update an existing campus.
/// All fields are optional for partial update.
/// </summary>
public record UpdateCampusRequest
{
    public string? Name { get; init; }
    public string? ShortCode { get; init; }
    public string? Description { get; init; }
    public string? Url { get; init; }
    public string? PhoneNumber { get; init; }
    public string? TimeZoneId { get; init; }
    public string? ServiceTimes { get; init; }
    public int? Order { get; init; }
    public bool? IsActive { get; init; }
}
