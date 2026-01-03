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
/// Generates directory reports in PDF, Excel, and CSV formats.
/// Shows contact information for active people with optional photo inclusion.
/// </summary>
public class DirectoryReportGenerator(
    IApplicationDbContext context,
    ILogger<DirectoryReportGenerator> logger) : IReportGenerator
{
    public ReportOutputFormat OutputFormat { get; init; }

    public async Task<(Stream Stream, string FileName, string MimeType)> GenerateAsync(
        string reportName,
        ReportType reportType,
        IEnumerable<object> data,
        string? parameters = null,
        CancellationToken ct = default)
    {
        if (reportType != ReportType.Directory)
        {
            throw new ArgumentException(
                $"This generator only supports {ReportType.Directory} reports.",
                nameof(reportType));
        }

        // Parse parameters
        var options = ParseParameters(parameters);

        // Query directory data
        var directoryData = await QueryDirectoryDataAsync(options, ct);

        // Generate report in requested format
        return OutputFormat switch
        {
            ReportOutputFormat.Pdf => GeneratePdf(reportName, directoryData, options),
            ReportOutputFormat.Excel => GenerateExcel(reportName, directoryData),
            ReportOutputFormat.Csv => GenerateCsv(reportName, directoryData),
            _ => throw new NotSupportedException($"Output format {OutputFormat} is not supported.")
        };
    }

    private DirectoryReportOptions ParseParameters(string? parameters)
    {
        if (string.IsNullOrWhiteSpace(parameters))
        {
            return new DirectoryReportOptions();
        }

        try
        {
            return JsonSerializer.Deserialize<DirectoryReportOptions>(parameters)
                   ?? new DirectoryReportOptions();
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Failed to parse directory report parameters, using defaults");
            return new DirectoryReportOptions();
        }
    }

    private async Task<List<DirectoryRow>> QueryDirectoryDataAsync(
        DirectoryReportOptions options,
        CancellationToken ct)
    {
        var query = context.People
            .Include(p => p.PhoneNumbers)
            .Include(p => p.FamilyMemberships)
                .ThenInclude(fm => fm.Family)
            .Include(p => p.RecordStatusValue)
            .AsNoTracking();

        // Only include active people
        query = query.Where(p => p.RecordStatusValue != null && p.RecordStatusValue.Value == "Active");

        // Apply group filter if specified
        if (options.GroupId.HasValue)
        {
            query = query.Where(p => p.GroupMemberships.Any(gm => gm.GroupId == options.GroupId.Value));
        }

        var people = await query.ToListAsync(ct);

        // Map to directory rows
        var directoryData = people
            .Select(p => new DirectoryRow
            {
                FullName = p.FullName,
                Email = p.Email ?? string.Empty,
                Phone = p.PhoneNumbers
                    .Where(pn => pn.NumberTypeValueId != null)
                    .OrderBy(pn => pn.NumberTypeValueId)
                    .Select(pn => pn.Number)
                    .FirstOrDefault() ?? string.Empty,
                Address = string.Empty, // Address will be added when Location entity is available
                FamilyName = p.FamilyMemberships
                    .Select(fm => fm.Family?.Name)
                    .FirstOrDefault() ?? string.Empty
            })
            .OrderBy(r => r.FullName)
            .ToList();

        return directoryData;
    }

    private (Stream Stream, string FileName, string MimeType) GeneratePdf(
        string reportName,
        List<DirectoryRow> data,
        DirectoryReportOptions options)
    {
        var stream = new MemoryStream();

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.Letter);
                page.Margin(50);

                page.Header().Element(ComposeHeader);
                page.Content().Element(content => ComposeContent(content, data, options));
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

            void ComposeContent(IContainer container, List<DirectoryRow> rows, DirectoryReportOptions opts)
            {
                container.Table(table =>
                {
                    // Column definitions based on whether photos are included
                    if (opts.IncludePhotos)
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(60); // Photo placeholder
                            columns.RelativeColumn(3);  // Name
                            columns.RelativeColumn(2);  // Email
                            columns.RelativeColumn(2);  // Phone
                            columns.RelativeColumn(2);  // Family
                        });
                    }
                    else
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(3);  // Name
                            columns.RelativeColumn(2);  // Email
                            columns.RelativeColumn(2);  // Phone
                            columns.RelativeColumn(2);  // Family
                        });
                    }

                    table.Header(header =>
                    {
                        if (opts.IncludePhotos)
                        {
                            header.Cell().Element(CellStyle).Text("Photo").Bold();
                        }
                        header.Cell().Element(CellStyle).Text("Name").Bold();
                        header.Cell().Element(CellStyle).Text("Email").Bold();
                        header.Cell().Element(CellStyle).Text("Phone").Bold();
                        header.Cell().Element(CellStyle).Text("Family").Bold();

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
                        if (opts.IncludePhotos)
                        {
                            // Placeholder for photo - actual photo loading would require BinaryFile access
                            table.Cell().Element(CellStyle).Text("[Photo]").FontSize(8);
                        }
                        table.Cell().Element(CellStyle).Text(row.FullName);
                        table.Cell().Element(CellStyle).Text(row.Email);
                        table.Cell().Element(CellStyle).Text(row.Phone);
                        table.Cell().Element(CellStyle).Text(row.FamilyName);

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
        List<DirectoryRow> data)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Directory");

        // Headers
        worksheet.Cell(1, 1).Value = "Name";
        worksheet.Cell(1, 2).Value = "Email";
        worksheet.Cell(1, 3).Value = "Phone";
        worksheet.Cell(1, 4).Value = "Address";
        worksheet.Cell(1, 5).Value = "Family";

        // Style headers
        var headerRange = worksheet.Range(1, 1, 1, 5);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

        // Data rows
        int row = 2;
        foreach (var item in data)
        {
            worksheet.Cell(row, 1).Value = item.FullName;
            worksheet.Cell(row, 2).Value = item.Email;
            worksheet.Cell(row, 3).Value = item.Phone;
            worksheet.Cell(row, 4).Value = item.Address;
            worksheet.Cell(row, 5).Value = item.FamilyName;
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
        List<DirectoryRow> data)
    {
        var stream = new MemoryStream();
        using var writer = new StreamWriter(stream, Encoding.UTF8, leaveOpen: true);

        // Headers
        writer.WriteLine("Name,Email,Phone,Address,Family");

        // Data rows
        foreach (var row in data)
        {
            writer.WriteLine(
                $"\"{EscapeCsv(row.FullName)}\"," +
                $"\"{EscapeCsv(row.Email)}\"," +
                $"\"{EscapeCsv(row.Phone)}\"," +
                $"\"{EscapeCsv(row.Address)}\"," +
                $"\"{EscapeCsv(row.FamilyName)}\"");
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

    private class DirectoryReportOptions
    {
        public bool IncludePhotos { get; set; }
        public int? GroupId { get; set; }
    }

    private class DirectoryRow
    {
        public required string FullName { get; set; }
        public required string Email { get; set; }
        public required string Phone { get; set; }
        public required string Address { get; set; }
        public required string FamilyName { get; set; }
    }
}
