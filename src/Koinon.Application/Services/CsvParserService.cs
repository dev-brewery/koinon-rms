using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using CsvHelper;
using CsvHelper.Configuration;
using Koinon.Application.DTOs.Import;
using Koinon.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Koinon.Application.Services;

/// <summary>
/// Service for parsing and validating CSV files using CsvHelper.
/// Supports multiple encodings, delimiters, and streaming for large files.
/// </summary>
public partial class CsvParserService(ILogger<CsvParserService> logger) : ICsvParserService
{
    private const int MaxFileSizeBytes = 10 * 1024 * 1024; // 10MB
    private const int PreviewSampleSize = 5;

    private static readonly char[] _supportedDelimiters = [',', ';', '\t'];

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailRegex();

    [GeneratedRegex(@"^\d{10,11}$")]
    private static partial Regex PhoneRegex();

    /// <inheritdoc />
    public async Task<CsvPreviewDto> GeneratePreviewAsync(
        Stream fileStream,
        CancellationToken cancellationToken = default)
    {
        ValidateFileSize(fileStream);

        var (encoding, delimiter) = await DetectFormatAsync(fileStream, cancellationToken);
        fileStream.Position = 0; // Reset for reading

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = delimiter.ToString(),
            HasHeaderRecord = true,
            TrimOptions = TrimOptions.Trim,
            BadDataFound = context => logger.LogWarning(
                "Bad data at row {Row}, field {Field}: {RawRecord}",
                context.Context?.Parser?.Row ?? 0,
                context.Field,
                context.RawRecord)
        };

        using var reader = new StreamReader(fileStream, encoding, leaveOpen: true);
        using var csv = new CsvReader(reader, config);

        await csv.ReadAsync();
        csv.ReadHeader();
        var headers = csv.HeaderRecord?.ToList() ?? [];

        // Validate no duplicate headers
        var duplicateHeaders = headers
            .GroupBy(h => h, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicateHeaders.Count != 0)
        {
            throw new InvalidOperationException(
                $"CSV contains duplicate headers: {string.Join(", ", duplicateHeaders)}");
        }

        // Read sample rows and count all rows
        var sampleRows = new List<IReadOnlyList<string>>();
        var rowCount = 0;

        while (await csv.ReadAsync())
        {
            rowCount++;

            // Only capture first N rows as sample
            if (sampleRows.Count < PreviewSampleSize)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var row = new List<string>();
                for (var i = 0; i < headers.Count; i++)
                {
                    row.Add(csv.GetField(i) ?? string.Empty);
                }
                sampleRows.Add(row);
            }
        }

        return new CsvPreviewDto(
            Headers: headers,
            SampleRows: sampleRows,
            TotalRowCount: rowCount,
            DetectedDelimiter: GetDelimiterName(delimiter),
            DetectedEncoding: encoding.WebName.ToUpperInvariant()
        );
    }

    /// <inheritdoc />
    public async Task<List<CsvValidationError>> ValidateFileAsync(
        Stream fileStream,
        IReadOnlyList<string> requiredColumns,
        CancellationToken cancellationToken = default)
    {
        ValidateFileSize(fileStream);

        var errors = new List<CsvValidationError>();
        var (encoding, delimiter) = await DetectFormatAsync(fileStream, cancellationToken);
        fileStream.Position = 0;

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = delimiter.ToString(),
            HasHeaderRecord = true,
            TrimOptions = TrimOptions.Trim,
            BadDataFound = context =>
            {
                errors.Add(new CsvValidationError(
                    RowNumber: context.Context?.Parser?.Row ?? 0,
                    ColumnName: context.Field ?? "Unknown",
                    Value: context.RawRecord ?? string.Empty,
                    ErrorMessage: "Malformed data detected"
                ));
            }
        };

        using var reader = new StreamReader(fileStream, encoding, leaveOpen: true);
        using var csv = new CsvReader(reader, config);

        await csv.ReadAsync();
        csv.ReadHeader();
        var headers = csv.HeaderRecord?.ToList() ?? [];

        // Check required columns
        var missingColumns = requiredColumns
            .Where(rc => !headers.Contains(rc, StringComparer.OrdinalIgnoreCase))
            .ToList();

        if (missingColumns.Count != 0)
        {
            errors.Add(new CsvValidationError(
                RowNumber: 0,
                ColumnName: string.Join(", ", missingColumns),
                Value: string.Empty,
                ErrorMessage: $"Required columns missing: {string.Join(", ", missingColumns)}"
            ));
            return errors; // Can't validate rows without required columns
        }

        // Build case-insensitive column index map
        var columnMap = headers
            .Select((name, index) => new { name, index })
            .ToDictionary(x => x.name, x => x.index, StringComparer.OrdinalIgnoreCase);

        var rowNumber = 1; // Header is row 0
        while (await csv.ReadAsync())
        {
            rowNumber++;

            // Validate required fields are not empty
            foreach (var column in requiredColumns)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!columnMap.TryGetValue(column, out var columnIndex))
                {
                    continue;
                }

                var value = csv.GetField(columnIndex)?.Trim() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(value))
                {
                    errors.Add(new CsvValidationError(
                        RowNumber: rowNumber,
                        ColumnName: column,
                        Value: value,
                        ErrorMessage: "Required field is empty"
                    ));
                }
            }

            // Validate email fields (columns containing "email" in name)
            var emailColumns = headers.Where(h => h.Contains("email", StringComparison.OrdinalIgnoreCase));
            foreach (var emailColumn in emailColumns)
            {
                var value = csv.GetField(columnMap[emailColumn])?.Trim();
                if (!string.IsNullOrWhiteSpace(value) && !EmailRegex().IsMatch(value))
                {
                    errors.Add(new CsvValidationError(
                        RowNumber: rowNumber,
                        ColumnName: emailColumn,
                        Value: value,
                        ErrorMessage: "Invalid email format"
                    ));
                }
            }

            // Validate phone fields (columns containing "phone" in name)
            var phoneColumns = headers.Where(h => h.Contains("phone", StringComparison.OrdinalIgnoreCase));
            foreach (var phoneColumn in phoneColumns)
            {
                var value = csv.GetField(columnMap[phoneColumn])?.Trim();
                if (!string.IsNullOrWhiteSpace(value))
                {
                    var normalized = NormalizePhone(value);
                    if (!PhoneRegex().IsMatch(normalized))
                    {
                        errors.Add(new CsvValidationError(
                            RowNumber: rowNumber,
                            ColumnName: phoneColumn,
                            Value: value,
                            ErrorMessage: "Invalid phone number (expected 10-11 digits)"
                        ));
                    }
                }
            }

            // Validate date fields (columns containing "date" in name)
            var dateColumns = headers.Where(h => h.Contains("date", StringComparison.OrdinalIgnoreCase));
            foreach (var dateColumn in dateColumns)
            {
                var value = csv.GetField(columnMap[dateColumn])?.Trim();
                if (!string.IsNullOrWhiteSpace(value) && !TryParseDate(value, out _))
                {
                    errors.Add(new CsvValidationError(
                        RowNumber: rowNumber,
                        ColumnName: dateColumn,
                        Value: value,
                        ErrorMessage: "Invalid date format (expected yyyy-MM-dd, MM/dd/yyyy, or dd/MM/yyyy)"
                    ));
                }
            }
        }

        logger.LogInformation(
            "CSV validation completed: {RowCount} rows, {ErrorCount} errors",
            rowNumber - 1,
            errors.Count);

        return errors;
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<Dictionary<string, string>> StreamRowsAsync(
        Stream fileStream,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ValidateFileSize(fileStream);

        var (encoding, delimiter) = await DetectFormatAsync(fileStream, cancellationToken);
        fileStream.Position = 0;

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = delimiter.ToString(),
            HasHeaderRecord = true,
            TrimOptions = TrimOptions.Trim
        };

        using var reader = new StreamReader(fileStream, encoding, leaveOpen: true);
        using var csv = new CsvReader(reader, config);

        await csv.ReadAsync();
        csv.ReadHeader();
        var headers = csv.HeaderRecord?.ToList() ?? [];

        while (await csv.ReadAsync())
        {
            cancellationToken.ThrowIfCancellationRequested();

            var row = new Dictionary<string, string>();
            for (var i = 0; i < headers.Count; i++)
            {
                row[headers[i]] = csv.GetField(i) ?? string.Empty;
            }

            yield return row;
        }
    }

    private static void ValidateFileSize(Stream fileStream)
    {
        if (fileStream.Length > MaxFileSizeBytes)
        {
            throw new InvalidOperationException(
                $"File size {fileStream.Length} bytes exceeds maximum allowed size of {MaxFileSizeBytes} bytes (10MB)");
        }
    }

    private async Task<(Encoding encoding, char delimiter)> DetectFormatAsync(
        Stream fileStream,
        CancellationToken cancellationToken)
    {
        var buffer = new byte[4096];
        var bytesRead = await fileStream.ReadAsync(buffer, cancellationToken);
        fileStream.Position = 0;

        // Detect encoding
        var encoding = DetectEncoding(buffer.AsSpan(0, bytesRead));

        // Detect delimiter by counting occurrences in first few lines
        var sample = encoding.GetString(buffer, 0, bytesRead);
        var lines = sample.Split('\n', StringSplitOptions.RemoveEmptyEntries).Take(5).ToList();

        var delimiter = ','; // Default
        if (lines.Count > 0)
        {
            var delimiterCounts = _supportedDelimiters
                .Select(d => new { Delimiter = d, Count = lines[0].Count(c => c == d) })
                .OrderByDescending(x => x.Count)
                .ToList();

            if (delimiterCounts[0].Count > 0)
            {
                delimiter = delimiterCounts[0].Delimiter;
            }
        }

        logger.LogInformation(
            "Detected CSV format: encoding={Encoding}, delimiter={Delimiter}",
            encoding.WebName,
            GetDelimiterName(delimiter));

        return (encoding, delimiter);
    }

    private static Encoding DetectEncoding(ReadOnlySpan<byte> bytes)
    {
        // Check for UTF-8 BOM
        if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
        {
            return new UTF8Encoding(true);
        }

        // Check for UTF-16 BOMs
        if (bytes.Length >= 2)
        {
            if (bytes[0] == 0xFE && bytes[1] == 0xFF)
            {
                return Encoding.BigEndianUnicode;
            }
            if (bytes[0] == 0xFF && bytes[1] == 0xFE)
            {
                return Encoding.Unicode;
            }
        }

        // Default to UTF-8 without BOM
        return Encoding.UTF8;
    }

    private static string GetDelimiterName(char delimiter) => delimiter switch
    {
        ',' => "Comma",
        ';' => "Semicolon",
        '\t' => "Tab",
        _ => "Unknown"
    };

    private static string NormalizePhone(string phone)
    {
        return new string(phone.Where(char.IsDigit).ToArray());
    }

    private static bool TryParseDate(string value, out DateTime date)
    {
        string[] formats =
        [
            "yyyy-MM-dd",
            "MM/dd/yyyy",
            "dd/MM/yyyy",
            "yyyy/MM/dd",
            "M/d/yyyy",
            "d/M/yyyy"
        ];

        return DateTime.TryParseExact(
            value,
            formats,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out date);
    }
}
