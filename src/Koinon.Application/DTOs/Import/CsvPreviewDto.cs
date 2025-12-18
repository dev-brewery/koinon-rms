namespace Koinon.Application.DTOs.Import;

/// <summary>
/// Preview of a CSV file including headers, sample data, and metadata.
/// </summary>
public record CsvPreviewDto(
    IReadOnlyList<string> Headers,
    IReadOnlyList<IReadOnlyList<string>> SampleRows,
    int TotalRowCount,
    string DetectedDelimiter,
    string DetectedEncoding
);
