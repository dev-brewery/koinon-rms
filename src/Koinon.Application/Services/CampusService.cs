using AutoMapper;
using FluentValidation;
using Koinon.Application.Common;
using Koinon.Application.DTOs;
using Koinon.Application.DTOs.Requests;
using Koinon.Application.Interfaces;
using Koinon.Domain.Data;
using Koinon.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Koinon.Application.Services;

/// <summary>
/// Service implementation for managing campus operations.
/// </summary>
public class CampusService(
    IApplicationDbContext context,
    IMapper mapper,
    IValidator<CreateCampusRequest> createValidator,
    IValidator<UpdateCampusRequest> updateValidator,
    ILogger<CampusService> logger) : ICampusService
{
    public async Task<IReadOnlyList<CampusSummaryDto>> GetAllAsync(bool includeInactive = false, CancellationToken ct = default)
    {
        var query = context.Campuses.AsNoTracking();

        if (!includeInactive)
        {
            query = query.Where(c => c.IsActive);
        }

        var entities = await query
            .OrderBy(c => c.Order)
            .ThenBy(c => c.Name)
            .ToListAsync(ct);

        return mapper.Map<IReadOnlyList<CampusSummaryDto>>(entities);
    }

    public async Task<Result<CampusDto>> GetByIdKeyAsync(string idKey, CancellationToken ct = default)
    {
        if (!IdKeyHelper.TryDecode(idKey, out var id))
        {
            return Result<CampusDto>.Failure(Error.NotFound("Campus", idKey));
        }

        var entity = await context.Campuses
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, ct);

        if (entity == null)
        {
            return Result<CampusDto>.Failure(Error.NotFound("Campus", idKey));
        }

        return Result<CampusDto>.Success(mapper.Map<CampusDto>(entity));
    }

    public async Task<Result<CampusDto>> CreateAsync(CreateCampusRequest request, CancellationToken ct = default)
    {
        var validationResult = await createValidator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
        {
            return Result<CampusDto>.Failure(Error.FromFluentValidation(validationResult));
        }

        var entity = mapper.Map<Campus>(request);
        entity.CreatedDateTime = DateTime.UtcNow;

        context.Campuses.Add(entity);
        await context.SaveChangesAsync(ct);

        logger.LogInformation("Campus created successfully: IdKey={IdKey}, Name={Name}", entity.IdKey, entity.Name);

        return Result<CampusDto>.Success(mapper.Map<CampusDto>(entity));
    }

    public async Task<Result<CampusDto>> UpdateAsync(string idKey, UpdateCampusRequest request, CancellationToken ct = default)
    {
        if (!IdKeyHelper.TryDecode(idKey, out var id))
        {
            return Result<CampusDto>.Failure(Error.NotFound("Campus", idKey));
        }

        var entity = await context.Campuses
            .FirstOrDefaultAsync(c => c.Id == id, ct);

        if (entity == null)
        {
            return Result<CampusDto>.Failure(Error.NotFound("Campus", idKey));
        }

        var validationResult = await updateValidator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
        {
            return Result<CampusDto>.Failure(Error.FromFluentValidation(validationResult));
        }

        mapper.Map(request, entity);
        entity.ModifiedDateTime = DateTime.UtcNow;

        await context.SaveChangesAsync(ct);

        logger.LogInformation("Campus updated successfully: IdKey={IdKey}, Name={Name}", entity.IdKey, entity.Name);

        return Result<CampusDto>.Success(mapper.Map<CampusDto>(entity));
    }

    public async Task<Result> DeleteAsync(string idKey, CancellationToken ct = default)
    {
        if (!IdKeyHelper.TryDecode(idKey, out var id))
        {
            return Result.Failure(Error.NotFound("Campus", idKey));
        }

        var entity = await context.Campuses
            .FirstOrDefaultAsync(c => c.Id == id, ct);

        if (entity == null)
        {
            return Result.Failure(Error.NotFound("Campus", idKey));
        }

        // Soft delete
        entity.IsActive = false;
        entity.ModifiedDateTime = DateTime.UtcNow;

        await context.SaveChangesAsync(ct);

        logger.LogInformation("Campus deactivated successfully: IdKey={IdKey}", idKey);

        return Result.Success();
    }
}
