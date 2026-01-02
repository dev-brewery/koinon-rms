using AutoMapper;
using Koinon.Application.Common;
using Koinon.Application.DTOs;
using Koinon.Application.Interfaces;
using Koinon.Domain.Data;
using Koinon.Domain.Entities;
using Koinon.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Koinon.Application.Services;

/// <summary>
/// Service for communication template management operations.
/// </summary>
public class CommunicationTemplateService(
    IApplicationDbContext context,
    IMapper mapper,
    ILogger<CommunicationTemplateService> logger) : ICommunicationTemplateService
{
    public async Task<CommunicationTemplateDto?> GetByIdKeyAsync(string idKey, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (!IdKeyHelper.TryDecode(idKey, out int id))
        {
            return null;
        }

        var template = await context.CommunicationTemplates
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id, ct);

        if (template is null)
        {
            return null;
        }

        return mapper.Map<CommunicationTemplateDto>(template);
    }

    public async Task<PagedResult<CommunicationTemplateSummaryDto>> SearchAsync(
        string? type = null,
        bool? isActive = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var query = context.CommunicationTemplates
            .AsNoTracking();

        // Filter by communication type if provided
        if (!string.IsNullOrWhiteSpace(type) && Enum.TryParse<CommunicationType>(type, true, out var typeEnum))
        {
            if (!Enum.IsDefined(typeof(CommunicationType), typeEnum))
            {
                return new PagedResult<CommunicationTemplateSummaryDto>(
                    new List<CommunicationTemplateSummaryDto>(),
                    0,
                    page,
                    pageSize);
            }
            query = query.Where(t => t.CommunicationType == typeEnum);
        }

        // Filter by active status if provided
        if (isActive.HasValue)
        {
            query = query.Where(t => t.IsActive == isActive.Value);
        }

        // Get total count
        var totalCount = await query.CountAsync(ct);

        // Get paged results
        var templates = await query
            .OrderBy(t => t.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var items = templates.Select(t => mapper.Map<CommunicationTemplateSummaryDto>(t)).ToList();

        return new PagedResult<CommunicationTemplateSummaryDto>(
            items,
            totalCount,
            page,
            pageSize);
    }

    public async Task<Result<CommunicationTemplateDto>> CreateAsync(
        CreateCommunicationTemplateDto dto,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        // Parse communication type
        if (!Enum.TryParse<CommunicationType>(dto.CommunicationType, true, out var communicationType))
        {
            return Result<CommunicationTemplateDto>.Failure(
                new Error("INVALID_TYPE", $"Invalid communication type: {dto.CommunicationType}"));
        }

        if (!Enum.IsDefined(typeof(CommunicationType), communicationType))
        {
            return Result<CommunicationTemplateDto>.Failure(
                new Error("INVALID_TYPE", $"Invalid communication type: {dto.CommunicationType}"));
        }

        // Check for duplicate name
        var existingTemplate = await context.CommunicationTemplates
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Name == dto.Name, ct);

        if (existingTemplate is not null)
        {
            return Result<CommunicationTemplateDto>.Failure(
                Error.Conflict($"A template with name '{dto.Name}' already exists"));
        }

        // Create template
        var template = new CommunicationTemplate
        {
            Name = dto.Name,
            CommunicationType = communicationType,
            Subject = dto.Subject,
            Body = dto.Body,
            Description = dto.Description,
            IsActive = dto.IsActive,
            CreatedDateTime = DateTime.UtcNow
        };

        context.CommunicationTemplates.Add(template);
        await context.SaveChangesAsync(ct);

        logger.LogInformation(
            "Created communication template {TemplateId} with name '{TemplateName}'",
            template.IdKey,
            template.Name);

        return Result<CommunicationTemplateDto>.Success(mapper.Map<CommunicationTemplateDto>(template));
    }

    public async Task<Result<CommunicationTemplateDto>> UpdateAsync(
        string idKey,
        UpdateCommunicationTemplateDto dto,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (!IdKeyHelper.TryDecode(idKey, out int id))
        {
            return Result<CommunicationTemplateDto>.Failure(Error.NotFound("CommunicationTemplate", idKey));
        }

        var template = await context.CommunicationTemplates
            .FirstOrDefaultAsync(t => t.Id == id, ct);

        if (template is null)
        {
            return Result<CommunicationTemplateDto>.Failure(Error.NotFound("CommunicationTemplate", idKey));
        }

        // Check for duplicate name if name is being changed
        if (dto.Name is not null && dto.Name != template.Name)
        {
            var existingTemplate = await context.CommunicationTemplates
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Name == dto.Name, ct);

            if (existingTemplate is not null)
            {
                return Result<CommunicationTemplateDto>.Failure(
                    Error.Conflict($"A template with name '{dto.Name}' already exists"));
            }

            template.Name = dto.Name;
        }

        // Update communication type if provided
        if (dto.CommunicationType is not null)
        {
            if (!Enum.TryParse<CommunicationType>(dto.CommunicationType, true, out var typeEnum))
            {
                return Result<CommunicationTemplateDto>.Failure(
                    new Error("INVALID_TYPE", $"Invalid communication type: {dto.CommunicationType}"));
            }

            if (!Enum.IsDefined(typeof(CommunicationType), typeEnum))
            {
                return Result<CommunicationTemplateDto>.Failure(
                    new Error("INVALID_TYPE", $"Invalid communication type: {dto.CommunicationType}"));
            }
            template.CommunicationType = typeEnum;
        }

        // Update other properties if provided
        if (dto.Subject is not null)
        {
            template.Subject = dto.Subject;
        }

        if (dto.Body is not null)
        {
            template.Body = dto.Body;
        }

        if (dto.Description is not null)
        {
            template.Description = dto.Description;
        }

        if (dto.IsActive.HasValue)
        {
            template.IsActive = dto.IsActive.Value;
        }

        template.ModifiedDateTime = DateTime.UtcNow;

        await context.SaveChangesAsync(ct);

        logger.LogInformation("Updated communication template {TemplateId}", template.IdKey);

        return Result<CommunicationTemplateDto>.Success(mapper.Map<CommunicationTemplateDto>(template));
    }

    public async Task<Result> DeleteAsync(string idKey, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (!IdKeyHelper.TryDecode(idKey, out int id))
        {
            return Result.Failure(Error.NotFound("CommunicationTemplate", idKey));
        }

        var template = await context.CommunicationTemplates
            .FirstOrDefaultAsync(t => t.Id == id, ct);

        if (template is null)
        {
            return Result.Failure(Error.NotFound("CommunicationTemplate", idKey));
        }

        context.CommunicationTemplates.Remove(template);
        await context.SaveChangesAsync(ct);

        logger.LogInformation("Deleted communication template {TemplateId}", idKey);

        return Result.Success();
    }
}
