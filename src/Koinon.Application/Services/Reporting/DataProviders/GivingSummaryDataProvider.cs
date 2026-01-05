using System.Text.Json;
using Koinon.Application.Interfaces;
using Koinon.Application.Interfaces.Reporting;
using Koinon.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Koinon.Application.Services.Reporting.DataProviders;

/// <summary>
/// Provides data for giving summary reports.
/// Retrieves contribution data grouped by person and fund.
/// </summary>
public class GivingSummaryDataProvider(
    IApplicationDbContext context,
    ILogger<GivingSummaryDataProvider> logger) : IReportDataProvider
{
    public ReportType ReportType => ReportType.GivingSummary;

    public async Task<IReadOnlyList<Dictionary<string, object?>>> GetDataAsync(
        string parametersJson,
        CancellationToken ct = default)
    {
        // Parse parameters
        var options = ParseParameters(parametersJson);

        logger.LogInformation(
            "Querying giving summary data with filters: StartDate={StartDate}, EndDate={EndDate}, FundId={FundId}",
            options.StartDate,
            options.EndDate,
            options.FundId);

        // Query contribution details with includes
        var query = context.ContributionDetails
            .Include(cd => cd.Contribution)
                .ThenInclude(c => c!.PersonAlias)
                    .ThenInclude(pa => pa!.Person)
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

        var details = await query.ToListAsync(ct);

        // Group by person and fund
        var groupedData = details
            .Where(cd => cd.Contribution?.PersonAlias?.Person != null && cd.Fund != null)
            .GroupBy(cd => new
            {
                PersonId = cd.Contribution!.PersonAlias!.PersonId,
                PersonName = $"{cd.Contribution.PersonAlias.Person!.FirstName} {cd.Contribution.PersonAlias.Person.LastName}",
                FundId = cd.FundId,
                FundName = cd.Fund!.Name
            })
            .Select(g => new
            {
                g.Key.PersonName,
                g.Key.FundName,
                TotalAmount = g.Sum(cd => cd.Amount),
                ContributionCount = g.Select(cd => cd.ContributionId).Distinct().Count(),
                FirstGiftDate = g.Min(cd => cd.Contribution!.TransactionDateTime),
                LastGiftDate = g.Max(cd => cd.Contribution!.TransactionDateTime)
            })
            .OrderBy(x => x.PersonName)
            .ThenBy(x => x.FundName)
            .ToList();

        logger.LogInformation("Retrieved {Count} giving summary records", groupedData.Count);

        // Convert to dictionary format
        var result = groupedData.Select(row => new Dictionary<string, object?>
        {
            ["PersonName"] = row.PersonName,
            ["FundName"] = row.FundName,
            ["TotalAmount"] = row.TotalAmount,
            ["ContributionCount"] = row.ContributionCount,
            ["FirstGiftDate"] = row.FirstGiftDate,
            ["LastGiftDate"] = row.LastGiftDate
        }).ToList();

        return result.AsReadOnly();
    }

    private GivingReportOptions ParseParameters(string parametersJson)
    {
        if (string.IsNullOrWhiteSpace(parametersJson))
        {
            return new GivingReportOptions();
        }

        try
        {
            return JsonSerializer.Deserialize<GivingReportOptions>(parametersJson)
                   ?? new GivingReportOptions();
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Failed to parse giving report parameters, using defaults");
            return new GivingReportOptions();
        }
    }

    private class GivingReportOptions
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int? FundId { get; set; }
    }
}
