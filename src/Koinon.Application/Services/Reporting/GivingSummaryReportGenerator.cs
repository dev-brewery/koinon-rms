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
/// Generates giving summary reports in PDF, Excel, and CSV formats.
/// Shows contribution totals grouped by fund with optional filtering.
/// </summary>
public class GivingSummaryReportGenerator(
    IApplicationDbContext context,
    ILogger<GivingSummaryReportGenerator> logger) : IReportGenerator
{
    public ReportOutputFormat OutputFormat { get; init; }

    public async Task<(Stream Stream, string FileName, string MimeType)> GenerateAsync(
        string reportName,
        ReportType reportType,
        IEnumerable<object> data,
        string? parameters = null,
        CancellationToken ct = default)
    {
        if (reportType != ReportType.GivingSummary)
        {
            throw new ArgumentException(
                $"This generator only supports {ReportType.GivingSummary} reports.",
                nameof(reportType));
        }

        // Parse parameters
        var options = ParseParameters(parameters);

        // Query giving data
        var givingData = await QueryGivingDataAsync(options, ct);

        // Generate report in requested format
        return OutputFormat switch
        {
            ReportOutputFormat.Pdf => GeneratePdf(reportName, givingData),
            ReportOutputFormat.Excel => GenerateExcel(reportName, givingData),
            ReportOutputFormat.Csv => GenerateCsv(reportName, givingData),
            _ => throw new NotSupportedException($"Output format {OutputFormat} is not supported.")
        };
    }

    private GivingReportOptions ParseParameters(string? parameters)
    {
        if (string.IsNullOrWhiteSpace(parameters))
        {
            return new GivingReportOptions();
        }

        try
        {
            return JsonSerializer.Deserialize<GivingReportOptions>(parameters)
                   ?? new GivingReportOptions();
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Failed to parse giving report parameters, using defaults");
            return new GivingReportOptions();
        }
    }

    private async Task<List<GivingSummaryRow>> QueryGivingDataAsync(
        GivingReportOptions options,
        CancellationToken ct)
    {
        var query = context.ContributionDetails
            .Include(cd => cd.Contribution)
            .Include(cd => cd.Fund)
            .AsNoTracking();

        // Apply date range filters on the contribution
        if (options.StartDate.HasValue)
        {
            query = query.Where(cd => cd.Contribution!.TransactionDateTime >= options.StartDate.Value);
        }

        if (options.EndDate.HasValue)
        {
            query = query.Where(cd => cd.Contribution!.TransactionDateTime <= options.EndDate.Value);
        }

        // Apply fund filter
        if (options.FundId.HasValue)
        {
            query = query.Where(cd => cd.FundId == options.FundId.Value);
        }

        // Apply person filter
        if (options.PersonId.HasValue)
        {
            query = query.Where(cd => cd.Contribution!.PersonAliasId == options.PersonId.Value);
        }

        var details = await query.ToListAsync(ct);

        // Group and summarize by fund
        var summaryData = details
            .Where(cd => cd.Fund != null)
            .GroupBy(cd => new
            {
                FundName = cd.Fund!.Name,
                FundId = cd.FundId
            })
            .Select(g => new GivingSummaryRow
            {
                FundName = g.Key.FundName,
                ContributionCount = g.Select(cd => cd.ContributionId).Distinct().Count(),
                TotalAmount = g.Sum(cd => cd.Amount)
            })
            .OrderBy(r => r.FundName)
            .ToList();

        return summaryData;
    }

    private (Stream Stream, string FileName, string MimeType) GeneratePdf(
        string reportName,
        List<GivingSummaryRow> data)
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

            void ComposeContent(IContainer container, List<GivingSummaryRow> rows)
            {
                container.Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(4);
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(2);
                    });

                    table.Header(header =>
                    {
                        header.Cell().Element(HeaderCellStyle).Text("Fund").Bold();
                        header.Cell().Element(HeaderCellStyle).AlignRight().Text("Contributions").Bold();
                        header.Cell().Element(HeaderCellStyle).AlignRight().Text("Total Amount").Bold();

                        static IContainer HeaderCellStyle(IContainer container)
                        {
                            return container
                                .Border(1)
                                .BorderColor(Colors.Grey.Lighten2)
                                .Padding(5);
                        }
                    });

                    foreach (var row in rows)
                    {
                        table.Cell().Element(DataCellStyle).Text(row.FundName);
                        table.Cell().Element(DataCellStyle).AlignRight().Text(row.ContributionCount.ToString());
                        table.Cell().Element(DataCellStyle).AlignRight().Text($"${row.TotalAmount:N2}");

                        static IContainer DataCellStyle(IContainer container)
                        {
                            return container
                                .Border(1)
                                .BorderColor(Colors.Grey.Lighten2)
                                .Padding(5);
                        }
                    }

                    // Add totals row
                    var totalContributions = rows.Sum(r => r.ContributionCount);
                    var totalAmount = rows.Sum(r => r.TotalAmount);

                    table.Cell().Element(TotalCellStyle).Text("TOTAL").Bold();
                    table.Cell().Element(TotalCellStyle).AlignRight().Text(totalContributions.ToString()).Bold();
                    table.Cell().Element(TotalCellStyle).AlignRight().Text($"${totalAmount:N2}").Bold();

                    static IContainer TotalCellStyle(IContainer container)
                    {
                        return container
                            .Border(1)
                            .BorderColor(Colors.Grey.Lighten2)
                            .Background(Colors.Grey.Lighten3)
                            .Padding(5);
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
        List<GivingSummaryRow> data)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Giving Summary");

        // Headers
        worksheet.Cell(1, 1).Value = "Fund";
        worksheet.Cell(1, 2).Value = "Contribution Count";
        worksheet.Cell(1, 3).Value = "Total Amount";

        // Style headers
        var headerRange = worksheet.Range(1, 1, 1, 3);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

        // Data rows
        int row = 2;
        foreach (var item in data)
        {
            worksheet.Cell(row, 1).Value = item.FundName;
            worksheet.Cell(row, 2).Value = item.ContributionCount;
            worksheet.Cell(row, 3).Value = item.TotalAmount;
            worksheet.Cell(row, 3).Style.NumberFormat.Format = "$#,##0.00";
            row++;
        }

        // Add totals row
        worksheet.Cell(row, 1).Value = "TOTAL";
        worksheet.Cell(row, 1).Style.Font.Bold = true;
        worksheet.Cell(row, 2).FormulaA1 = $"=SUM(B2:B{row - 1})";
        worksheet.Cell(row, 2).Style.Font.Bold = true;
        worksheet.Cell(row, 3).FormulaA1 = $"=SUM(C2:C{row - 1})";
        worksheet.Cell(row, 3).Style.Font.Bold = true;
        worksheet.Cell(row, 3).Style.NumberFormat.Format = "$#,##0.00";

        var totalRange = worksheet.Range(row, 1, row, 3);
        totalRange.Style.Fill.BackgroundColor = XLColor.LightGray;

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
        List<GivingSummaryRow> data)
    {
        var stream = new MemoryStream();
        using var writer = new StreamWriter(stream, Encoding.UTF8, leaveOpen: true);

        // Headers
        writer.WriteLine("Fund,Contribution Count,Total Amount");

        // Data rows
        foreach (var row in data)
        {
            writer.WriteLine($"\"{EscapeCsv(row.FundName)}\",{row.ContributionCount},{row.TotalAmount:F2}");
        }

        // Totals row
        var totalContributions = data.Sum(r => r.ContributionCount);
        var totalAmount = data.Sum(r => r.TotalAmount);
        writer.WriteLine($"\"TOTAL\",{totalContributions},{totalAmount:F2}");

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

    private class GivingReportOptions
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int? FundId { get; set; }
        public int? PersonId { get; set; }
    }

    private class GivingSummaryRow
    {
        public required string FundName { get; set; }
        public int ContributionCount { get; set; }
        public decimal TotalAmount { get; set; }
    }
}
