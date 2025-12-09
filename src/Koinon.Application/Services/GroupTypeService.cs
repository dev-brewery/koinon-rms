using FluentValidation;
using Koinon.Application.Common;
using Koinon.Application.DTOs;
using Koinon.Application.DTOs.Requests;
using Koinon.Application.Interfaces;
using Koinon.Domain.Data;
using Koinon.Domain.Entities;
using Koinon.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Koinon.Application.Services;

/// <summary>
/// Service for group type management operations.
/// </summary>
public class GroupTypeService(
    IApplicationDbContext context,
    IValidator<CreateGroupTypeRequest> createValidator,
    IValidator<UpdateGroupTypeRequest> updateValidator,
    ILogger<GroupTypeService> logger) : IGroupTypeService
{
    public async Task<IReadOnlyList<GroupTypeDto>> GetAllGroupTypesAsync(
        bool includeArchived = false,
        CancellationToken ct = default)
    {
        var query = context.GroupTypes.AsNoTracking();

        if (!includeArchived)
        {
            query = query.Where(gt => !gt.IsArchived);
        }

        var groupTypes = await query
            .OrderBy(gt => gt.Order)
            .ThenBy(gt => gt.Name)
            .ToListAsync(ct);

        // Get group counts for each group type
        var groupCounts = await context.Groups
            .Where(g => !g.IsArchived)
            .GroupBy(g => g.GroupTypeId)
            .Select(g => new { GroupTypeId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.GroupTypeId, x => x.Count, ct);

        return groupTypes.Select(gt => MapToGroupTypeDto(gt, groupCounts.GetValueOrDefault(gt.Id, 0))).ToList();
    }

    public async Task<GroupTypeDetailDto?> GetGroupTypeByIdKeyAsync(string idKey, CancellationToken ct = default)
    {
        if (!IdKeyHelper.TryDecode(idKey, out int id))
        {
            return null;
        }

        var groupType = await context.GroupTypes
            .AsNoTracking()
            .FirstOrDefaultAsync(gt => gt.Id == id, ct);

        if (groupType == null)
        {
            return null;
        }

        // Get group count
        var groupCount = await context.Groups
            .Where(g => g.GroupTypeId == id && !g.IsArchived)
            .CountAsync(ct);

        return MapToGroupTypeDetailDto(groupType, groupCount);
    }

    public async Task<Result<GroupTypeDto>> CreateGroupTypeAsync(
        CreateGroupTypeRequest request,
        CancellationToken ct = default)
    {
        // Validate request
        var validationResult = await createValidator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
        {
            return Result<GroupTypeDto>.Failure(Error.FromFluentValidation(validationResult));
        }

        var groupType = new GroupType
        {
            Name = request.Name,
            Description = request.Description,
            IconCssClass = request.IconCssClass,
            Color = request.Color,
            GroupTerm = request.GroupTerm,
            GroupMemberTerm = request.GroupMemberTerm,
            TakesAttendance = request.TakesAttendance,
            AllowSelfRegistration = request.AllowSelfRegistration,
            RequiresMemberApproval = request.RequiresMemberApproval,
            DefaultIsPublic = request.DefaultIsPublic,
            DefaultGroupCapacity = request.DefaultGroupCapacity,
            ShowInGroupList = request.ShowInGroupList,
            ShowInNavigation = request.ShowInNavigation,
            Order = request.Order,
            Guid = Guid.NewGuid(),
            CreatedDateTime = DateTime.UtcNow,
            IsSystem = false
        };

        context.GroupTypes.Add(groupType);
        await context.SaveChangesAsync(ct);

        logger.LogInformation(
            "Group type created: IdKey={IdKey}, Name={Name}",
            groupType.IdKey, groupType.Name);

        return Result<GroupTypeDto>.Success(MapToGroupTypeDto(groupType, 0));
    }

    public async Task<Result<GroupTypeDto>> UpdateGroupTypeAsync(
        string idKey,
        UpdateGroupTypeRequest request,
        CancellationToken ct = default)
    {
        // Validate request
        var validationResult = await updateValidator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
        {
            return Result<GroupTypeDto>.Failure(Error.FromFluentValidation(validationResult));
        }

        // Decode IdKey
        if (!IdKeyHelper.TryDecode(idKey, out int id))
        {
            return Result<GroupTypeDto>.Failure(Error.NotFound("GroupType", idKey));
        }

        // Find group type
        var groupType = await context.GroupTypes.FindAsync([id], ct);
        if (groupType == null)
        {
            return Result<GroupTypeDto>.Failure(Error.NotFound("GroupType", idKey));
        }

        // Cannot update system group types
        if (groupType.IsSystem)
        {
            return Result<GroupTypeDto>.Failure(
                Error.UnprocessableEntity("Cannot modify system group types"));
        }

        // Update properties
        if (request.Name != null)
        {
            groupType.Name = request.Name;
        }

        if (request.Description != null)
        {
            groupType.Description = request.Description;
        }

        if (request.IconCssClass != null)
        {
            groupType.IconCssClass = request.IconCssClass;
        }

        if (request.Color != null)
        {
            groupType.Color = request.Color;
        }

        if (request.GroupTerm != null)
        {
            groupType.GroupTerm = request.GroupTerm;
        }

        if (request.GroupMemberTerm != null)
        {
            groupType.GroupMemberTerm = request.GroupMemberTerm;
        }

        if (request.TakesAttendance.HasValue)
        {
            groupType.TakesAttendance = request.TakesAttendance.Value;
        }

        if (request.AllowSelfRegistration.HasValue)
        {
            groupType.AllowSelfRegistration = request.AllowSelfRegistration.Value;
        }

        if (request.RequiresMemberApproval.HasValue)
        {
            groupType.RequiresMemberApproval = request.RequiresMemberApproval.Value;
        }

        if (request.DefaultIsPublic.HasValue)
        {
            groupType.DefaultIsPublic = request.DefaultIsPublic.Value;
        }

        if (request.DefaultGroupCapacity.HasValue)
        {
            groupType.DefaultGroupCapacity = request.DefaultGroupCapacity.Value;
        }

        if (request.ShowInGroupList.HasValue)
        {
            groupType.ShowInGroupList = request.ShowInGroupList.Value;
        }

        if (request.ShowInNavigation.HasValue)
        {
            groupType.ShowInNavigation = request.ShowInNavigation.Value;
        }

        if (request.Order.HasValue)
        {
            groupType.Order = request.Order.Value;
        }

        groupType.ModifiedDateTime = DateTime.UtcNow;

        await context.SaveChangesAsync(ct);

        // Get group count
        var groupCount = await context.Groups
            .Where(g => g.GroupTypeId == id && !g.IsArchived)
            .CountAsync(ct);

        logger.LogInformation(
            "Group type updated: IdKey={IdKey}, Name={Name}",
            groupType.IdKey, groupType.Name);

        return Result<GroupTypeDto>.Success(MapToGroupTypeDto(groupType, groupCount));
    }

    public async Task<Result<bool>> ArchiveGroupTypeAsync(string idKey, CancellationToken ct = default)
    {
        // Decode IdKey
        if (!IdKeyHelper.TryDecode(idKey, out int id))
        {
            return Result<bool>.Failure(Error.NotFound("GroupType", idKey));
        }

        // Find group type
        var groupType = await context.GroupTypes.FindAsync([id], ct);
        if (groupType == null)
        {
            return Result<bool>.Failure(Error.NotFound("GroupType", idKey));
        }

        // Cannot archive system group types
        if (groupType.IsSystem)
        {
            return Result<bool>.Failure(
                Error.UnprocessableEntity("Cannot archive system group types"));
        }

        // Check if any groups exist for this type
        var hasGroups = await context.Groups
            .AnyAsync(g => g.GroupTypeId == id && !g.IsArchived, ct);

        if (hasGroups)
        {
            return Result<bool>.Failure(
                Error.UnprocessableEntity("Cannot archive group type with existing groups. Archive the groups first."));
        }

        groupType.IsArchived = true;
        groupType.ArchivedDateTime = DateTime.UtcNow;
        groupType.ModifiedDateTime = DateTime.UtcNow;

        await context.SaveChangesAsync(ct);

        logger.LogInformation(
            "Group type archived: IdKey={IdKey}, Name={Name}",
            groupType.IdKey, groupType.Name);

        return Result<bool>.Success(true);
    }

    public async Task<IReadOnlyList<GroupSummaryDto>> GetGroupsByTypeAsync(
        string groupTypeIdKey,
        CancellationToken ct = default)
    {
        if (!IdKeyHelper.TryDecode(groupTypeIdKey, out int groupTypeId))
        {
            return Array.Empty<GroupSummaryDto>();
        }

        var groups = await context.Groups
            .AsNoTracking()
            .Include(g => g.GroupType)
            .Where(g => g.GroupTypeId == groupTypeId && !g.IsArchived)
            .OrderBy(g => g.Order)
            .ThenBy(g => g.Name)
            .ToListAsync(ct);

        // Get member counts for each group
        var groupIds = groups.Select(g => g.Id).ToList();
        var memberCounts = await context.GroupMembers
            .Where(gm => groupIds.Contains(gm.GroupId) && gm.GroupMemberStatus == GroupMemberStatus.Active)
            .GroupBy(gm => gm.GroupId)
            .Select(g => new { GroupId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.GroupId, x => x.Count, ct);

        return groups.Select(g => new GroupSummaryDto
        {
            IdKey = g.IdKey,
            Name = g.Name,
            Description = g.Description,
            IsActive = g.IsActive,
            IsArchived = g.IsArchived,
            MemberCount = memberCounts.GetValueOrDefault(g.Id, 0),
            GroupTypeName = g.GroupType?.Name ?? string.Empty
        }).ToList();
    }

    /// <summary>
    /// Maps a GroupType entity to GroupTypeDto.
    /// </summary>
    private static GroupTypeDto MapToGroupTypeDto(GroupType groupType, int groupCount)
    {
        return new GroupTypeDto
        {
            IdKey = groupType.IdKey,
            Guid = groupType.Guid,
            Name = groupType.Name,
            Description = groupType.Description,
            IconCssClass = groupType.IconCssClass,
            Color = groupType.Color,
            GroupTerm = groupType.GroupTerm,
            GroupMemberTerm = groupType.GroupMemberTerm,
            TakesAttendance = groupType.TakesAttendance,
            AllowSelfRegistration = groupType.AllowSelfRegistration,
            RequiresMemberApproval = groupType.RequiresMemberApproval,
            DefaultIsPublic = groupType.DefaultIsPublic,
            DefaultGroupCapacity = groupType.DefaultGroupCapacity,
            IsSystem = groupType.IsSystem,
            IsArchived = groupType.IsArchived,
            Order = groupType.Order,
            GroupCount = groupCount
        };
    }

    /// <summary>
    /// Maps a GroupType entity to GroupTypeDetailDto.
    /// </summary>
    private static GroupTypeDetailDto MapToGroupTypeDetailDto(GroupType groupType, int groupCount)
    {
        return new GroupTypeDetailDto
        {
            IdKey = groupType.IdKey,
            Guid = groupType.Guid,
            Name = groupType.Name,
            Description = groupType.Description,
            IconCssClass = groupType.IconCssClass,
            Color = groupType.Color,
            GroupTerm = groupType.GroupTerm,
            GroupMemberTerm = groupType.GroupMemberTerm,
            TakesAttendance = groupType.TakesAttendance,
            AllowSelfRegistration = groupType.AllowSelfRegistration,
            RequiresMemberApproval = groupType.RequiresMemberApproval,
            DefaultIsPublic = groupType.DefaultIsPublic,
            DefaultGroupCapacity = groupType.DefaultGroupCapacity,
            ShowInGroupList = groupType.ShowInGroupList,
            ShowInNavigation = groupType.ShowInNavigation,
            AttendanceCountsAsWeekendService = groupType.AttendanceCountsAsWeekendService,
            SendAttendanceReminder = groupType.SendAttendanceReminder,
            AllowMultipleLocations = groupType.AllowMultipleLocations,
            EnableSpecificGroupRequirements = groupType.EnableSpecificGroupRequirements,
            AllowGroupSync = groupType.AllowGroupSync,
            AllowSpecificGroupMemberAttributes = groupType.AllowSpecificGroupMemberAttributes,
            ShowConnectionStatus = groupType.ShowConnectionStatus,
            IgnorePersonInactivated = groupType.IgnorePersonInactivated,
            IsSystem = groupType.IsSystem,
            IsArchived = groupType.IsArchived,
            Order = groupType.Order,
            GroupCount = groupCount,
            CreatedDateTime = groupType.CreatedDateTime,
            ModifiedDateTime = groupType.ModifiedDateTime
        };
    }
}
