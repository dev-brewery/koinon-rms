using Koinon.Domain.Enums;

namespace Koinon.Domain.Entities;

/// <summary>
/// Represents a saved report configuration that defines a reusable report template.
/// Report definitions specify the report type, available parameters, and output format.
/// </summary>
public class ReportDefinition : Entity
{
    /// <summary>
    /// Display name of the report.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Detailed description of what the report provides.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Type of report (e.g., Attendance, Giving, People).
    /// </summary>
    public required ReportType ReportType { get; set; }

    /// <summary>
    /// JSON schema defining the available parameters for this report.
    /// Defines parameter names, types, validation rules, and UI hints.
    /// </summary>
    public required string ParameterSchema { get; set; }

    /// <summary>
    /// Default parameter values as JSON.
    /// Applied when a new report run is created from this definition.
    /// </summary>
    public string? DefaultParameters { get; set; }

    /// <summary>
    /// Default output format for reports generated from this definition.
    /// </summary>
    public required ReportOutputFormat OutputFormat { get; set; }

    /// <summary>
    /// Whether this definition is active and can be used to generate reports.
    /// Inactive definitions are not shown in report selection lists.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Whether this is a built-in system report that cannot be deleted.
    /// System reports can be modified but not removed.
    /// </summary>
    public bool IsSystem { get; set; }
}
