namespace Koinon.Application.DTOs;

/// <summary>
/// Lightweight DefinedType DTO for list views.
/// </summary>
public record DefinedTypeSummaryDto
{
    public required string IdKey { get; init; }
    public required string Name { get; init; }
    public string? Category { get; init; }
    public bool IsSystem { get; init; }
    public int Order { get; init; }
    public int ValueCount { get; init; }
}
