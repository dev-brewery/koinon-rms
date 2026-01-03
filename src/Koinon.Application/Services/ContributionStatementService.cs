using Koinon.Application.Common;
using Koinon.Application.DTOs.Giving;
using Koinon.Application.Interfaces;
using Koinon.Domain.Data;
using Koinon.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Koinon.Application.Services;

/// <summary>
/// Service for generating contribution statements with PDF output.
/// </summary>
public class ContributionStatementService(
    IApplicationDbContext context,
    IConfiguration configuration,
    StatementPdfGenerator pdfGenerator,
    ILogger<ContributionStatementService> logger) : IContributionStatementService
{
    public async Task<Result<PagedResult<ContributionStatementDto>>> GetStatementsAsync(
        int page = 1,
        int pageSize = 25,
        CancellationToken ct = default)
    {
        if (page < 1 || pageSize < 1 || pageSize > 100)
        {
            return Result<PagedResult<ContributionStatementDto>>.Failure(
                Error.Validation("Page must be >= 1 and pageSize must be between 1 and 100"));
        }

        var query = context.ContributionStatements
            .AsNoTracking()
            .Include(s => s.Person)
            .OrderByDescending(s => s.GeneratedDateTime);

        var totalCount = await query.CountAsync(ct);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var statements = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new ContributionStatementDto
            {
                IdKey = s.IdKey,
                PersonIdKey = s.Person.IdKey,
                PersonName = s.Person.FullName ?? "Unknown",
                StartDate = s.StartDate,
                EndDate = s.EndDate,
                TotalAmount = s.TotalAmount,
                ContributionCount = s.ContributionCount,
                GeneratedDateTime = s.GeneratedDateTime
            })
            .ToListAsync(ct);

        var pagedResult = new PagedResult<ContributionStatementDto>(
            statements,
            totalCount,
            page,
            pageSize);

        return Result<PagedResult<ContributionStatementDto>>.Success(pagedResult);
    }

    public async Task<Result<ContributionStatementDto>> GetStatementAsync(
        string idKey,
        CancellationToken ct = default)
    {
        if (!IdKeyHelper.TryDecode(idKey, out int id))
        {
            return Result<ContributionStatementDto>.Failure(
                Error.NotFound("ContributionStatement", idKey));
        }

        var statement = await context.ContributionStatements
            .AsNoTracking()
            .Include(s => s.Person)
            .FirstOrDefaultAsync(s => s.Id == id, ct);

        if (statement == null)
        {
            return Result<ContributionStatementDto>.Failure(
                Error.NotFound("ContributionStatement", idKey));
        }

        var dto = new ContributionStatementDto
        {
            IdKey = statement.IdKey,
            PersonIdKey = statement.Person.IdKey,
            PersonName = statement.Person.FullName ?? "Unknown",
            StartDate = statement.StartDate,
            EndDate = statement.EndDate,
            TotalAmount = statement.TotalAmount,
            ContributionCount = statement.ContributionCount,
            GeneratedDateTime = statement.GeneratedDateTime
        };

        return Result<ContributionStatementDto>.Success(dto);
    }

    public async Task<Result<StatementPreviewDto>> PreviewStatementAsync(
        GenerateStatementRequest request,
        CancellationToken ct = default)
    {
        // Decode person IdKey
        if (!IdKeyHelper.TryDecode(request.PersonIdKey, out int personId))
        {
            return Result<StatementPreviewDto>.Failure(
                Error.NotFound("Person", request.PersonIdKey));
        }

        // Validate date range
        if (request.StartDate > request.EndDate)
        {
            return Result<StatementPreviewDto>.Failure(
                Error.Validation("StartDate must be before or equal to EndDate"));
        }

        // Get person with location info
        var person = await context.People
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == personId, ct);

        if (person == null)
        {
            return Result<StatementPreviewDto>.Failure(
                Error.NotFound("Person", request.PersonIdKey));
        }

        // For now, use a placeholder for address until we add GroupLocation support
        string personAddress = $"{person.FullName}\nAddress on file";

        // Get church info from configuration
        var churchName = configuration["Church:Name"] ?? "Church";
        var churchStreet1 = configuration["Church:Address:Street1"];
        var churchStreet2 = configuration["Church:Address:Street2"];
        var churchCity = configuration["Church:Address:City"];
        var churchState = configuration["Church:Address:State"];
        var churchPostalCode = configuration["Church:Address:PostalCode"];
        var churchAddress = FormatAddress(churchStreet1, churchStreet2, churchCity, churchState, churchPostalCode);

        // Get contributions for the period
        var contributions = await GetContributionsForPeriodAsync(personId, request.StartDate, request.EndDate, ct);

        var totalAmount = contributions.Sum(c => c.Amount);

        var preview = new StatementPreviewDto
        {
            PersonIdKey = person.IdKey,
            PersonName = person.FullName ?? "Unknown",
            PersonAddress = personAddress,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            TotalAmount = totalAmount,
            Contributions = contributions,
            ChurchName = churchName,
            ChurchAddress = churchAddress
        };

        return Result<StatementPreviewDto>.Success(preview);
    }

    public async Task<Result<ContributionStatementDto>> GenerateStatementAsync(
        GenerateStatementRequest request,
        CancellationToken ct = default)
    {
        // Get preview first (includes all validation)
        var previewResult = await PreviewStatementAsync(request, ct);
        if (!previewResult.IsSuccess)
        {
            return Result<ContributionStatementDto>.Failure(previewResult.Error!);
        }

        var preview = previewResult.Value!;

        // Decode person ID
        if (!IdKeyHelper.TryDecode(request.PersonIdKey, out int personId))
        {
            return Result<ContributionStatementDto>.Failure(
                Error.NotFound("Person", request.PersonIdKey));
        }

        // Create statement entity
        var statement = new ContributionStatement
        {
            PersonId = personId,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            TotalAmount = preview.TotalAmount,
            ContributionCount = preview.Contributions.Count,
            GeneratedDateTime = DateTime.UtcNow
        };

        context.ContributionStatements.Add(statement);
        await context.SaveChangesAsync(ct);

        logger.LogInformation(
            "Generated contribution statement {StatementId} for person {PersonId} ({PersonName}). Period: {StartDate} - {EndDate}, Total: ${TotalAmount}",
            statement.IdKey,
            request.PersonIdKey,
            preview.PersonName,
            request.StartDate,
            request.EndDate,
            preview.TotalAmount);

        var dto = new ContributionStatementDto
        {
            IdKey = statement.IdKey,
            PersonIdKey = request.PersonIdKey,
            PersonName = preview.PersonName,
            StartDate = statement.StartDate,
            EndDate = statement.EndDate,
            TotalAmount = statement.TotalAmount,
            ContributionCount = statement.ContributionCount,
            GeneratedDateTime = statement.GeneratedDateTime
        };

        return Result<ContributionStatementDto>.Success(dto);
    }

    public async Task<Result<byte[]>> GenerateStatementPdfAsync(
        string statementIdKey,
        CancellationToken ct = default)
    {
        if (!IdKeyHelper.TryDecode(statementIdKey, out int statementId))
        {
            return Result<byte[]>.Failure(
                Error.NotFound("ContributionStatement", statementIdKey));
        }

        var statement = await context.ContributionStatements
            .AsNoTracking()
            .Include(s => s.Person)
            .FirstOrDefaultAsync(s => s.Id == statementId, ct);

        if (statement == null)
        {
            return Result<byte[]>.Failure(
                Error.NotFound("ContributionStatement", statementIdKey));
        }

        // Recreate preview for PDF generation
        var request = new GenerateStatementRequest
        {
            PersonIdKey = statement.Person.IdKey,
            StartDate = statement.StartDate,
            EndDate = statement.EndDate
        };

        var previewResult = await PreviewStatementAsync(request, ct);
        if (!previewResult.IsSuccess)
        {
            return Result<byte[]>.Failure(previewResult.Error!);
        }

        var preview = previewResult.Value!;

        try
        {
            var pdfBytes = pdfGenerator.GeneratePdf(preview);
            logger.LogInformation("Generated PDF for statement {StatementIdKey}", statementIdKey);
            return Result<byte[]>.Success(pdfBytes);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to generate PDF for statement {StatementIdKey}", statementIdKey);
            return Result<byte[]>.Failure(
                Error.Internal("Failed to generate PDF", ex.Message));
        }
    }

    public async Task<Result<List<EligiblePersonDto>>> GetEligiblePeopleAsync(
        BatchStatementRequest request,
        CancellationToken ct = default)
    {
        if (request.StartDate > request.EndDate)
        {
            return Result<List<EligiblePersonDto>>.Failure(
                Error.Validation("StartDate must be before or equal to EndDate"));
        }

        // Query contributions grouped by person
        var eligiblePeople = await (from c in context.Contributions
                .AsNoTracking()
                .Where(c => c.PersonAliasId != null)
                .Where(c => c.TransactionDateTime >= request.StartDate && c.TransactionDateTime <= request.EndDate)
                                    from pa in context.PersonAliases.Where(pa => pa.Id == c.PersonAliasId)
                                    from p in context.People.Where(p => p.Id == pa.PersonId)
                                    from cd in context.ContributionDetails.Where(cd => cd.ContributionId == c.Id)
                                    from f in context.Funds.Where(f => f.Id == cd.FundId && f.IsTaxDeductible)
                                    group cd.Amount by new { p.Id, p.FullName } into g
                                    where g.Sum() >= request.MinimumAmount
                                    select new
                                    {
                                        PersonId = g.Key.Id,
                                        PersonName = g.Key.FullName,
                                        TotalAmount = g.Sum(),
                                        ContributionCount = g.Count()
                                    })
            .OrderBy(x => x.PersonName)
            .ToListAsync(ct);

        var result = eligiblePeople.Select(p => new EligiblePersonDto
        {
            PersonIdKey = IdKeyHelper.Encode(p.PersonId),
            PersonName = p.PersonName ?? "Unknown",
            TotalAmount = p.TotalAmount,
            ContributionCount = p.ContributionCount
        }).ToList();

        return Result<List<EligiblePersonDto>>.Success(result);
    }

    private async Task<List<StatementContributionDto>> GetContributionsForPeriodAsync(
        int personId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken ct)
    {
        var personAliasIds = await context.PersonAliases
            .Where(pa => pa.PersonId == personId)
            .Select(pa => pa.Id)
            .ToListAsync(ct);

        var contributions = await (
            from cd in context.ContributionDetails
            join c in context.Contributions on cd.ContributionId equals c.Id
            join f in context.Funds on cd.FundId equals f.Id
            where c.PersonAliasId != null
                && personAliasIds.Contains(c.PersonAliasId.Value)
                && c.TransactionDateTime >= startDate
                && c.TransactionDateTime <= endDate
                && f.IsTaxDeductible
            orderby c.TransactionDateTime
            select new StatementContributionDto
            {
                Date = c.TransactionDateTime,
                FundName = f.PublicName ?? f.Name,
                Amount = cd.Amount,
                CheckNumber = c.TransactionCode
            })
            .ToListAsync(ct);

        return contributions;
    }

    private static string FormatAddress(string? street1, string? street2, string? city, string? state, string? postalCode)
    {
        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(street1))
        {
            parts.Add(street1);
        }

        if (!string.IsNullOrWhiteSpace(street2))
        {
            parts.Add(street2);
        }

        var cityStateZip = new List<string>();
        if (!string.IsNullOrWhiteSpace(city))
        {
            cityStateZip.Add(city);
        }

        if (!string.IsNullOrWhiteSpace(state))
        {
            cityStateZip.Add(state);
        }

        if (!string.IsNullOrWhiteSpace(postalCode))
        {
            cityStateZip.Add(postalCode);
        }

        if (cityStateZip.Any())
        {
            parts.Add(string.Join(", ", cityStateZip));
        }

        return parts.Any() ? string.Join("\n", parts) : "No address available";
    }
}
