using Koinon.Application.DTOs.Giving;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Koinon.Application.Services;

/// <summary>
/// Generates PDF contribution statements using QuestPDF.
/// </summary>
public class StatementPdfGenerator
{
    /// <summary>
    /// Generates a PDF byte array for a contribution statement.
    /// </summary>
    /// <param name="preview">The statement preview data.</param>
    /// <returns>PDF file bytes.</returns>
    public byte[] GeneratePdf(StatementPreviewDto preview)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.Letter);
                page.Margin(50);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                page.Header().Element(ComposeHeader);
                page.Content().Element(ComposeContent);
                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("Page ");
                    x.CurrentPageNumber();
                    x.Span(" of ");
                    x.TotalPages();
                });

                void ComposeHeader(IContainer container)
                {
                    container.Column(column =>
                    {
                        column.Item().PaddingBottom(10).Column(headerColumn =>
                        {
                            headerColumn.Item().Text(preview.ChurchName).FontSize(16).Bold();
                            headerColumn.Item().Text(preview.ChurchAddress).FontSize(9);
                        });

                        column.Item().PaddingVertical(10).BorderBottom(1).BorderColor(Colors.Grey.Lighten2);

                        column.Item().PaddingTop(10).Row(row =>
                        {
                            row.RelativeItem().Column(recipientColumn =>
                            {
                                recipientColumn.Item().Text(preview.PersonName).FontSize(12).Bold();
                                recipientColumn.Item().Text(preview.PersonAddress).FontSize(9);
                            });

                            row.RelativeItem().AlignRight().Column(dateColumn =>
                            {
                                dateColumn.Item().Text($"Statement Period").FontSize(9).Bold();
                                dateColumn.Item().Text($"{preview.StartDate:MMMM d, yyyy} - {preview.EndDate:MMMM d, yyyy}").FontSize(9);
                                dateColumn.Item().PaddingTop(5).Text($"Generated: {DateTime.UtcNow:MMMM d, yyyy}").FontSize(9);
                            });
                        });
                    });
                }

                void ComposeContent(IContainer container)
                {
                    container.PaddingVertical(20).Column(column =>
                    {
                        column.Item().Text("Contribution Summary").FontSize(14).Bold();
                        column.Item().PaddingBottom(10).Text("Thank you for your faithful giving.").FontSize(9);

                        // Contribution table
                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(2); // Date
                                columns.RelativeColumn(3); // Fund
                                columns.RelativeColumn(2); // Check/Transaction
                                columns.RelativeColumn(2); // Amount
                            });

                            // Header
                            table.Header(header =>
                            {
                                header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Date").Bold();
                                header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Fund").Bold();
                                header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Check #").Bold();
                                header.Cell().Background(Colors.Grey.Lighten2).Padding(5).AlignRight().Text("Amount").Bold();
                            });

                            // Rows
                            foreach (var contribution in preview.Contributions.OrderBy(c => c.Date))
                            {
                                table.Cell().Padding(5).Text(contribution.Date.ToString("MM/dd/yyyy"));
                                table.Cell().Padding(5).Text(contribution.FundName);
                                table.Cell().Padding(5).Text(contribution.CheckNumber ?? "-");
                                table.Cell().Padding(5).AlignRight().Text($"${contribution.Amount:N2}");
                            }

                            // Total row
                            table.Cell().ColumnSpan(3).Background(Colors.Grey.Lighten3).Padding(5).AlignRight().Text("Total Tax-Deductible Contributions:").Bold();
                            table.Cell().Background(Colors.Grey.Lighten3).Padding(5).AlignRight().Text($"${preview.TotalAmount:N2}").Bold().FontSize(11);
                        });

                        // Tax notice
                        column.Item().PaddingTop(20).PaddingHorizontal(10).Column(noticeColumn =>
                        {
                            noticeColumn.Item().BorderTop(1).BorderColor(Colors.Grey.Lighten2).PaddingTop(10);
                            noticeColumn.Item().Text("Tax Information").FontSize(10).Bold();
                            noticeColumn.Item().PaddingTop(5).Text(
                                "This statement summarizes your tax-deductible contributions. " +
                                "No goods or services were provided in exchange for these contributions. " +
                                "Please retain this statement for your tax records. " +
                                "Consult your tax advisor for information on deductibility."
                            ).FontSize(8).LineHeight(1.3f);
                        });
                    });
                }
            });
        });

        return document.GeneratePdf();
    }
}
