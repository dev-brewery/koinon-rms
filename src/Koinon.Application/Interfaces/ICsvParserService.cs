using Koinon.Application.DTOs.Import;

namespace Koinon.Application.Interfaces;

/// <summary>
/// Service for parsing and validating CSV files with support for various encodings and delimiters.
/// </summary>
public interface ICsvParserService
{
    /// <summary>
    /// Generates a preview of a CSV file including headers, sample rows, and metadata.
    /// </summary>
    /// <param name="fileStream">The CSV file stream to preview.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A preview containing headers, first 5 rows, total count, and detected format info.</returns>
    Task<CsvPreviewDto> GeneratePreviewAsync(Stream fileStream, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a CSV file against required columns and data quality rules.
    /// </summary>
    /// <param name="fileStream">The CSV file stream to validate.</param>
    /// <param name="requiredColumns">List of column names that must be present.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of validation errors found (empty if valid).</returns>
    Task<List<CsvValidationError>> ValidateFileAsync(
        Stream fileStream,
        IReadOnlyList<string> requiredColumns,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Streams rows from a CSV file as key-value dictionaries (column name => value).
    /// Efficient for processing large files without loading entire file into memory.
    /// </summary>
    /// <param name="fileStream">The CSV file stream to process.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Async enumerable of row dictionaries.</returns>
    IAsyncEnumerable<Dictionary<string, string>> StreamRowsAsync(
        Stream fileStream,
        CancellationToken cancellationToken = default);
}
