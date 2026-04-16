namespace Koinon.Application.DTOs;

/// <summary>
/// Full DefinedValue DTO for admin management endpoints.
/// Includes parent type reference and audit timestamps.
/// </summary>
public record DefinedValueManagementDto
{
    public required string IdKey { get; init; }
    public required string DefinedTypeIdKey { get; init; }
    public required string Value { get; init; }
    public string? Description { get; init; }
    public int Order { get; init; }
    public bool IsActive { get; init; }
    public required DateTime CreatedDateTime { get; init; }
    public DateTime? ModifiedDateTime { get; init; }
}
