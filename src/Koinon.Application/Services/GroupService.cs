using AutoMapper;
using AutoMapper.QueryableExtensions;
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
/// Service for group management operations (non-family groups).
/// </summary>
public class GroupService(
    IApplicationDbContext context,
    IMapper mapper,
    IValidator<CreateGroupRequest> createValidator,
    IValidator<UpdateGroupRequest> updateValidator,
    IValidator<AddGroupMemberRequest> addMemberValidator,
    ILogger<GroupService> logger) : IGroupService
{
    public async Task<GroupDto?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var group = await context.Groups
            .AsNoTracking()
            .Include(g => g.GroupType)
                .ThenInclude(gt => gt!.Roles)
            .Include(g => g.Campus)
            .Include(g => g.ParentGroup)
                .ThenInclude(pg => pg!.GroupType)
            .Include(g => g.Members.Where(m => m.GroupMemberStatus == GroupMemberStatus.Active))
                .ThenInclude(m => m.Person)
            .Include(g => g.Members.Where(m => m.GroupMemberStatus == GroupMemberStatus.Active))
                .ThenInclude(m => m.GroupRole)
            .Include(g => g.ChildGroups.Where(cg => !cg.IsArchived))
                .ThenInclude(cg => cg.GroupType)
            .FirstOrDefaultAsync(g => g.Id == id && g.GroupType!.Guid != SystemGuid.GroupType.Family, ct);

        if (group is null)
        {
            return null;
        }

        // Pre-calculate member counts for child groups
        var childGroupIds = group.ChildGroups.Select(cg => cg.Id).ToList();
        var childGroupMemberCounts = await context.GroupMembers
            .Where(gm => childGroupIds.Contains(gm.GroupId) && gm.GroupMemberStatus == GroupMemberStatus.Active)
            .GroupBy(gm => gm.GroupId)
            .Select(g => new { GroupId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.GroupId, x => x.Count, ct);

        // Pre-calculate member count for parent group
        int parentMemberCount = 0;
        if (group.ParentGroup != null)
        {
            parentMemberCount = await context.GroupMembers
                .CountAsync(gm => gm.GroupId == group.ParentGroupId && gm.GroupMemberStatus == GroupMemberStatus.Active, ct);
        }

        return MapToGroupDto(group, childGroupMemberCounts, parentMemberCount);
    }

    public async Task<GroupDto?> GetByIdKeyAsync(string idKey, CancellationToken ct = default)
    {
        if (!IdKeyHelper.TryDecode(idKey, out int id))
        {
            return null;
        }

        return await GetByIdAsync(id, ct);
    }

    public async Task<PagedResult<GroupSummaryDto>> SearchAsync(
        GroupSearchParameters parameters,
        CancellationToken ct = default)
    {
        var query = context.Groups
            .AsNoTracking()
            .Include(g => g.GroupType)
            .Where(g => !g.IsArchived && g.GroupType!.Guid != SystemGuid.GroupType.Family); // Exclude archived and family groups

        // Apply search query
        if (!string.IsNullOrWhiteSpace(parameters.Query))
        {
            var searchTerm = $"%{parameters.Query}%";
            query = query.Where(g =>
                EF.Functions.Like(g.Name, searchTerm) ||
                (g.Description != null && EF.Functions.Like(g.Description, searchTerm))
            );
        }

        // Filter by group type
        if (!string.IsNullOrWhiteSpace(parameters.GroupTypeId))
        {
            if (IdKeyHelper.TryDecode(parameters.GroupTypeId, out int groupTypeId))
            {
                query = query.Where(g => g.GroupTypeId == groupTypeId);
            }
        }

        // Filter by campus
        if (!string.IsNullOrWhiteSpace(parameters.CampusId))
        {
            if (IdKeyHelper.TryDecode(parameters.CampusId, out int campusId))
            {
                query = query.Where(g => g.CampusId == campusId);
            }
        }

        // Filter by parent group
        if (!string.IsNullOrWhiteSpace(parameters.ParentGroupId))
        {
            if (IdKeyHelper.TryDecode(parameters.ParentGroupId, out int parentGroupId))
            {
                query = query.Where(g => g.ParentGroupId == parentGroupId);
            }
        }

        // Exclude inactive by default
        if (!parameters.IncludeInactive)
        {
            query = query.Where(g => g.IsActive);
        }

        // Exclude archived by default
        if (!parameters.IncludeArchived)
        {
            query = query.Where(g => !g.IsArchived);
        }

        // Get total count
        var totalCount = await query.CountAsync(ct);

        // Get member counts for each group
        var groupIds = await query
            .OrderBy(g => g.Name)
            .Skip((parameters.Page - 1) * parameters.PageSize)
            .Take(parameters.PageSize)
            .Select(g => g.Id)
            .ToListAsync(ct);

        var memberCounts = await context.GroupMembers
            .Where(gm => groupIds.Contains(gm.GroupId) && gm.GroupMemberStatus == GroupMemberStatus.Active)
            .GroupBy(gm => gm.GroupId)
            .Select(g => new { GroupId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.GroupId, x => x.Count, ct);

        // Apply pagination and projection
        var items = await query
            .OrderBy(g => g.Name)
            .Skip((parameters.Page - 1) * parameters.PageSize)
            .Take(parameters.PageSize)
            .Select(g => new GroupSummaryDto
            {
                IdKey = g.IdKey,
                Name = g.Name,
                Description = g.Description,
                IsActive = g.IsActive,
                MemberCount = memberCounts.ContainsKey(g.Id) ? memberCounts[g.Id] : 0,
                GroupTypeName = g.GroupType!.Name
            })
            .ToListAsync(ct);

        logger.LogInformation(
            "Group search completed: Query={Query}, Results={Count}, Page={Page}",
            parameters.Query, totalCount, parameters.Page);

        return new PagedResult<GroupSummaryDto>(
            items, totalCount, parameters.Page, parameters.PageSize);
    }

    public async Task<Result<GroupDto>> CreateAsync(
        CreateGroupRequest request,
        CancellationToken ct = default)
    {
        // Validate
        var validation = await createValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return Result<GroupDto>.Failure(Error.FromFluentValidation(validation));
        }

        // Get group type and ensure it's not a family type
        if (!IdKeyHelper.TryDecode(request.GroupTypeId, out int groupTypeId))
        {
            return Result<GroupDto>.Failure(Error.NotFound("GroupType", request.GroupTypeId));
        }

        var groupType = await context.GroupTypes
            .FirstOrDefaultAsync(gt => gt.Id == groupTypeId, ct);

        if (groupType is null)
        {
            return Result<GroupDto>.Failure(Error.NotFound("GroupType", request.GroupTypeId));
        }

        // Prevent creating family groups - use FamilyService instead
        if (groupType.Guid == SystemGuid.GroupType.Family)
        {
            return Result<GroupDto>.Failure(Error.UnprocessableEntity(
                "Family groups cannot be created through GroupService. Use FamilyService instead."));
        }

        // Map to entity
        var group = mapper.Map<Group>(request);
        group.GroupTypeId = groupTypeId;
        group.CreatedDateTime = DateTime.UtcNow;

        // Decode and set optional IDs
        if (!string.IsNullOrWhiteSpace(request.ParentGroupId))
        {
            if (IdKeyHelper.TryDecode(request.ParentGroupId, out int parentGroupId))
            {
                var parentExists = await context.Groups
                    .AnyAsync(g => g.Id == parentGroupId, ct);

                if (!parentExists)
                {
                    return Result<GroupDto>.Failure(Error.NotFound("ParentGroup", request.ParentGroupId));
                }

                group.ParentGroupId = parentGroupId;
            }
        }

        if (!string.IsNullOrWhiteSpace(request.CampusId))
        {
            if (IdKeyHelper.TryDecode(request.CampusId, out int campusId))
            {
                var campusExists = await context.Campuses
                    .AnyAsync(c => c.Id == campusId, ct);

                if (!campusExists)
                {
                    return Result<GroupDto>.Failure(Error.NotFound("Campus", request.CampusId));
                }

                group.CampusId = campusId;
            }
        }

        // Add to database
        await context.Groups.AddAsync(group, ct);
        await context.SaveChangesAsync(ct);

        logger.LogInformation("Created group {GroupId}: {Name}", group.Id, group.Name);

        // Fetch full group with includes
        var createdGroup = await GetByIdAsync(group.Id, ct);
        return createdGroup != null
            ? Result<GroupDto>.Success(createdGroup)
            : Result<GroupDto>.Failure(Error.UnprocessableEntity("Failed to retrieve created group"));
    }

    public async Task<Result<GroupDto>> UpdateAsync(
        string idKey,
        UpdateGroupRequest request,
        CancellationToken ct = default)
    {
        // Validate
        var validation = await updateValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return Result<GroupDto>.Failure(Error.FromFluentValidation(validation));
        }

        // Get group
        if (!IdKeyHelper.TryDecode(idKey, out int id))
        {
            return Result<GroupDto>.Failure(Error.NotFound("Group", idKey));
        }

        var group = await context.Groups
            .Include(g => g.GroupType)
            .FirstOrDefaultAsync(g => g.Id == id, ct);

        if (group is null)
        {
            return Result<GroupDto>.Failure(Error.NotFound("Group", idKey));
        }

        // Update fields
        if (request.Name != null)
        {
            group.Name = request.Name;
        }

        if (request.Description != null)
        {
            group.Description = request.Description;
        }

        if (request.IsActive.HasValue)
        {
            group.IsActive = request.IsActive.Value;
        }

        if (request.IsPublic.HasValue)
        {
            group.IsPublic = request.IsPublic.Value;
        }

        if (request.AllowGuests.HasValue)
        {
            group.AllowGuests = request.AllowGuests.Value;
        }

        if (request.GroupCapacity.HasValue)
        {
            group.GroupCapacity = request.GroupCapacity;
        }

        if (request.Order.HasValue)
        {
            group.Order = request.Order.Value;
        }

        if (!string.IsNullOrWhiteSpace(request.CampusId))
        {
            if (IdKeyHelper.TryDecode(request.CampusId, out int campusId))
            {
                var campusExists = await context.Campuses
                    .AnyAsync(c => c.Id == campusId, ct);

                if (!campusExists)
                {
                    return Result<GroupDto>.Failure(Error.NotFound("Campus", request.CampusId));
                }

                group.CampusId = campusId;
            }
        }

        group.ModifiedDateTime = DateTime.UtcNow;

        await context.SaveChangesAsync(ct);

        logger.LogInformation("Updated group {GroupId}: {Name}", group.Id, group.Name);

        // Return updated group
        var updatedGroup = await GetByIdAsync(group.Id, ct);
        return updatedGroup != null
            ? Result<GroupDto>.Success(updatedGroup)
            : Result<GroupDto>.Failure(Error.UnprocessableEntity("Failed to retrieve updated group"));
    }

    public async Task<Result> DeleteAsync(string idKey, CancellationToken ct = default)
    {
        if (!IdKeyHelper.TryDecode(idKey, out int id))
        {
            return Result.Failure(Error.NotFound("Group", idKey));
        }

        var group = await context.Groups
            .Include(g => g.GroupType)
            .FirstOrDefaultAsync(g => g.Id == id, ct);

        if (group is null)
        {
            return Result.Failure(Error.NotFound("Group", idKey));
        }

        // Check if group is protected
        if (group.IsSystem)
        {
            return Result.Failure(
                Error.UnprocessableEntity("Cannot delete system-protected groups"));
        }

        // Soft delete by archiving
        group.IsArchived = true;
        group.ArchivedDateTime = DateTime.UtcNow;
        group.ModifiedDateTime = DateTime.UtcNow;

        await context.SaveChangesAsync(ct);

        logger.LogInformation("Archived group {GroupId}: {Name}", group.Id, group.Name);

        return Result.Success();
    }

    public async Task<Result<GroupMemberDto>> AddMemberAsync(
        string groupIdKey,
        AddGroupMemberRequest request,
        CancellationToken ct = default)
    {
        // Validate
        var validation = await addMemberValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return Result<GroupMemberDto>.Failure(Error.FromFluentValidation(validation));
        }

        // Get group
        if (!IdKeyHelper.TryDecode(groupIdKey, out int groupId))
        {
            return Result<GroupMemberDto>.Failure(Error.NotFound("Group", groupIdKey));
        }

        var group = await context.Groups
            .Include(g => g.GroupType)
            .FirstOrDefaultAsync(g => g.Id == groupId, ct);

        if (group is null)
        {
            return Result<GroupMemberDto>.Failure(Error.NotFound("Group", groupIdKey));
        }

        // Get person
        if (!IdKeyHelper.TryDecode(request.PersonId, out int personId))
        {
            return Result<GroupMemberDto>.Failure(Error.NotFound("Person", request.PersonId));
        }

        var person = await context.People.FindAsync(new object[] { personId }, ct);
        if (person is null)
        {
            return Result<GroupMemberDto>.Failure(Error.NotFound("Person", request.PersonId));
        }

        // Get role
        if (!IdKeyHelper.TryDecode(request.RoleId, out int roleId))
        {
            return Result<GroupMemberDto>.Failure(Error.NotFound("Role", request.RoleId));
        }

        var role = await context.GroupTypeRoles
            .FirstOrDefaultAsync(r => r.Id == roleId && r.GroupTypeId == group.GroupTypeId, ct);

        if (role is null)
        {
            return Result<GroupMemberDto>.Failure(
                Error.UnprocessableEntity("Role is not valid for this group type"));
        }

        // Check if person is already an active member
        var existingMember = await context.GroupMembers
            .FirstOrDefaultAsync(gm => gm.GroupId == groupId && gm.PersonId == personId, ct);

        if (existingMember != null && existingMember.GroupMemberStatus == GroupMemberStatus.Active)
        {
            return Result<GroupMemberDto>.Failure(
                Error.Conflict("Person is already an active member of this group"));
        }

        // If there's an inactive member, reactivate
        if (existingMember != null)
        {
            existingMember.GroupMemberStatus = GroupMemberStatus.Active;
            existingMember.GroupRoleId = roleId;
            existingMember.InactiveDateTime = null;
            existingMember.Note = request.Note;
            existingMember.ModifiedDateTime = DateTime.UtcNow;

            await context.SaveChangesAsync(ct);

            logger.LogInformation(
                "Reactivated person {PersonId} in group {GroupId} with role {RoleId}",
                personId, groupId, roleId);
        }
        else
        {
            // Create new group member
            var groupMember = new GroupMember
            {
                GroupId = groupId,
                PersonId = personId,
                GroupRoleId = roleId,
                GroupMemberStatus = GroupMemberStatus.Active,
                DateTimeAdded = DateTime.UtcNow,
                Note = request.Note,
                CreatedDateTime = DateTime.UtcNow
            };

            await context.GroupMembers.AddAsync(groupMember, ct);
            await context.SaveChangesAsync(ct);

            logger.LogInformation(
                "Added person {PersonId} to group {GroupId} with role {RoleId}",
                personId, groupId, roleId);
        }

        // Fetch full member with includes
        var createdMember = await context.GroupMembers
            .AsNoTracking()
            .Include(gm => gm.Person)
            .Include(gm => gm.GroupRole)
            .FirstOrDefaultAsync(gm => gm.GroupId == groupId && gm.PersonId == personId, ct);

        if (createdMember is null)
        {
            return Result<GroupMemberDto>.Failure(
                Error.UnprocessableEntity("Failed to retrieve created group member"));
        }

        var memberDto = mapper.Map<GroupMemberDto>(createdMember);
        return Result<GroupMemberDto>.Success(memberDto);
    }

    public async Task<Result> RemoveMemberAsync(
        string groupIdKey,
        string personIdKey,
        CancellationToken ct = default)
    {
        // Get group
        if (!IdKeyHelper.TryDecode(groupIdKey, out int groupId))
        {
            return Result.Failure(Error.NotFound("Group", groupIdKey));
        }

        var group = await context.Groups
            .Include(g => g.GroupType)
            .FirstOrDefaultAsync(g => g.Id == groupId, ct);

        if (group is null)
        {
            return Result.Failure(Error.NotFound("Group", groupIdKey));
        }

        // Get person
        if (!IdKeyHelper.TryDecode(personIdKey, out int personId))
        {
            return Result.Failure(Error.NotFound("Person", personIdKey));
        }

        // Find group member
        var groupMember = await context.GroupMembers
            .FirstOrDefaultAsync(gm => gm.GroupId == groupId && gm.PersonId == personId, ct);

        if (groupMember is null)
        {
            return Result.Failure(
                Error.NotFound("GroupMember", $"Person {personIdKey} in Group {groupIdKey}"));
        }

        // Soft delete by marking as inactive
        groupMember.GroupMemberStatus = GroupMemberStatus.Inactive;
        groupMember.InactiveDateTime = DateTime.UtcNow;
        groupMember.ModifiedDateTime = DateTime.UtcNow;

        await context.SaveChangesAsync(ct);

        logger.LogInformation(
            "Removed person {PersonId} from group {GroupId}",
            personId, groupId);

        return Result.Success();
    }

    public async Task<IReadOnlyList<GroupMemberDto>> GetMembersAsync(
        string groupIdKey,
        CancellationToken ct = default)
    {
        if (!IdKeyHelper.TryDecode(groupIdKey, out int groupId))
        {
            return Array.Empty<GroupMemberDto>();
        }

        var members = await context.GroupMembers
            .AsNoTracking()
            .Include(gm => gm.Person)
            .Include(gm => gm.GroupRole)
            .Include(gm => gm.Group)
                .ThenInclude(g => g!.GroupType)
            .Where(gm => gm.GroupId == groupId
                && gm.GroupMemberStatus == GroupMemberStatus.Active
)
            .OrderBy(gm => gm.GroupRole!.Order)
            .ThenBy(gm => gm.Person!.LastName)
            .ThenBy(gm => gm.Person!.FirstName)
            .ToListAsync(ct);

        return members.Select(m => mapper.Map<GroupMemberDto>(m)).ToList();
    }

    public async Task<GroupSummaryDto?> GetParentGroupAsync(
        string groupIdKey,
        CancellationToken ct = default)
    {
        if (!IdKeyHelper.TryDecode(groupIdKey, out int groupId))
        {
            return null;
        }

        var group = await context.Groups
            .AsNoTracking()
            .Include(g => g.ParentGroup)
                .ThenInclude(pg => pg!.GroupType)
            .FirstOrDefaultAsync(g => g.Id == groupId, ct);

        if (group?.ParentGroup is null)
        {
            return null;
        }

        var memberCount = await context.GroupMembers
            .CountAsync(gm => gm.GroupId == group.ParentGroupId
                && gm.GroupMemberStatus == GroupMemberStatus.Active, ct);

        return new GroupSummaryDto
        {
            IdKey = group.ParentGroup.IdKey,
            Name = group.ParentGroup.Name,
            Description = group.ParentGroup.Description,
            IsActive = group.ParentGroup.IsActive,
            MemberCount = memberCount,
            GroupTypeName = group.ParentGroup.GroupType!.Name
        };
    }

    public async Task<IReadOnlyList<GroupSummaryDto>> GetChildGroupsAsync(
        string groupIdKey,
        CancellationToken ct = default)
    {
        if (!IdKeyHelper.TryDecode(groupIdKey, out int groupId))
        {
            return Array.Empty<GroupSummaryDto>();
        }

        var childGroups = await context.Groups
            .AsNoTracking()
            .Include(g => g.GroupType)
            .Where(g => g.ParentGroupId == groupId
                && !g.IsArchived
)
            .OrderBy(g => g.Order)
            .ThenBy(g => g.Name)
            .ToListAsync(ct);

        var groupIds = childGroups.Select(g => g.Id).ToList();
        var memberCounts = await context.GroupMembers
            .Where(gm => groupIds.Contains(gm.GroupId) && gm.GroupMemberStatus == GroupMemberStatus.Active)
            .GroupBy(gm => gm.GroupId)
            .Select(g => new { GroupId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.GroupId, x => x.Count, ct);

        return childGroups.Select(g => new GroupSummaryDto
        {
            IdKey = g.IdKey,
            Name = g.Name,
            Description = g.Description,
            IsActive = g.IsActive,
            MemberCount = memberCounts.ContainsKey(g.Id) ? memberCounts[g.Id] : 0,
            GroupTypeName = g.GroupType!.Name
        }).ToList();
    }

    public async Task<IReadOnlyList<GroupScheduleDto>> GetSchedulesAsync(
        string groupIdKey,
        CancellationToken ct = default)
    {
        if (!IdKeyHelper.TryDecode(groupIdKey, out int groupId))
        {
            return Array.Empty<GroupScheduleDto>();
        }

        var groupSchedules = await context.GroupSchedules
            .AsNoTracking()
            .Include(gs => gs.Schedule)
            .Where(gs => gs.GroupId == groupId)
            .OrderBy(gs => gs.Order)
            .ToListAsync(ct);

        return groupSchedules.Select(gs => new GroupScheduleDto
        {
            IdKey = gs.IdKey,
            Guid = gs.Guid,
            Schedule = new ScheduleSummaryDto
            {
                IdKey = gs.Schedule!.IdKey,
                Guid = gs.Schedule.Guid,
                Name = gs.Schedule.Name,
                Description = gs.Schedule.Description,
                WeeklyDayOfWeek = gs.Schedule.WeeklyDayOfWeek,
                WeeklyTimeOfDay = gs.Schedule.WeeklyTimeOfDay,
                IsActive = gs.Schedule.IsActive
            },
            Order = gs.Order
        }).ToList();
    }

    public async Task<Result<GroupScheduleDto>> AddScheduleAsync(
        string groupIdKey,
        AddGroupScheduleRequest request,
        CancellationToken ct = default)
    {
        if (!IdKeyHelper.TryDecode(groupIdKey, out int groupId))
        {
            return Result<GroupScheduleDto>.Failure(
                new Error("NOT_FOUND", $"Group with IdKey '{groupIdKey}' not found"));
        }

        if (!IdKeyHelper.TryDecode(request.ScheduleIdKey, out int scheduleId))
        {
            return Result<GroupScheduleDto>.Failure(
                new Error("NOT_FOUND", $"Schedule with IdKey '{request.ScheduleIdKey}' not found"));
        }

        // Verify group exists
        var group = await context.Groups
            .FirstOrDefaultAsync(g => g.Id == groupId && !g.IsArchived, ct);

        if (group == null)
        {
            return Result<GroupScheduleDto>.Failure(
                new Error("NOT_FOUND", $"Group with IdKey '{groupIdKey}' not found"));
        }

        // Verify schedule exists
        var schedule = await context.Schedules
            .FirstOrDefaultAsync(s => s.Id == scheduleId && s.IsActive, ct);

        if (schedule == null)
        {
            return Result<GroupScheduleDto>.Failure(
                new Error("NOT_FOUND", $"Schedule with IdKey '{request.ScheduleIdKey}' not found"));
        }

        // Check if association already exists
        var existing = await context.GroupSchedules
            .FirstOrDefaultAsync(gs => gs.GroupId == groupId && gs.ScheduleId == scheduleId, ct);

        if (existing != null)
        {
            return Result<GroupScheduleDto>.Failure(
                new Error("DUPLICATE", "This schedule is already associated with the group"));
        }

        var groupSchedule = new GroupSchedule
        {
            GroupId = groupId,
            ScheduleId = scheduleId,
            Order = request.Order
        };

        context.GroupSchedules.Add(groupSchedule);
        await context.SaveChangesAsync(ct);

        logger.LogInformation(
            "Schedule added to group: GroupId={GroupId}, ScheduleId={ScheduleId}",
            groupId, scheduleId);

        return Result<GroupScheduleDto>.Success(new GroupScheduleDto
        {
            IdKey = groupSchedule.IdKey,
            Guid = groupSchedule.Guid,
            Schedule = new ScheduleSummaryDto
            {
                IdKey = schedule.IdKey,
                Guid = schedule.Guid,
                Name = schedule.Name,
                Description = schedule.Description,
                WeeklyDayOfWeek = schedule.WeeklyDayOfWeek,
                WeeklyTimeOfDay = schedule.WeeklyTimeOfDay,
                IsActive = schedule.IsActive
            },
            Order = groupSchedule.Order
        });
    }

    public async Task<Result> RemoveScheduleAsync(
        string groupIdKey,
        string scheduleIdKey,
        CancellationToken ct = default)
    {
        if (!IdKeyHelper.TryDecode(groupIdKey, out int groupId))
        {
            return Result.Failure(
                new Error("NOT_FOUND", $"Group with IdKey '{groupIdKey}' not found"));
        }

        if (!IdKeyHelper.TryDecode(scheduleIdKey, out int scheduleId))
        {
            return Result.Failure(
                new Error("NOT_FOUND", $"Schedule with IdKey '{scheduleIdKey}' not found"));
        }

        var groupSchedule = await context.GroupSchedules
            .FirstOrDefaultAsync(gs => gs.GroupId == groupId && gs.ScheduleId == scheduleId, ct);

        if (groupSchedule == null)
        {
            return Result.Failure(
                new Error("NOT_FOUND", "Schedule association not found for this group"));
        }

        context.GroupSchedules.Remove(groupSchedule);
        await context.SaveChangesAsync(ct);

        logger.LogInformation(
            "Schedule removed from group: GroupId={GroupId}, ScheduleId={ScheduleId}",
            groupId, scheduleId);

        return Result.Success();
    }

    private GroupDto MapToGroupDto(Group group, Dictionary<int, int> childGroupMemberCounts, int parentMemberCount)
    {
        var groupDto = mapper.Map<GroupDto>(group);

        // Map active members
        var memberDtos = group.Members
            .Where(m => m.GroupMemberStatus == GroupMemberStatus.Active)
            .Select(m => mapper.Map<GroupMemberDto>(m))
            .ToList();

        // Map child groups using pre-calculated member counts
        var childGroupDtos = group.ChildGroups
            .Where(cg => !cg.IsArchived)
            .Select(cg => new GroupSummaryDto
            {
                IdKey = cg.IdKey,
                Name = cg.Name,
                Description = cg.Description,
                IsActive = cg.IsActive,
                MemberCount = childGroupMemberCounts.ContainsKey(cg.Id) ? childGroupMemberCounts[cg.Id] : 0,
                GroupTypeName = cg.GroupType?.Name ?? "Unknown"
            })
            .ToList();

        // Map parent group summary using pre-calculated member count
        GroupSummaryDto? parentGroupDto = null;
        if (group.ParentGroup != null)
        {
            parentGroupDto = new GroupSummaryDto
            {
                IdKey = group.ParentGroup.IdKey,
                Name = group.ParentGroup.Name,
                Description = group.ParentGroup.Description,
                IsActive = group.ParentGroup.IsActive,
                MemberCount = parentMemberCount,
                GroupTypeName = group.ParentGroup.GroupType?.Name ?? "Unknown"
            };
        }

        // Create new DTO with members and children populated
        var result = new GroupDto
        {
            IdKey = groupDto.IdKey,
            Guid = groupDto.Guid,
            Name = groupDto.Name,
            Description = groupDto.Description,
            IsActive = groupDto.IsActive,
            IsArchived = groupDto.IsArchived,
            IsSecurityRole = groupDto.IsSecurityRole,
            IsPublic = groupDto.IsPublic,
            AllowGuests = groupDto.AllowGuests,
            GroupCapacity = groupDto.GroupCapacity,
            Order = groupDto.Order,
            GroupType = groupDto.GroupType,
            Campus = groupDto.Campus,
            ParentGroup = parentGroupDto,
            Members = memberDtos,
            ChildGroups = childGroupDtos,
            CreatedDateTime = groupDto.CreatedDateTime,
            ModifiedDateTime = groupDto.ModifiedDateTime,
            ArchivedDateTime = groupDto.ArchivedDateTime
        };

        return result;
    }
}
