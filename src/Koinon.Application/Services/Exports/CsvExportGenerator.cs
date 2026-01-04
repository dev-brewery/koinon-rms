using System.Text;
using Koinon.Application.Interfaces;
using Koinon.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Koinon.Application.Services.Exports;

/// <summary>
/// Generates CSV format exports from data.
/// </summary>
public class CsvExportGenerator(ILogger<CsvExportGenerator> logger) : IExportFormatGenerator
{
    public ReportOutputFormat OutputFormat => ReportOutputFormat.Csv;

    public string GetMimeType() => "text/csv";

    public string GetFileExtension() => ".csv";

    public Task<Stream> GenerateAsync(
        IReadOnlyList<Dictionary<string, object?>> data,
        List<string> fields,
        string exportName,
        CancellationToken ct = default)
    {
        logger.LogInformation("Generating CSV export '{ExportName}' with {FieldCount} fields and {RowCount} rows",
            exportName, fields.Count, data.Count);

        var stream = new MemoryStream();
        
        // Use UTF-8 with BOM for Excel compatibility
        var encoding = new UTF8Encoding(true);
        using var writer = new StreamWriter(stream, encoding, leaveOpen: true);

        // Write header row
        var headerLine = string.Join(",", fields.Select(f => EscapeCsvValue(f)));
        writer.WriteLine(headerLine);

        // Write data rows
        foreach (var row in data)
        {
            var values = new List<string>();
            
            foreach (var field in fields)
            {
                var value = row.TryGetValue(field, out var val) ? val : null;
                values.Add(FormatCsvValue(value));
            }

            writer.WriteLine(string.Join(",", values));
        }

        writer.Flush();
        stream.Position = 0;

        logger.LogInformation("CSV export generated successfully with {ByteCount} bytes", stream.Length);

        return Task.FromResult<Stream>(stream);
    }

    private static string FormatCsvValue(object? value)
    {
        if (value == null)
        {
            return string.Empty;
        }

        // Format dates consistently
        if (value is DateTime dateTime)
        {
            return EscapeCsvValue(dateTime.ToString("yyyy-MM-dd HH:mm:ss"));
        }

        // Format decimals with 2 decimal places
        if (value is decimal decimalValue)
        {
            return decimalValue.ToString("F2");
        }

        // Format booleans as Yes/No
        if (value is bool boolValue)
        {
            return boolValue ? "Yes" : "No";
        }

        return EscapeCsvValue(value.ToString() ?? string.Empty);
    }

    private static string EscapeCsvValue(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        // If value contains comma, quote, or newline, wrap in quotes and escape quotes
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
        {
            // Escape existing quotes by doubling them
            var escaped = value.Replace("\"", "\"\"");
            return $"\"{escaped}\"";
        }

        return value;
    }
}
