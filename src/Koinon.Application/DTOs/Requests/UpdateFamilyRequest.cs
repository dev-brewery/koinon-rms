namespace Koinon.Application.DTOs.Requests;

/// <summary>
/// Request to update a family's basic details.
/// </summary>
public record UpdateFamilyRequest
{
    public string? Name { get; init; }
    public string? CampusId { get; init; }
}
