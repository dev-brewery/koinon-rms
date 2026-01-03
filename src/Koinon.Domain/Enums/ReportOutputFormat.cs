namespace Koinon.Domain.Enums;

/// <summary>
/// Represents the output format for generated reports.
/// </summary>
public enum ReportOutputFormat
{
    /// <summary>
    /// Portable Document Format (PDF) for printable reports.
    /// </summary>
    Pdf = 1,

    /// <summary>
    /// Microsoft Excel spreadsheet format for data analysis.
    /// </summary>
    Excel = 2,

    /// <summary>
    /// Comma-Separated Values (CSV) format for data import/export.
    /// </summary>
    Csv = 3
}
