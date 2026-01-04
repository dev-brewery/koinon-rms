using Koinon.Application.DTOs;
using Koinon.Domain.Enums;

namespace Koinon.Application.Interfaces;

/// <summary>
/// Defines the contract for export data providers that retrieve and format data for export operations.
/// </summary>
public interface IExportDataProvider
{
    /// <summary>
    /// The type of export this provider handles.
    /// </summary>
    ExportType ExportType { get; }

    /// <summary>
    /// Gets the list of fields available for export from this provider.
    /// </summary>
    /// <returns>List of available export fields with metadata.</returns>
    List<ExportFieldDto> GetAvailableFields();

    /// <summary>
    /// Retrieves the data for export based on the specified fields and filters.
    /// </summary>
    /// <param name="fields">List of field names to include in the export. If null, all default fields are included.</param>
    /// <param name="filters">Optional filters to apply when retrieving data.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Collection of data rows where each row is a dictionary of field name to value.</returns>
    Task<IReadOnlyList<Dictionary<string, object?>>> GetDataAsync(
        List<string>? fields,
        Dictionary<string, string>? filters,
        CancellationToken ct = default);
}
