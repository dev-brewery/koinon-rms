namespace Koinon.Application.DTOs.Requests;

/// <summary>
/// Request to update an existing DefinedValue.
/// </summary>
public record UpdateDefinedValueRequest
{
    public required string Value { get; init; }
    public string? Description { get; init; }
    public int Order { get; init; }
    public bool IsActive { get; init; }
}
