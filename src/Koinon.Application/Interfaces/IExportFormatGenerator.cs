using Koinon.Domain.Enums;

namespace Koinon.Application.Interfaces;

/// <summary>
/// Defines the contract for export format generators that convert data to specific file formats.
/// </summary>
public interface IExportFormatGenerator
{
    /// <summary>
    /// The output format this generator produces.
    /// </summary>
    ReportOutputFormat OutputFormat { get; }

    /// <summary>
    /// Generates a formatted export file from the provided data.
    /// </summary>
    /// <param name="data">The data rows to export.</param>
    /// <param name="fields">The ordered list of field names to include in the export.</param>
    /// <param name="exportName">The name of the export (used in file metadata/naming).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Stream containing the generated export file.</returns>
    Task<Stream> GenerateAsync(
        IReadOnlyList<Dictionary<string, object?>> data,
        List<string> fields,
        string exportName,
        CancellationToken ct = default);

    /// <summary>
    /// Gets the MIME type for the generated file format.
    /// </summary>
    /// <returns>MIME type string.</returns>
    string GetMimeType();

    /// <summary>
    /// Gets the file extension for the generated file format (including the dot).
    /// </summary>
    /// <returns>File extension string.</returns>
    string GetFileExtension();
}
