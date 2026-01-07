using Koinon.Domain.Enums;

namespace Koinon.Domain.Entities;

/// <summary>
/// Represents a label template used for generating physical labels during check-in.
/// Supports multiple label types (child name tags, parent claim tickets, allergy alerts, etc.)
/// using various template formats (ZPL, PDF, etc.).
/// </summary>
public class LabelTemplate : Entity
{
    /// <summary>
    /// Name of the label template (e.g., "Child Name Label (Standard)", "Allergy Alert 2x4").
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Type of label this template generates.
    /// Determines the label's purpose (child name tag, parent claim ticket, etc.).
    /// </summary>
    public required LabelType Type { get; set; }

    /// <summary>
    /// Template format identifier (e.g., "ZPL", "PDF", "ESC/POS").
    /// Maximum 50 characters.
    /// </summary>
    public required string Format { get; set; }

    /// <summary>
    /// The template content in the specified format.
    /// For ZPL templates, this contains the complete ZPL command sequence.
    /// Supports merge fields for dynamic content (e.g., {{ChildName}}, {{SecurityCode}}).
    /// </summary>
    public required string Template { get; set; }

    /// <summary>
    /// Width of the label in millimeters.
    /// Used for printer configuration and preview rendering.
    /// </summary>
    public required int WidthMm { get; set; }

    /// <summary>
    /// Height of the label in millimeters.
    /// Used for printer configuration and preview rendering.
    /// </summary>
    public required int HeightMm { get; set; }

    /// <summary>
    /// Indicates whether this template is currently active and available for use.
    /// Inactive templates are hidden from selection but preserved for historical records.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Indicates whether this is a system-provided template.
    /// System templates cannot be deleted, only deactivated.
    /// </summary>
    public bool IsSystem { get; set; } = false;
}
