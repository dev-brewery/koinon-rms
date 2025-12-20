using Koinon.Domain.Enums;

namespace Koinon.Domain.Entities;

/// <summary>
/// Represents a reusable template for CSV imports with saved field mappings.
/// </summary>
public class ImportTemplate : Entity
{
    /// <summary>
    /// User-defined name for the template (e.g., "Planning Center People Export").
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Optional description of what this template is for.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Type of data this template imports.
    /// </summary>
    public ImportType ImportType { get; set; }

    /// <summary>
    /// JSON structure storing field mappings from CSV columns to entity properties.
    /// Example: {"FirstName": "First Name", "Email": "Email Address"}
    /// </summary>
    public required string FieldMappings { get; set; }

    /// <summary>
    /// Whether this template is active and available for use.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Whether this template is a system default (read-only).
    /// </summary>
    public bool IsSystem { get; set; }

    // Navigation properties

    /// <summary>
    /// Collection of import jobs that used this template.
    /// </summary>
    public virtual ICollection<ImportJob> ImportJobs { get; set; } = new List<ImportJob>();
}
