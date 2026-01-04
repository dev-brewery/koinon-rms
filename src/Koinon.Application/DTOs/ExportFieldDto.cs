namespace Koinon.Application.DTOs;

/// <summary>
/// Represents a field available for export in a data export operation.
/// </summary>
public record ExportFieldDto
{
    /// <summary>
    /// The internal field name/key used to identify this field.
    /// </summary>
    public required string FieldName { get; init; }

    /// <summary>
    /// The display-friendly label for this field.
    /// </summary>
    public required string DisplayName { get; init; }

    /// <summary>
    /// The data type of the field (e.g., "string", "number", "date", "boolean").
    /// </summary>
    public required string DataType { get; init; }

    /// <summary>
    /// Optional description of what this field contains.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Whether this field is included by default in exports.
    /// </summary>
    public bool IsDefaultField { get; init; } = true;

    /// <summary>
    /// Whether this field is required and cannot be deselected.
    /// </summary>
    public bool IsRequired { get; init; } = false;
}
