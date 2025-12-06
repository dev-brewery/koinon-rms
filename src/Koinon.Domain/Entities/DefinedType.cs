namespace Koinon.Domain.Entities;

/// <summary>
/// Defines a category of lookup values (e.g., "Phone Number Type", "Record Status", "Connection Status").
/// DefinedTypes contain a collection of DefinedValues that represent the individual options.
/// </summary>
public class DefinedType : Entity
{
    /// <summary>
    /// The name of this defined type (e.g., "Phone Number Type").
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Detailed description of this defined type's purpose.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Category for grouping related defined types (e.g., "Person", "Communication").
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// Help text displayed to users when selecting values from this type.
    /// </summary>
    public string? HelpText { get; set; }

    /// <summary>
    /// Indicates whether this is a system-defined type that cannot be deleted.
    /// </summary>
    public bool IsSystem { get; set; }

    /// <summary>
    /// Display order when listing defined types.
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// Assembly name for custom field type implementations (if applicable).
    /// </summary>
    public string? FieldTypeAssemblyName { get; set; }

    /// <summary>
    /// Navigation property to the collection of values for this type.
    /// </summary>
    public virtual ICollection<DefinedValue> DefinedValues { get; set; } = new List<DefinedValue>();
}
