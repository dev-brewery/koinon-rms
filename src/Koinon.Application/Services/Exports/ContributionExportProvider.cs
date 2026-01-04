using Koinon.Application.DTOs;
using Koinon.Application.Interfaces;
using Koinon.Domain.Enums;
using Koinon.Domain.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Koinon.Application.Services.Exports;

/// <summary>
/// Provides financial contribution data for export operations.
/// </summary>
public class ContributionExportProvider(
    IApplicationDbContext context,
    ILogger<ContributionExportProvider> logger) : IExportDataProvider
{
    public ExportType ExportType => ExportType.Contributions;

    public List<ExportFieldDto> GetAvailableFields()
    {
        return
        [
            new ExportFieldDto
            {
                FieldName = "IdKey",
                DisplayName = "ID Key",
                DataType = "string",
                Description = "Unique identifier for the contribution",
                IsDefaultField = true,
                IsRequired = true
            },
            new ExportFieldDto
            {
                FieldName = "PersonName",
                DisplayName = "Person Name",
                DataType = "string",
                IsDefaultField = true
            },
            new ExportFieldDto
            {
                FieldName = "Amount",
                DisplayName = "Amount",
                DataType = "number",
                IsDefaultField = true
            },
            new ExportFieldDto
            {
                FieldName = "TransactionDateTime",
                DisplayName = "Transaction Date",
                DataType = "date",
                IsDefaultField = true
            },
            new ExportFieldDto
            {
                FieldName = "FundName",
                DisplayName = "Fund",
                DataType = "string",
                IsDefaultField = true
            },
            new ExportFieldDto
            {
                FieldName = "BatchName",
                DisplayName = "Batch Name",
                DataType = "string",
                IsDefaultField = true
            },
            new ExportFieldDto
            {
                FieldName = "TransactionCode",
                DisplayName = "Transaction Code",
                DataType = "string",
                Description = "Check number or confirmation code",
                IsDefaultField = true
            }
        ];
    }

    public async Task<IReadOnlyList<Dictionary<string, object?>>> GetDataAsync(
        List<string>? fields,
        Dictionary<string, string>? filters,
        CancellationToken ct = default)
    {
        // Determine which fields to include
        var fieldsToInclude = fields ?? GetAvailableFields()
            .Where(f => f.IsDefaultField)
            .Select(f => f.FieldName)
            .ToList();

        logger.LogInformation("Exporting contribution data with fields: {Fields}", string.Join(", ", fieldsToInclude));

        // Build query - we need to query contribution details to get fund information
        var query = context.ContributionDetails
            .Include(cd => cd.Contribution)
                .ThenInclude(c => c!.PersonAlias)
                    .ThenInclude(pa => pa!.Person)
            .Include(cd => cd.Contribution)
                .ThenInclude(c => c!.Batch)
            .Include(cd => cd.Fund)
            .AsNoTracking();

        // Apply filters
        if (filters != null)
        {
            if (filters.TryGetValue("startDate", out var startDateStr) && DateTime.TryParse(startDateStr, out var startDate))
            {
                query = query.Where(cd => cd.Contribution!.TransactionDateTime >= startDate);
            }

            if (filters.TryGetValue("endDate", out var endDateStr) && DateTime.TryParse(endDateStr, out var endDate))
            {
                query = query.Where(cd => cd.Contribution!.TransactionDateTime <= endDate);
            }

            if (filters.TryGetValue("fundIdKey", out var fundIdKey))
            {
                var fundId = IdKeyHelper.Decode(fundIdKey);
                query = query.Where(cd => cd.FundId == fundId);
            }
        }

        // Execute query
        var contributionDetails = await query.ToListAsync(ct);

        logger.LogInformation("Retrieved {Count} contribution details for export", contributionDetails.Count);

        // Map to dictionaries
        var result = new List<Dictionary<string, object?>>();

        foreach (var detail in contributionDetails)
        {
            if (detail.Contribution == null)
            {
                continue;
            }

            var row = new Dictionary<string, object?>();

            foreach (var field in fieldsToInclude)
            {
                row[field] = field switch
                {
                    "IdKey" => IdKeyHelper.Encode(detail.Contribution.Id),
                    "PersonName" => detail.Contribution.PersonAlias?.Person?.FullName ?? "Anonymous",
                    "Amount" => detail.Amount,
                    "TransactionDateTime" => detail.Contribution.TransactionDateTime,
                    "FundName" => detail.Fund?.Name,
                    "BatchName" => detail.Contribution.Batch?.Name,
                    "TransactionCode" => detail.Contribution.TransactionCode,
                    _ => null
                };
            }

            result.Add(row);
        }

        return result;
    }
}
