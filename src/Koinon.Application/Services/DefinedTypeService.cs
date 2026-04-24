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
/// Service implementation for DefinedType and DefinedValue management.
/// </summary>
public class DefinedTypeService(
    IApplicationDbContext context,
    IMapper mapper,
    IValidator<CreateDefinedValueRequest> createValidator,
    IValidator<UpdateDefinedValueRequest> updateValidator,
    ILogger<DefinedTypeService> logger) : IDefinedTypeService
{
    public async Task<IReadOnlyList<DefinedTypeSummaryDto>> GetAllTypesAsync(CancellationToken ct = default)
    {
        var types = await context.DefinedTypes
            .AsNoTracking()
            .OrderBy(t => t.Category)
            .ThenBy(t => t.Name)
            .Select(t => new DefinedTypeSummaryDto
            {
                IdKey = t.IdKey,
                Name = t.Name,
                Category = t.Category,
                IsSystem = t.IsSystem,
                Order = t.Order,
                ValueCount = t.DefinedValues.Count
            })
            .ToListAsync(ct);

        return types;
    }

    public async Task<Result<DefinedTypeDto>> GetTypeByIdKeyAsync(string idKey, CancellationToken ct = default)
    {
        if (!IdKeyHelper.TryDecode(idKey, out var id))
        {
            return Result<DefinedTypeDto>.Failure(Error.NotFound("DefinedType", idKey));
        }

        var entity = await context.DefinedTypes
            .AsNoTracking()
            .Include(t => t.DefinedValues.OrderBy(v => v.Order))
            .FirstOrDefaultAsync(t => t.Id == id, ct);

        if (entity == null)
        {
            return Result<DefinedTypeDto>.Failure(Error.NotFound("DefinedType", idKey));
        }

        return Result<DefinedTypeDto>.Success(mapper.Map<DefinedTypeDto>(entity));
    }

    public async Task<Result<IReadOnlyList<DefinedValueDto>>> GetValuesByTypeAsync(
        string idKeyOrGuid,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(idKeyOrGuid))
        {
            return Result<IReadOnlyList<DefinedValueDto>>.Failure(Error.NotFound("DefinedType", idKeyOrGuid));
        }

        // Accept either IdKey (Hashids-encoded int) or a raw GUID.
        // The frontend populates transaction-type dropdowns with the GUID from seed data.
        var query = context.DefinedTypes.AsNoTracking();

        if (Guid.TryParse(idKeyOrGuid, out var guid))
        {
            query = query.Where(t => t.Guid == guid);
        }
        else if (IdKeyHelper.TryDecode(idKeyOrGuid, out var id))
        {
            query = query.Where(t => t.Id == id);
        }
        else
        {
            return Result<IReadOnlyList<DefinedValueDto>>.Failure(Error.NotFound("DefinedType", idKeyOrGuid));
        }

        var entity = await query
            .Include(t => t.DefinedValues.OrderBy(v => v.Order))
            .FirstOrDefaultAsync(ct);

        if (entity == null)
        {
            return Result<IReadOnlyList<DefinedValueDto>>.Failure(Error.NotFound("DefinedType", idKeyOrGuid));
        }

        var values = mapper.Map<IReadOnlyList<DefinedValueDto>>(entity.DefinedValues);
        return Result<IReadOnlyList<DefinedValueDto>>.Success(values);
    }

    public async Task<Result<DefinedValueManagementDto>> CreateValueAsync(
        string typeIdKey,
        CreateDefinedValueRequest request,
        CancellationToken ct = default)
    {
        if (!IdKeyHelper.TryDecode(typeIdKey, out var typeId))
        {
            return Result<DefinedValueManagementDto>.Failure(Error.NotFound("DefinedType", typeIdKey));
        }

        var typeExists = await context.DefinedTypes
            .AsNoTracking()
            .AnyAsync(t => t.Id == typeId, ct);

        if (!typeExists)
        {
            return Result<DefinedValueManagementDto>.Failure(Error.NotFound("DefinedType", typeIdKey));
        }

        var validationResult = await createValidator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
        {
            return Result<DefinedValueManagementDto>.Failure(Error.FromFluentValidation(validationResult));
        }

        var maxOrder = await context.DefinedValues
            .Where(v => v.DefinedTypeId == typeId)
            .Select(v => (int?)v.Order)
            .MaxAsync(ct) ?? 0;

        var entity = new DefinedValue
        {
            DefinedTypeId = typeId,
            Value = request.Value,
            Description = request.Description,
            Order = request.Order > 0 ? request.Order : maxOrder + 1,
            IsActive = true,
            CreatedDateTime = DateTime.UtcNow
        };

        context.DefinedValues.Add(entity);
        await context.SaveChangesAsync(ct);

        // Re-fetch with nav prop to populate DefinedTypeIdKey for mapping
        var created = await context.DefinedValues
            .AsNoTracking()
            .Include(v => v.DefinedType)
            .FirstAsync(v => v.Id == entity.Id, ct);

        logger.LogInformation(
            "DefinedValue created: IdKey={IdKey}, Value={Value}, TypeIdKey={TypeIdKey}",
            entity.IdKey, entity.Value, typeIdKey);

        return Result<DefinedValueManagementDto>.Success(mapper.Map<DefinedValueManagementDto>(created));
    }

    public async Task<Result<DefinedValueManagementDto>> UpdateValueAsync(
        string valueIdKey,
        UpdateDefinedValueRequest request,
        CancellationToken ct = default)
    {
        if (!IdKeyHelper.TryDecode(valueIdKey, out var id))
        {
            return Result<DefinedValueManagementDto>.Failure(Error.NotFound("DefinedValue", valueIdKey));
        }

        // AsTracking required: global QueryTrackingBehavior is NoTracking (see PostgreSqlProvider),
        // so without it SaveChanges would not persist the mutations below. (fixes #685)
        var entity = await context.DefinedValues
            .AsTracking()
            .Include(v => v.DefinedType)
            .FirstOrDefaultAsync(v => v.Id == id, ct);

        if (entity == null)
        {
            return Result<DefinedValueManagementDto>.Failure(Error.NotFound("DefinedValue", valueIdKey));
        }

        var validationResult = await updateValidator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
        {
            return Result<DefinedValueManagementDto>.Failure(Error.FromFluentValidation(validationResult));
        }

        entity.Value = request.Value;
        entity.Description = request.Description;
        entity.Order = request.Order;
        entity.IsActive = request.IsActive;
        entity.ModifiedDateTime = DateTime.UtcNow;

        await context.SaveChangesAsync(ct);

        logger.LogInformation(
            "DefinedValue updated: IdKey={IdKey}, Value={Value}",
            entity.IdKey, entity.Value);

        return Result<DefinedValueManagementDto>.Success(mapper.Map<DefinedValueManagementDto>(entity));
    }

    public async Task<Result> DeleteValueAsync(string valueIdKey, CancellationToken ct = default)
    {
        if (!IdKeyHelper.TryDecode(valueIdKey, out var id))
        {
            return Result.Failure(Error.NotFound("DefinedValue", valueIdKey));
        }

        // AsTracking required for soft-delete mutation (global QueryTrackingBehavior is NoTracking). (fixes #685)
        var entity = await context.DefinedValues
            .AsTracking()
            .FirstOrDefaultAsync(v => v.Id == id, ct);

        if (entity == null)
        {
            return Result.Failure(Error.NotFound("DefinedValue", valueIdKey));
        }

        // Soft delete — set IsActive = false so foreign-key references remain valid
        entity.IsActive = false;
        entity.ModifiedDateTime = DateTime.UtcNow;

        await context.SaveChangesAsync(ct);

        logger.LogInformation("DefinedValue deactivated: IdKey={IdKey}", valueIdKey);

        return Result.Success();
    }

    public async Task<Result> ReorderValuesAsync(
        string typeIdKey,
        ReorderDefinedValuesRequest request,
        CancellationToken ct = default)
    {
        if (!IdKeyHelper.TryDecode(typeIdKey, out var typeId))
        {
            return Result.Failure(Error.NotFound("DefinedType", typeIdKey));
        }

        var typeExists = await context.DefinedTypes
            .AsNoTracking()
            .AnyAsync(t => t.Id == typeId, ct);

        if (!typeExists)
        {
            return Result.Failure(Error.NotFound("DefinedType", typeIdKey));
        }

        // Decode all IdKeys up front; skip any that are malformed
        var decodedItems = new List<(int Id, int Order)>();
        foreach (var item in request.Items)
        {
            if (IdKeyHelper.TryDecode(item.IdKey, out var itemId))
            {
                decodedItems.Add((itemId, item.Order));
            }
        }

        if (decodedItems.Count == 0)
        {
            return Result.Success();
        }

        var itemIds = decodedItems.Select(i => i.Id).ToArray();

        // AsTracking required for reorder mutations (global QueryTrackingBehavior is NoTracking). (fixes #685)
        var values = await context.DefinedValues
            .AsTracking()
            .Where(v => v.DefinedTypeId == typeId && itemIds.Contains(v.Id))
            .ToListAsync(ct);

        foreach (var (itemId, itemOrder) in decodedItems)
        {
            var value = values.Find(v => v.Id == itemId);
            if (value == null)
            {
                continue;
            }

            value.Order = itemOrder;
            value.ModifiedDateTime = DateTime.UtcNow;
        }

        await context.SaveChangesAsync(ct);

        logger.LogInformation(
            "Reordered {Count} DefinedValues for type IdKey={TypeIdKey}",
            values.Count, typeIdKey);

        return Result.Success();
    }
}
