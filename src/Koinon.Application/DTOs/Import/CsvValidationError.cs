namespace Koinon.Application.DTOs.Import;

/// <summary>
/// Represents a validation error found in a CSV file row.
/// </summary>
public record CsvValidationError(
    int RowNumber,
    string ColumnName,
    string Value,
    string ErrorMessage
);
