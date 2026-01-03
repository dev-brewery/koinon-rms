using System.Globalization;
using System.Text;
using System.Text.Json;
using ClosedXML.Excel;
using Koinon.Application.Interfaces;
using Koinon.Application.Interfaces.Reporting;
using Koinon.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Koinon.Application.Services.Reporting;

/// <summary>
/// Generates attendance summary reports in PDF, Excel, and CSV formats.
/// Shows attendance statistics grouped by group and date.
/// </summary>
public class AttendanceSummaryReportGenerator(
    IApplicationDbContext context,
    ILogger<AttendanceSummaryReportGenerator> logger) : IReportGenerator
{
    public ReportOutputFormat OutputFormat { get; init; }

    public async Task<(Stream Stream, string FileName, string MimeType)> GenerateAsync(
        string reportName,
        ReportType reportType,
        IEnumerable<object> data,
        string? parameters = null,
        CancellationToken ct = default)
    {
        if (reportType != ReportType.AttendanceSummary)
        {
            throw new ArgumentException(
                $"This generator only supports {ReportType.AttendanceSummary} reports.",
                nameof(reportType));
        }

        // Parse parameters
        var options = ParseParameters(parameters);

        // Query attendance data
        var attendanceData = await QueryAttendanceDataAsync(options, ct);

        // Generate report in requested format
        return OutputFormat switch
        {
            ReportOutputFormat.Pdf => GeneratePdf(reportName, attendanceData),
            ReportOutputFormat.Excel => GenerateExcel(reportName, attendanceData),
            ReportOutputFormat.Csv => GenerateCsv(reportName, attendanceData),
            _ => throw new NotSupportedException($"Output format {OutputFormat} is not supported.")
        };
    }

    private AttendanceReportOptions ParseParameters(string? parameters)
    {
        if (string.IsNullOrWhiteSpace(parameters))
        {
            return new AttendanceReportOptions();
        }

        try
        {
            return JsonSerializer.Deserialize<AttendanceReportOptions>(parameters)
                   ?? new AttendanceReportOptions();
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Failed to parse attendance report parameters, using defaults");
            return new AttendanceReportOptions();
        }
    }

    private async Task<List<AttendanceSummaryRow>> QueryAttendanceDataAsync(
        AttendanceReportOptions options,
        CancellationToken ct)
    {
        var query = context.Attendances
            .Include(a => a.Occurrence)
            .ThenInclude(o => o!.Group)
            .AsNoTracking();

        // Apply date range filters
        if (options.StartDate.HasValue)
        {
            var startDateOnly = DateOnly.FromDateTime(options.StartDate.Value);
            query = query.Where(a => a.Occurrence!.OccurrenceDate >= startDateOnly);
        }

        if (options.EndDate.HasValue)
        {
            var endDateOnly = DateOnly.FromDateTime(options.EndDate.Value);
            query = query.Where(a => a.Occurrence!.OccurrenceDate <= endDateOnly);
        }

        // Apply group filter
        if (options.GroupId.HasValue)
        {
            query = query.Where(a => a.Occurrence!.GroupId == options.GroupId.Value);
        }

        var attendances = await query.ToListAsync(ct);

        // Group and summarize data
        var summaryData = attendances
            .Where(a => a.Occurrence?.Group != null)
            .GroupBy(a => new
            {
                GroupName = a.Occurrence!.Group!.Name,
                Date = a.Occurrence.OccurrenceDate
            })
            .Select(g => new AttendanceSummaryRow
            {
                GroupName = g.Key.GroupName,
                Date = g.Key.Date,
                PresentCount = g.Count(a => a.DidAttend == true),
                AbsentCount = g.Count(a => a.DidAttend == false)
            })
            .OrderBy(r => r.Date)
            .ThenBy(r => r.GroupName)
            .ToList();

        return summaryData;
    }

    private (Stream Stream, string FileName, string MimeType) GeneratePdf(
        string reportName,
        List<AttendanceSummaryRow> data)
    {
        var stream = new MemoryStream();

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.Letter);
                page.Margin(50);

                page.Header().Element(ComposeHeader);
                page.Content().Element(content => ComposeContent(content, data));
                page.Footer().AlignCenter().Text(x =>
                {
                    x.CurrentPageNumber();
                    x.Span(" / ");
                    x.TotalPages();
                });
            });

            void ComposeHeader(IContainer container)
            {
                container.Row(row =>
                {
                    row.RelativeItem().Column(column =>
                    {
                        column.Item().Text(reportName)
                            .FontSize(20)
                            .Bold();
                        column.Item().Text($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm}")
                            .FontSize(10);
                    });
                });
            }

            void ComposeContent(IContainer container, List<AttendanceSummaryRow> rows)
            {
                container.Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(3);
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(1);
                        columns.RelativeColumn(1);
                    });

                    table.Header(header =>
                    {
                        header.Cell().Element(CellStyle).Text("Group").Bold();
                        header.Cell().Element(CellStyle).Text("Date").Bold();
                        header.Cell().Element(CellStyle).AlignRight().Text("Present").Bold();
                        header.Cell().Element(CellStyle).AlignRight().Text("Absent").Bold();

                        static IContainer CellStyle(IContainer container)
                        {
                            return container
                                .Border(1)
                                .BorderColor(Colors.Grey.Lighten2)
                                .Padding(5);
                        }
                    });

                    foreach (var row in rows)
                    {
                        table.Cell().Element(CellStyle).Text(row.GroupName);
                        table.Cell().Element(CellStyle).Text(row.Date.ToString("yyyy-MM-dd"));
                        table.Cell().Element(CellStyle).AlignRight().Text(row.PresentCount.ToString());
                        table.Cell().Element(CellStyle).AlignRight().Text(row.AbsentCount.ToString());

                        static IContainer CellStyle(IContainer container)
                        {
                            return container
                                .Border(1)
                                .BorderColor(Colors.Grey.Lighten2)
                                .Padding(5);
                        }
                    }
                });
            }
        }).GeneratePdf(stream);

        stream.Position = 0;

        var fileName = $"{SanitizeFileName(reportName)}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
        return (stream, fileName, "application/pdf");
    }

    private (Stream Stream, string FileName, string MimeType) GenerateExcel(
        string reportName,
        List<AttendanceSummaryRow> data)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Attendance Summary");

        // Headers
        worksheet.Cell(1, 1).Value = "Group";
        worksheet.Cell(1, 2).Value = "Date";
        worksheet.Cell(1, 3).Value = "Present Count";
        worksheet.Cell(1, 4).Value = "Absent Count";

        // Style headers
        var headerRange = worksheet.Range(1, 1, 1, 4);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

        // Data rows
        int row = 2;
        foreach (var item in data)
        {
            worksheet.Cell(row, 1).Value = item.GroupName;
            worksheet.Cell(row, 2).Value = item.Date.ToString("yyyy-MM-dd");
            worksheet.Cell(row, 3).Value = item.PresentCount;
            worksheet.Cell(row, 4).Value = item.AbsentCount;
            row++;
        }

        // Auto-fit columns
        worksheet.Columns().AdjustToContents();

        var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;

        var fileName = $"{SanitizeFileName(reportName)}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
        return (stream, fileName, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
    }

    private (Stream Stream, string FileName, string MimeType) GenerateCsv(
        string reportName,
        List<AttendanceSummaryRow> data)
    {
        var stream = new MemoryStream();
        using var writer = new StreamWriter(stream, Encoding.UTF8, leaveOpen: true);

        // Headers
        writer.WriteLine("Group,Date,Present Count,Absent Count");

        // Data rows
        foreach (var row in data)
        {
            writer.WriteLine($"\"{EscapeCsv(row.GroupName)}\",\"{row.Date:yyyy-MM-dd}\",{row.PresentCount},{row.AbsentCount}");
        }

        writer.Flush();
        stream.Position = 0;

        var fileName = $"{SanitizeFileName(reportName)}_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
        return (stream, fileName, "text/csv");
    }

    private static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return string.Join("_", name.Split(invalid, StringSplitOptions.RemoveEmptyEntries)).TrimEnd('.');
    }

    private static string EscapeCsv(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        return value.Contains('"') ? value.Replace("\"", "\"\"") : value;
    }

    private class AttendanceReportOptions
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int? GroupId { get; set; }
    }

    private class AttendanceSummaryRow
    {
        public required string GroupName { get; set; }
        public required DateOnly Date { get; set; }
        public int PresentCount { get; set; }
        public int AbsentCount { get; set; }
    }
}
