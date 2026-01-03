using Koinon.Domain.Enums;

namespace Koinon.Application.Interfaces.Reporting;

/// <summary>
/// Interface for pluggable report generation implementations.
/// Implementations handle specific output formats (PDF, Excel, CSV).
/// </summary>
public interface IReportGenerator
{
    /// <summary>
    /// Gets the output format this generator produces.
    /// </summary>
    ReportOutputFormat OutputFormat { get; }

    /// <summary>
    /// Generates a report based on the provided data and parameters.
    /// </summary>
    /// <param name="reportName">Name of the report being generated</param>
    /// <param name="reportType">Type of report (AttendanceSummary, GivingSummary, etc.)</param>
    /// <param name="data">Report data as a collection of objects</param>
    /// <param name="parameters">Optional JSON parameters for customizing output</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Tuple of (stream, fileName, mimeType) containing the generated report</returns>
    Task<(Stream Stream, string FileName, string MimeType)> GenerateAsync(
        string reportName,
        ReportType reportType,
        IEnumerable<object> data,
        string? parameters = null,
        CancellationToken ct = default);
}
