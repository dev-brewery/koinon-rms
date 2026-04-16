namespace Koinon.Application.DTOs;

/// <summary>
/// Full DefinedType DTO including all values.
/// </summary>
public record DefinedTypeDto
{
    public required string IdKey { get; init; }
    public required Guid Guid { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public string? Category { get; init; }
    public string? HelpText { get; init; }
    public bool IsSystem { get; init; }
    public int Order { get; init; }
    public string? FieldTypeAssemblyName { get; init; }
    public required IReadOnlyList<DefinedValueDto> DefinedValues { get; init; }
    public required DateTime CreatedDateTime { get; init; }
    public DateTime? ModifiedDateTime { get; init; }
}
