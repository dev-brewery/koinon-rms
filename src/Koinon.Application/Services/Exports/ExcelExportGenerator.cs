using ClosedXML.Excel;
using Koinon.Application.Interfaces;
using Koinon.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Koinon.Application.Services.Exports;

/// <summary>
/// Generates Excel (XLSX) format exports from data using ClosedXML.
/// </summary>
public class ExcelExportGenerator(ILogger<ExcelExportGenerator> logger) : IExportFormatGenerator
{
    public ReportOutputFormat OutputFormat => ReportOutputFormat.Excel;

    public string GetMimeType() => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    public string GetFileExtension() => ".xlsx";

    public Task<Stream> GenerateAsync(
        IReadOnlyList<Dictionary<string, object?>> data,
        List<string> fields,
        string exportName,
        CancellationToken ct = default)
    {
        logger.LogInformation("Generating Excel export '{ExportName}' with {FieldCount} fields and {RowCount} rows",
            exportName, fields.Count, data.Count);

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add(SanitizeSheetName(exportName));

        // Write header row
        for (int col = 0; col < fields.Count; col++)
        {
            var cell = worksheet.Cell(1, col + 1);
            cell.Value = fields[col];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.LightGray;
            cell.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
        }

        // Write data rows
        int row = 2;
        foreach (var dataRow in data)
        {
            for (int col = 0; col < fields.Count; col++)
            {
                var field = fields[col];
                var value = dataRow.TryGetValue(field, out var val) ? val : null;
                
                var cell = worksheet.Cell(row, col + 1);
                SetCellValue(cell, value);
            }
            row++;
        }

        // Auto-fit columns
        worksheet.Columns().AdjustToContents();

        // Freeze the header row
        worksheet.SheetView.FreezeRows(1);

        // Apply filter to header row
        worksheet.RangeUsed()?.SetAutoFilter();

        // Save to stream
        var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;

        logger.LogInformation("Excel export generated successfully with {ByteCount} bytes", stream.Length);

        return Task.FromResult<Stream>(stream);
    }

    private static void SetCellValue(IXLCell cell, object? value)
    {
        if (value == null)
        {
            cell.Value = string.Empty;
            return;
        }

        switch (value)
        {
            case DateTime dateTime:
                cell.Value = dateTime;
                cell.Style.NumberFormat.Format = "yyyy-mm-dd hh:mm:ss";
                break;

            case decimal decimalValue:
                cell.Value = decimalValue;
                cell.Style.NumberFormat.Format = "#,##0.00";
                break;

            case double doubleValue:
                cell.Value = doubleValue;
                cell.Style.NumberFormat.Format = "#,##0.00";
                break;

            case int intValue:
                cell.Value = intValue;
                cell.Style.NumberFormat.Format = "#,##0";
                break;

            case long longValue:
                cell.Value = longValue;
                cell.Style.NumberFormat.Format = "#,##0";
                break;

            case bool boolValue:
                cell.Value = boolValue ? "Yes" : "No";
                break;

            default:
                cell.Value = value.ToString();
                break;
        }
    }

    private static string SanitizeSheetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "Export";
        }

        // Excel worksheet names cannot exceed 31 characters and cannot contain: \ / ? * [ ]
        var sanitized = name;
        var invalidChars = new[] { '\\', '/', '?', '*', '[', ']' };
        
        foreach (var c in invalidChars)
        {
            sanitized = sanitized.Replace(c, '_');
        }

        if (sanitized.Length > 31)
        {
            sanitized = sanitized.Substring(0, 31);
        }

        return sanitized;
    }
}
