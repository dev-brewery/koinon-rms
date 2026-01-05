using Koinon.Domain.Enums;

namespace Koinon.Application.Interfaces.Reporting;

/// <summary>
/// Defines the contract for report data providers that retrieve data for report generation.
/// </summary>
public interface IReportDataProvider
{
    /// <summary>
    /// The type of report this provider handles.
    /// </summary>
    ReportType ReportType { get; }

    /// <summary>
    /// Retrieves the data for report generation based on the specified parameters.
    /// </summary>
    /// <param name="parametersJson">JSON string containing report parameters and filters.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Collection of data rows where each row is a dictionary of field name to value.</returns>
    Task<IReadOnlyList<Dictionary<string, object?>>> GetDataAsync(
        string parametersJson,
        CancellationToken ct = default);
}
