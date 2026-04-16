using Koinon.Application.Common;
using Koinon.Application.DTOs.Giving;
using Koinon.Application.DTOs.Requests;
using Koinon.Application.Interfaces;
using Koinon.Domain.Data;
using Koinon.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Koinon.Application.Services;

/// <summary>
/// Service implementation for fund administration.
/// </summary>
public class FundService(
    IApplicationDbContext context,
    ILogger<FundService> logger) : IFundService
{
    public async Task<IReadOnlyList<FundAdminDto>> GetAllFundsAsync(CancellationToken ct = default)
    {
        var funds = await context.Funds
            .AsNoTracking()
            .OrderBy(f => f.Order)
            .ThenBy(f => f.Name)
            .Select(f => new FundAdminDto
            {
                IdKey = f.IdKey,
                Name = f.Name,
                PublicName = f.PublicName,
                Description = f.Description,
                GlCode = f.GlCode,
                IsActive = f.IsActive,
                IsPublic = f.IsPublic,
                IsTaxDeductible = f.IsTaxDeductible,
                Order = f.Order,
                CreatedDateTime = f.CreatedDateTime,
                ModifiedDateTime = f.ModifiedDateTime
            })
            .ToListAsync(ct);

        return funds;
    }

    public async Task<Result<FundAdminDto>> GetFundAdminAsync(string idKey, CancellationToken ct = default)
    {
        if (!IdKeyHelper.TryDecode(idKey, out var id))
        {
            return Result<FundAdminDto>.Failure(Error.NotFound("Fund", idKey));
        }

        var entity = await context.Funds
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == id, ct);

        if (entity == null)
        {
            return Result<FundAdminDto>.Failure(Error.NotFound("Fund", idKey));
        }

        return Result<FundAdminDto>.Success(MapToAdminDto(entity));
    }

    public async Task<Result<FundAdminDto>> CreateFundAsync(
        CreateFundRequest request,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return Result<FundAdminDto>.Failure(Error.Validation("Fund name is required."));
        }

        var entity = new Fund
        {
            Name = request.Name.Trim(),
            PublicName = request.PublicName?.Trim(),
            Description = request.Description?.Trim(),
            GlCode = request.GlCode?.Trim(),
            IsPublic = request.IsPublic,
            IsTaxDeductible = request.IsTaxDeductible,
            IsActive = true,
            CreatedDateTime = DateTime.UtcNow
        };

        context.Funds.Add(entity);
        await context.SaveChangesAsync(ct);

        logger.LogInformation(
            "Fund created: IdKey={IdKey}, Name={Name}",
            entity.IdKey, entity.Name);

        return Result<FundAdminDto>.Success(MapToAdminDto(entity));
    }

    public async Task<Result<FundAdminDto>> UpdateFundAsync(
        string idKey,
        UpdateFundRequest request,
        CancellationToken ct = default)
    {
        if (!IdKeyHelper.TryDecode(idKey, out var id))
        {
            return Result<FundAdminDto>.Failure(Error.NotFound("Fund", idKey));
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return Result<FundAdminDto>.Failure(Error.Validation("Fund name is required."));
        }

        var exists = await context.Funds.AnyAsync(f => f.Id == id, ct);
        if (!exists)
        {
            return Result<FundAdminDto>.Failure(Error.NotFound("Fund", idKey));
        }

        var name = request.Name.Trim();
        var publicName = request.PublicName?.Trim();
        var description = request.Description?.Trim();
        var glCode = request.GlCode?.Trim();
        var isPublic = request.IsPublic;
        var isTaxDeductible = request.IsTaxDeductible;
        var modifiedAt = DateTime.UtcNow;

        await context.Funds
            .Where(f => f.Id == id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(f => f.Name, name)
                .SetProperty(f => f.PublicName, publicName)
                .SetProperty(f => f.Description, description)
                .SetProperty(f => f.GlCode, glCode)
                .SetProperty(f => f.IsPublic, isPublic)
                .SetProperty(f => f.IsTaxDeductible, isTaxDeductible)
                .SetProperty(f => f.ModifiedDateTime, modifiedAt),
                ct);

        logger.LogInformation("Fund updated: IdKey={IdKey}, Name={Name}", idKey, name);

        var updated = await context.Funds.AsNoTracking()
            .FirstAsync(f => f.Id == id, ct);

        return Result<FundAdminDto>.Success(MapToAdminDto(updated));
    }

    public async Task<Result> DeactivateFundAsync(string idKey, CancellationToken ct = default)
    {
        if (!IdKeyHelper.TryDecode(idKey, out var id))
        {
            return Result.Failure(Error.NotFound("Fund", idKey));
        }

        var exists = await context.Funds.AnyAsync(f => f.Id == id, ct);
        if (!exists)
        {
            return Result.Failure(Error.NotFound("Fund", idKey));
        }

        await context.Funds
            .Where(f => f.Id == id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(f => f.IsActive, false)
                .SetProperty(f => f.ModifiedDateTime, DateTime.UtcNow),
                ct);

        logger.LogInformation("Fund deactivated: IdKey={IdKey}", idKey);

        return Result.Success();
    }

    private static FundAdminDto MapToAdminDto(Fund fund) =>
        new()
        {
            IdKey = fund.IdKey,
            Name = fund.Name,
            PublicName = fund.PublicName,
            Description = fund.Description,
            GlCode = fund.GlCode,
            IsActive = fund.IsActive,
            IsPublic = fund.IsPublic,
            IsTaxDeductible = fund.IsTaxDeductible,
            Order = fund.Order,
            CreatedDateTime = fund.CreatedDateTime,
            ModifiedDateTime = fund.ModifiedDateTime
        };
}
