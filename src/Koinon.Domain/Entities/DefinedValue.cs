namespace Koinon.Domain.Entities;

/// <summary>
/// Represents an individual value within a DefinedType lookup table.
/// (e.g., "Mobile", "Home", "Work" within the "Phone Number Type" DefinedType).
/// </summary>
public class DefinedValue : Entity
{
    /// <summary>
    /// Foreign key to the DefinedType this value belongs to.
    /// </summary>
    public required int DefinedTypeId { get; set; }

    /// <summary>
    /// The display value (e.g., "Mobile", "Active", "Member").
    /// </summary>
    public required string Value { get; set; }

    /// <summary>
    /// Detailed description of this value's meaning or usage.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Display order within the parent DefinedType.
    /// Lower values appear first.
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// Indicates whether this value is currently active and should be displayed in UI.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Navigation property to the parent DefinedType.
    /// </summary>
    public virtual DefinedType? DefinedType { get; set; }
}
