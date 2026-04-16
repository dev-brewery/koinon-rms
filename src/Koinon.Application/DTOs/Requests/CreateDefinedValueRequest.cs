namespace Koinon.Application.DTOs.Requests;

/// <summary>
/// Request to create a new DefinedValue within a DefinedType.
/// </summary>
public record CreateDefinedValueRequest
{
    public required string Value { get; init; }
    public string? Description { get; init; }
    public int Order { get; init; }
}
