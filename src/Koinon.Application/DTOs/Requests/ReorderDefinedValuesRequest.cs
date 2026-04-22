namespace Koinon.Application.DTOs.Requests;

/// <summary>
/// Request to reorder DefinedValues within a DefinedType.
/// </summary>
public record ReorderDefinedValuesRequest
{
    public required IReadOnlyList<DefinedValueOrderItem> Items { get; init; }
}

/// <summary>
/// An individual item specifying the desired order for a DefinedValue.
/// </summary>
public record DefinedValueOrderItem
{
    public required string IdKey { get; init; }
    public int Order { get; init; }
}
