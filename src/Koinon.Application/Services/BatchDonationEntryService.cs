using System.Text.Json;
using FluentValidation;
using Koinon.Application.Common;
using Koinon.Application.DTOs.Giving;
using Koinon.Application.DTOs.Requests;
using Koinon.Application.Interfaces;
using Koinon.Domain.Data;
using Koinon.Domain.Entities;
using Koinon.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Koinon.Application.Services;

/// <summary>
/// Service for manual contribution entry with batch reconciliation.
/// </summary>
public class BatchDonationEntryService(
    IApplicationDbContext context,
    IUserContext userContext,
    IValidator<CreateBatchRequest> createBatchValidator,
    IValidator<AddContributionRequest> addContributionValidator,
    IValidator<UpdateContributionRequest> updateContributionValidator,
    ILogger<BatchDonationEntryService> logger) : IBatchDonationEntryService
{
    private const string ManualEntrySourceName = "Manual Entry";
    private const string TransactionSourceTypeName = "Transaction Source";

    public async Task<Result<ContributionBatchDto>> CreateBatchAsync(
        CreateBatchRequest request,
        CancellationToken ct = default)
    {
        // Validate
        var validation = await createBatchValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return Result<ContributionBatchDto>.Failure(Error.FromFluentValidation(validation));
        }

        // Decode campus if provided
        int? campusId = null;
        if (!string.IsNullOrWhiteSpace(request.CampusIdKey))
        {
            if (!IdKeyHelper.TryDecode(request.CampusIdKey, out int decodedCampusId))
            {
                return Result<ContributionBatchDto>.Failure(
                    Error.NotFound("Campus", request.CampusIdKey));
            }

            var campusExists = await context.Campuses.AnyAsync(c => c.Id == decodedCampusId, ct);
            if (!campusExists)
            {
                return Result<ContributionBatchDto>.Failure(
                    Error.NotFound("Campus", request.CampusIdKey));
            }

            campusId = decodedCampusId;
        }

        // Verify authentication before any database changes
        EnsureAuthenticated();

        // Create batch
        var batch = new ContributionBatch
        {
            Name = request.Name,
            BatchDate = request.BatchDate,
            Status = BatchStatus.Open,
            ControlAmount = request.ControlAmount,
            ControlItemCount = request.ControlItemCount,
            CampusId = campusId,
            Note = request.Note,
            CreatedDateTime = DateTime.UtcNow
        };

        await context.ContributionBatches.AddAsync(batch, ct);
        await context.SaveChangesAsync(ct);

        await LogAuditAsync(FinancialAuditAction.Create, "ContributionBatch", batch.IdKey, new { request.Name }, ct);
        await context.SaveChangesAsync(ct);

        logger.LogInformation("Created contribution batch {BatchId}: {Name}", batch.Id, batch.Name);

        return Result<ContributionBatchDto>.Success(MapToBatchDto(batch));
    }

    public async Task<Result<ContributionBatchDto>> GetBatchAsync(
        string batchIdKey,
        CancellationToken ct = default)
    {
        if (!IdKeyHelper.TryDecode(batchIdKey, out int batchId))
        {
            return Result<ContributionBatchDto>.Failure(Error.NotFound("ContributionBatch", batchIdKey));
        }

        var batch = await context.ContributionBatches
            .AsNoTracking()
            .Include(b => b.Campus)
            .FirstOrDefaultAsync(b => b.Id == batchId, ct);

        if (batch is null)
        {
            return Result<ContributionBatchDto>.Failure(Error.NotFound("ContributionBatch", batchIdKey));
        }

        return Result<ContributionBatchDto>.Success(MapToBatchDto(batch));
    }

    public async Task<Result<BatchSummaryDto>> GetBatchSummaryAsync(
        string batchIdKey,
        CancellationToken ct = default)
    {
        if (!IdKeyHelper.TryDecode(batchIdKey, out int batchId))
        {
            return Result<BatchSummaryDto>.Failure(Error.NotFound("ContributionBatch", batchIdKey));
        }

        var batch = await context.ContributionBatches
            .AsNoTracking()
            .Include(b => b.Contributions)
                .ThenInclude(c => c.ContributionDetails)
            .FirstOrDefaultAsync(b => b.Id == batchId, ct);

        if (batch is null)
        {
            return Result<BatchSummaryDto>.Failure(Error.NotFound("ContributionBatch", batchIdKey));
        }

        var actualAmount = batch.Contributions
            .SelectMany(c => c.ContributionDetails)
            .Sum(cd => cd.Amount);

        var contributionCount = batch.Contributions.Count;
        var variance = (batch.ControlAmount ?? 0) - actualAmount;
        var itemCountVariance = batch.ControlItemCount.HasValue
            ? batch.ControlItemCount.Value - contributionCount
            : (int?)null;

        var summary = new BatchSummaryDto
        {
            IdKey = batch.IdKey,
            Name = batch.Name,
            Status = batch.Status.ToString(),
            ControlAmount = batch.ControlAmount,
            ControlItemCount = batch.ControlItemCount,
            ActualAmount = actualAmount,
            ContributionCount = contributionCount,
            ItemCountVariance = itemCountVariance,
            Variance = variance,
            IsBalanced = variance == 0 && (itemCountVariance is null || itemCountVariance == 0)
        };

        return Result<BatchSummaryDto>.Success(summary);
    }

    public async Task<Result> OpenBatchAsync(string batchIdKey, CancellationToken ct = default)
    {
        if (!IdKeyHelper.TryDecode(batchIdKey, out int batchId))
        {
            return Result.Failure(Error.NotFound("ContributionBatch", batchIdKey));
        }

        var batch = await context.ContributionBatches.FindAsync(new object[] { batchId }, ct);
        if (batch is null)
        {
            return Result.Failure(Error.NotFound("ContributionBatch", batchIdKey));
        }

        // No-op if already open
        if (batch.Status == BatchStatus.Open)
        {
            return Result.Success();
        }

        // Cannot reopen closed or posted batches
        if (batch.Status == BatchStatus.Closed || batch.Status == BatchStatus.Posted)
        {
            return Result.Failure(Error.UnprocessableEntity(
                $"Cannot open a batch that is {batch.Status}. Closed and Posted batches cannot be reopened."));
        }

        // Verify authentication before any database changes
        EnsureAuthenticated();

        var previousStatus = batch.Status;
        batch.Status = BatchStatus.Open;
        batch.ModifiedDateTime = DateTime.UtcNow;

        await LogAuditAsync(FinancialAuditAction.Update, "ContributionBatch", batch.IdKey,
            new { Action = "Open", PreviousStatus = previousStatus.ToString(), NewStatus = "Open" }, ct);
        await context.SaveChangesAsync(ct);

        logger.LogInformation("Opened contribution batch {BatchId}: {Name}", batch.Id, batch.Name);

        return Result.Success();
    }

    public async Task<Result> CloseBatchAsync(string batchIdKey, CancellationToken ct = default)
    {
        if (!IdKeyHelper.TryDecode(batchIdKey, out int batchId))
        {
            return Result.Failure(Error.NotFound("ContributionBatch", batchIdKey));
        }

        var batch = await context.ContributionBatches.FindAsync(new object[] { batchId }, ct);
        if (batch is null)
        {
            return Result.Failure(Error.NotFound("ContributionBatch", batchIdKey));
        }

        // Can only close open batches
        if (batch.Status != BatchStatus.Open)
        {
            return Result.Failure(Error.UnprocessableEntity(
                $"Can only close Open batches. Current status: {batch.Status}"));
        }

        // Verify authentication before any database changes
        EnsureAuthenticated();

        batch.Status = BatchStatus.Closed;
        batch.ModifiedDateTime = DateTime.UtcNow;

        await LogAuditAsync(FinancialAuditAction.Update, "ContributionBatch", batch.IdKey,
            new { Action = "Close", PreviousStatus = "Open", NewStatus = "Closed" }, ct);
        await context.SaveChangesAsync(ct);

        logger.LogInformation("Closed contribution batch {BatchId}: {Name}", batch.Id, batch.Name);

        return Result.Success();
    }

    public async Task<Result<ContributionDto>> AddContributionAsync(
        string batchIdKey,
        AddContributionRequest request,
        CancellationToken ct = default)
    {
        // Validate
        var validation = await addContributionValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return Result<ContributionDto>.Failure(Error.FromFluentValidation(validation));
        }

        // Get batch
        if (!IdKeyHelper.TryDecode(batchIdKey, out int batchId))
        {
            return Result<ContributionDto>.Failure(Error.NotFound("ContributionBatch", batchIdKey));
        }

        var batch = await context.ContributionBatches
            .FirstOrDefaultAsync(b => b.Id == batchId, ct);

        if (batch is null)
        {
            return Result<ContributionDto>.Failure(Error.NotFound("ContributionBatch", batchIdKey));
        }

        // Only allow adding to open batches
        if (batch.Status != BatchStatus.Open)
        {
            return Result<ContributionDto>.Failure(Error.UnprocessableEntity(
                $"Can only add contributions to Open batches. Current status: {batch.Status}"));
        }

        // Verify authentication before any database changes
        EnsureAuthenticated();

        // Resolve person if provided
        int? personAliasId = null;
        if (!string.IsNullOrWhiteSpace(request.PersonIdKey))
        {
            if (!IdKeyHelper.TryDecode(request.PersonIdKey, out int personId))
            {
                return Result<ContributionDto>.Failure(Error.NotFound("Person", request.PersonIdKey));
            }

            // Get primary PersonAlias for this person
            var personAlias = await context.PersonAliases
                .FirstOrDefaultAsync(pa => pa.PersonId == personId, ct);

            if (personAlias is null)
            {
                return Result<ContributionDto>.Failure(Error.NotFound("Person", request.PersonIdKey));
            }

            personAliasId = personAlias.Id;
        }

        // Resolve transaction type
        if (!IdKeyHelper.TryDecode(request.TransactionTypeValueIdKey, out int transactionTypeValueId))
        {
            return Result<ContributionDto>.Failure(
                Error.NotFound("TransactionType", request.TransactionTypeValueIdKey));
        }

        var transactionTypeExists = await context.DefinedValues.AnyAsync(dv => dv.Id == transactionTypeValueId, ct);
        if (!transactionTypeExists)
        {
            return Result<ContributionDto>.Failure(
                Error.NotFound("TransactionType", request.TransactionTypeValueIdKey));
        }

        // Get "Manual Entry" source type
        var sourceTypeValue = await context.DefinedValues
            .Include(dv => dv.DefinedType)
            .FirstOrDefaultAsync(dv =>
                dv.DefinedType != null &&
                dv.DefinedType.Name == TransactionSourceTypeName &&
                dv.Value == ManualEntrySourceName, ct);

        if (sourceTypeValue is null)
        {
            return Result<ContributionDto>.Failure(Error.UnprocessableEntity(
                $"'{ManualEntrySourceName}' source type not found. Please ensure the '{TransactionSourceTypeName}' defined type has a '{ManualEntrySourceName}' value."));
        }

        // Validate and resolve funds - batch load to avoid N+1
        var fundIdKeys = request.Details.Select(d => d.FundIdKey).ToList();
        var fundIdList = new List<int>();
        foreach (var fundIdKey in fundIdKeys)
        {
            if (!IdKeyHelper.TryDecode(fundIdKey, out int fundId))
            {
                return Result<ContributionDto>.Failure(Error.NotFound("Fund", fundIdKey));
            }
            fundIdList.Add(fundId);
        }

        var funds = await context.Funds
            .Where(f => fundIdList.Contains(f.Id))
            .ToDictionaryAsync(f => f.Id, ct);

        var contributionDetails = new List<ContributionDetail>();
        foreach (var detail in request.Details)
        {
            var fundId = IdKeyHelper.Decode(detail.FundIdKey);

            if (!funds.TryGetValue(fundId, out var fund))
            {
                return Result<ContributionDto>.Failure(Error.NotFound("Fund", detail.FundIdKey));
            }

            if (!fund.IsActive)
            {
                return Result<ContributionDto>.Failure(Error.UnprocessableEntity(
                    $"Fund '{fund.Name}' is not active and cannot receive contributions."));
            }

            contributionDetails.Add(new ContributionDetail
            {
                ContributionId = 0, // Will be set after contribution is saved
                FundId = fundId,
                Amount = detail.Amount,
                Summary = detail.Summary,
                CreatedDateTime = DateTime.UtcNow
            });
        }

        // Create contribution
        var contribution = new Contribution
        {
            PersonAliasId = personAliasId,
            BatchId = batchId,
            TransactionDateTime = request.TransactionDateTime,
            TransactionCode = request.TransactionCode,
            TransactionTypeValueId = transactionTypeValueId,
            SourceTypeValueId = sourceTypeValue.Id,
            Summary = request.Summary,
            CampusId = batch.CampusId,
            CreatedDateTime = DateTime.UtcNow
        };

        await context.Contributions.AddAsync(contribution, ct);
        await context.SaveChangesAsync(ct);

        // Add details with contribution ID
        foreach (var detail in contributionDetails)
        {
            detail.ContributionId = contribution.Id;
        }

        await context.ContributionDetails.AddRangeAsync(contributionDetails, ct);
        await LogAuditAsync(FinancialAuditAction.Create, "Contribution", contribution.IdKey,
            new { BatchIdKey = batchIdKey, TotalAmount = contributionDetails.Sum(d => d.Amount) }, ct);
        await context.SaveChangesAsync(ct);

        logger.LogInformation("Added contribution {ContributionId} to batch {BatchId}",
            contribution.Id, batchId);

        // Fetch with includes for DTO mapping
        var createdContribution = await context.Contributions
            .AsNoTracking()
            .Include(c => c.PersonAlias)
                .ThenInclude(pa => pa != null ? pa.Person : null)
            .Include(c => c.ContributionDetails)
                .ThenInclude(cd => cd.Fund)
            .FirstOrDefaultAsync(c => c.Id == contribution.Id, ct);

        if (createdContribution is null)
        {
            logger.LogError("Contribution {ContributionId} not found after creation", contribution.Id);
            return Result<ContributionDto>.Failure(Error.UnprocessableEntity("Contribution created but could not be retrieved"));
        }

        return Result<ContributionDto>.Success(MapToContributionDto(createdContribution));
    }

    public async Task<Result<ContributionDto>> UpdateContributionAsync(
        string contributionIdKey,
        UpdateContributionRequest request,
        CancellationToken ct = default)
    {
        // Validate
        var validation = await updateContributionValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return Result<ContributionDto>.Failure(Error.FromFluentValidation(validation));
        }

        // Get contribution with batch
        if (!IdKeyHelper.TryDecode(contributionIdKey, out int contributionId))
        {
            return Result<ContributionDto>.Failure(Error.NotFound("Contribution", contributionIdKey));
        }

        var contribution = await context.Contributions
            .Include(c => c.Batch)
            .Include(c => c.ContributionDetails)
            .FirstOrDefaultAsync(c => c.Id == contributionId, ct);

        if (contribution is null)
        {
            return Result<ContributionDto>.Failure(Error.NotFound("Contribution", contributionIdKey));
        }

        // Only allow updating if parent batch is open
        if (contribution.Batch?.Status != BatchStatus.Open)
        {
            return Result<ContributionDto>.Failure(Error.UnprocessableEntity(
                $"Can only update contributions in Open batches. Batch status: {contribution.Batch?.Status}"));
        }

        // Verify authentication before any database changes
        EnsureAuthenticated();

        // Resolve person if provided
        int? personAliasId = null;
        if (!string.IsNullOrWhiteSpace(request.PersonIdKey))
        {
            if (!IdKeyHelper.TryDecode(request.PersonIdKey, out int personId))
            {
                return Result<ContributionDto>.Failure(Error.NotFound("Person", request.PersonIdKey));
            }

            var personAlias = await context.PersonAliases
                .FirstOrDefaultAsync(pa => pa.PersonId == personId, ct);

            if (personAlias is null)
            {
                return Result<ContributionDto>.Failure(Error.NotFound("Person", request.PersonIdKey));
            }

            personAliasId = personAlias.Id;
        }

        // Resolve transaction type
        if (!IdKeyHelper.TryDecode(request.TransactionTypeValueIdKey, out int transactionTypeValueId))
        {
            return Result<ContributionDto>.Failure(
                Error.NotFound("TransactionType", request.TransactionTypeValueIdKey));
        }

        var transactionTypeExists = await context.DefinedValues.AnyAsync(dv => dv.Id == transactionTypeValueId, ct);
        if (!transactionTypeExists)
        {
            return Result<ContributionDto>.Failure(
                Error.NotFound("TransactionType", request.TransactionTypeValueIdKey));
        }

        // Validate and resolve funds - batch load to avoid N+1
        var updateFundIdKeys = request.Details.Select(d => d.FundIdKey).ToList();
        var updateFundIdList = new List<int>();
        foreach (var fundIdKey in updateFundIdKeys)
        {
            if (!IdKeyHelper.TryDecode(fundIdKey, out int fundId))
            {
                return Result<ContributionDto>.Failure(Error.NotFound("Fund", fundIdKey));
            }
            updateFundIdList.Add(fundId);
        }

        var updateFunds = await context.Funds
            .Where(f => updateFundIdList.Contains(f.Id))
            .ToDictionaryAsync(f => f.Id, ct);

        var newDetails = new List<ContributionDetail>();
        foreach (var detail in request.Details)
        {
            var fundId = IdKeyHelper.Decode(detail.FundIdKey);

            if (!updateFunds.TryGetValue(fundId, out var fund))
            {
                return Result<ContributionDto>.Failure(Error.NotFound("Fund", detail.FundIdKey));
            }

            if (!fund.IsActive)
            {
                return Result<ContributionDto>.Failure(Error.UnprocessableEntity(
                    $"Fund '{fund.Name}' is not active and cannot receive contributions."));
            }

            newDetails.Add(new ContributionDetail
            {
                ContributionId = contribution.Id,
                FundId = fundId,
                Amount = detail.Amount,
                Summary = detail.Summary,
                CreatedDateTime = DateTime.UtcNow
            });
        }

        // Track old values for audit
        var oldAmount = contribution.ContributionDetails.Sum(d => d.Amount);

        // Remove old details
        context.ContributionDetails.RemoveRange(contribution.ContributionDetails);

        // Update contribution
        contribution.PersonAliasId = personAliasId;
        contribution.TransactionDateTime = request.TransactionDateTime;
        contribution.TransactionCode = request.TransactionCode;
        contribution.TransactionTypeValueId = transactionTypeValueId;
        contribution.Summary = request.Summary;
        contribution.ModifiedDateTime = DateTime.UtcNow;

        // Add new details
        await context.ContributionDetails.AddRangeAsync(newDetails, ct);

        var newAmount = newDetails.Sum(d => d.Amount);
        await LogAuditAsync(FinancialAuditAction.Update, "Contribution", contribution.IdKey,
            new { OldAmount = oldAmount, NewAmount = newAmount }, ct);
        await context.SaveChangesAsync(ct);

        logger.LogInformation("Updated contribution {ContributionId}", contribution.Id);

        // Fetch with includes for DTO mapping
        var updatedContribution = await context.Contributions
            .AsNoTracking()
            .Include(c => c.PersonAlias)
                .ThenInclude(pa => pa != null ? pa.Person : null)
            .Include(c => c.ContributionDetails)
                .ThenInclude(cd => cd.Fund)
            .FirstOrDefaultAsync(c => c.Id == contribution.Id, ct);

        if (updatedContribution is null)
        {
            logger.LogError("Contribution {ContributionId} not found after update", contribution.Id);
            return Result<ContributionDto>.Failure(Error.UnprocessableEntity("Contribution updated but could not be retrieved"));
        }

        return Result<ContributionDto>.Success(MapToContributionDto(updatedContribution));
    }

    public async Task<Result> DeleteContributionAsync(string contributionIdKey, CancellationToken ct = default)
    {
        if (!IdKeyHelper.TryDecode(contributionIdKey, out int contributionId))
        {
            return Result.Failure(Error.NotFound("Contribution", contributionIdKey));
        }

        var contribution = await context.Contributions
            .Include(c => c.Batch)
            .Include(c => c.ContributionDetails)
            .FirstOrDefaultAsync(c => c.Id == contributionId, ct);

        if (contribution is null)
        {
            return Result.Failure(Error.NotFound("Contribution", contributionIdKey));
        }

        // Only allow deleting if parent batch is open
        if (contribution.Batch?.Status != BatchStatus.Open)
        {
            return Result.Failure(Error.UnprocessableEntity(
                $"Can only delete contributions from Open batches. Batch status: {contribution.Batch?.Status}"));
        }

        // Verify authentication before any database changes
        EnsureAuthenticated();

        var amount = contribution.ContributionDetails.Sum(d => d.Amount);

        // Remove details first, then contribution
        context.ContributionDetails.RemoveRange(contribution.ContributionDetails);
        context.Contributions.Remove(contribution);

        await LogAuditAsync(FinancialAuditAction.Delete, "Contribution", contributionIdKey,
            new { Amount = amount, BatchId = contribution.BatchId }, ct);
        await context.SaveChangesAsync(ct);

        logger.LogInformation("Deleted contribution {ContributionId}", contributionId);

        return Result.Success();
    }

    public async Task<IReadOnlyList<PersonLookupDto>> SearchContributorsAsync(
        string searchTerm,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(searchTerm) || searchTerm.Length < 2)
        {
            return Array.Empty<PersonLookupDto>();
        }

        var searchLower = searchTerm.ToLower();

        var people = await context.People
            .AsNoTracking()
            .Where(p =>
                p.FirstName.ToLower().Contains(searchLower) ||
                p.LastName.ToLower().Contains(searchLower) ||
                (p.Email != null && p.Email.ToLower().Contains(searchLower)))
            .OrderBy(p => p.LastName)
            .ThenBy(p => p.FirstName)
            .Take(20)
            .ToListAsync(ct);

        return people.Select(p => new PersonLookupDto
        {
            IdKey = p.IdKey,
            FullName = p.FullName,
            Email = p.Email
        }).ToList();
    }

    public async Task<IReadOnlyList<FundDto>> GetActiveFundsAsync(CancellationToken ct = default)
    {
        var funds = await context.Funds
            .AsNoTracking()
            .Where(f => f.IsActive)
            .OrderBy(f => f.Order)
            .ThenBy(f => f.Name)
            .Select(f => new FundDto
            {
                IdKey = f.IdKey,
                Name = f.Name,
                PublicName = f.PublicName,
                IsActive = f.IsActive,
                IsPublic = f.IsPublic
            })
            .ToListAsync(ct);

        return funds;
    }

    private ContributionBatchDto MapToBatchDto(ContributionBatch batch)
    {
        return new ContributionBatchDto
        {
            IdKey = batch.IdKey,
            Name = batch.Name,
            BatchDate = batch.BatchDate,
            Status = batch.Status.ToString(),
            ControlAmount = batch.ControlAmount,
            ControlItemCount = batch.ControlItemCount,
            CampusIdKey = batch.Campus?.IdKey,
            Note = batch.Note,
            CreatedDateTime = batch.CreatedDateTime,
            ModifiedDateTime = batch.ModifiedDateTime
        };
    }

    private ContributionDto MapToContributionDto(Contribution contribution)
    {
        return new ContributionDto
        {
            IdKey = contribution.IdKey,
            PersonIdKey = contribution.PersonAlias?.Person?.IdKey,
            PersonName = contribution.PersonAlias?.Person?.FullName,
            BatchIdKey = contribution.Batch?.IdKey,
            TransactionDateTime = contribution.TransactionDateTime,
            TransactionCode = contribution.TransactionCode,
            TransactionTypeValueIdKey = IdKeyHelper.Encode(contribution.TransactionTypeValueId),
            SourceTypeValueIdKey = IdKeyHelper.Encode(contribution.SourceTypeValueId),
            Summary = contribution.Summary,
            CampusIdKey = contribution.Campus?.IdKey,
            Details = contribution.ContributionDetails
                .Where(cd => cd.Fund != null)
                .Select(cd => new ContributionDetailDto
                {
                    IdKey = cd.IdKey,
                    FundIdKey = cd.Fund!.IdKey,
                    FundName = cd.Fund.Name,
                    Amount = cd.Amount,
                    Summary = cd.Summary
                }).ToList(),
            TotalAmount = contribution.ContributionDetails.Sum(cd => cd.Amount)
        };
    }

    /// <summary>
    /// Ensures user is authenticated before any financial operation.
    /// Must be called at the START of any mutation method, BEFORE any database changes.
    /// </summary>
    private void EnsureAuthenticated()
    {
        if (!userContext.IsAuthenticated || !userContext.CurrentPersonId.HasValue)
        {
            logger.LogWarning("Unauthenticated user attempted financial operation");
            throw new UnauthorizedAccessException(
                "Financial operations require authentication. Please log in to continue.");
        }
    }

    private async Task LogAuditAsync(
        FinancialAuditAction action,
        string entityType,
        string entityIdKey,
        object? details = null,
        CancellationToken ct = default)
    {
        // This should never happen if EnsureAuthenticated was called first
        if (!userContext.CurrentPersonId.HasValue)
        {
            logger.LogError(
                "Financial audit failed - no authenticated user for {Action} on {EntityType}/{EntityIdKey}",
                action, entityType, entityIdKey);
            throw new UnauthorizedAccessException(
                $"Financial operations require authentication. Cannot perform {action} on {entityType}/{entityIdKey} without authenticated user.");
        }

        var auditLog = new FinancialAuditLog
        {
            PersonId = userContext.CurrentPersonId.Value,
            ActionType = action,
            EntityType = entityType,
            EntityIdKey = entityIdKey,
            IpAddress = null, // TODO(#260): Inject IHttpContextAccessor for forensic data
            UserAgent = null, // TODO(#260): Inject IHttpContextAccessor for forensic data
            Details = details != null ? JsonSerializer.Serialize(details) : null,
            Timestamp = DateTime.UtcNow
        };

        await context.FinancialAuditLogs.AddAsync(auditLog, ct);
    }
}
