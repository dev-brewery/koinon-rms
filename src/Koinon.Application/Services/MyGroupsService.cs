using FluentValidation;
using Koinon.Application.Common;
using Koinon.Application.Constants;
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
/// Service for managing groups where the current user is a leader.
/// Provides leader-specific functionality including member management and attendance tracking.
/// </summary>
public class MyGroupsService(
    IApplicationDbContext context,
    IUserContext userContext,
    IValidator<UpdateGroupMemberRequest> updateMemberValidator,
    IValidator<RecordAttendanceRequest> recordAttendanceValidator,
    ILogger<MyGroupsService> logger) : IMyGroupsService
{
    public async Task<IReadOnlyList<MyGroupDto>> GetMyGroupsAsync(CancellationToken ct = default)
    {
        var currentPersonId = userContext.CurrentPersonId;
        if (!currentPersonId.HasValue)
        {
            return Array.Empty<MyGroupDto>();
        }

        // Get groups where current user is a leader
        var leaderGroups = await context.GroupMembers
            .AsNoTracking()
            .Include(gm => gm.Group)
                .ThenInclude(g => g!.GroupType)
            .Include(gm => gm.Group)
                .ThenInclude(g => g!.Campus)
            .Include(gm => gm.GroupRole)
            .Where(gm => gm.PersonId == currentPersonId.Value
                && gm.GroupMemberStatus == GroupMemberStatus.Active
                && gm.Group != null
                && gm.Group.GroupType != null
                && gm.GroupRole != null
                && gm.GroupRole.IsLeader
                && !gm.Group.IsArchived
                && !gm.Group.GroupType.IsFamilyGroupType)
            .Select(gm => gm.Group!)
            .Distinct()
            .ToListAsync(ct);

        var groupIds = leaderGroups.Select(g => g.Id).ToList();

        // Get member counts
        var memberCounts = await context.GroupMembers
            .Where(gm => groupIds.Contains(gm.GroupId) && gm.GroupMemberStatus == GroupMemberStatus.Active)
            .GroupBy(gm => gm.GroupId)
            .Select(g => new { GroupId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.GroupId, x => x.Count, ct);

        // Get last meeting dates from attendance occurrences
        var lastMeetingDates = await context.AttendanceOccurrences
            .Where(ao => groupIds.Contains(ao.GroupId!.Value) && ao.DidNotOccur != true)
            .GroupBy(ao => ao.GroupId)
            .Select(g => new
            {
                GroupId = g.Key!.Value,
                LastMeetingDate = g.Max(ao => ao.OccurrenceDate)
            })
            .ToDictionaryAsync(x => x.GroupId, x => (DateTime?)x.LastMeetingDate.ToDateTime(TimeOnly.MinValue), ct);

        // Map to DTOs
        var result = leaderGroups.Select(g => new MyGroupDto
        {
            IdKey = g.IdKey,
            Guid = g.Guid,
            Name = g.Name,
            Description = g.Description,
            GroupTypeName = g.GroupType?.Name ?? "Unknown",
            IsActive = g.IsActive,
            MemberCount = memberCounts.ContainsKey(g.Id) ? memberCounts[g.Id] : 0,
            GroupCapacity = g.GroupCapacity,
            LastMeetingDate = lastMeetingDates.ContainsKey(g.Id) ? lastMeetingDates[g.Id] : null,
            Campus = g.Campus != null ? new CampusSummaryDto
            {
                IdKey = g.Campus.IdKey,
                Name = g.Campus.Name,
                ShortCode = g.Campus.ShortCode
            } : null,
            CreatedDateTime = g.CreatedDateTime,
            ModifiedDateTime = g.ModifiedDateTime
        }).OrderBy(g => g.Name).ToList();

        logger.LogInformation(
            "Retrieved {Count} groups for leader PersonIdKey={PersonIdKey}",
            result.Count, IdKeyHelper.Encode(currentPersonId.Value));

        return result;
    }

    public async Task<Result<IReadOnlyList<GroupMemberDetailDto>>> GetGroupMembersWithContactInfoAsync(
        string groupIdKey,
        CancellationToken ct = default)
    {
        if (!IdKeyHelper.TryDecode(groupIdKey, out int groupId))
        {
            return Result<IReadOnlyList<GroupMemberDetailDto>>.Failure(Error.NotFound("Group", groupIdKey));
        }

        // Verify user is a leader of this group
        if (!await IsGroupLeaderAsync(groupId, ct))
        {
            return Result<IReadOnlyList<GroupMemberDetailDto>>.Failure(
                Error.Forbidden("You must be a leader of this group to view member contact information"));
        }

        // Get members with contact info
        var members = await context.GroupMembers
            .AsNoTracking()
            .Include(gm => gm.Person)
                .ThenInclude(p => p!.PhoneNumbers)
            .Include(gm => gm.GroupRole)
            .Where(gm => gm.GroupId == groupId && gm.GroupMemberStatus == GroupMemberStatus.Active && gm.Person != null && gm.GroupRole != null)
            .OrderBy(gm => gm.GroupRole!.Order)
            .ThenBy(gm => gm.Person!.LastName)
            .ThenBy(gm => gm.Person!.FirstName)
            .ToListAsync(ct);

        var memberDtos = members.Select(m => new GroupMemberDetailDto
        {
            IdKey = m.IdKey,
            PersonIdKey = m.Person!.IdKey,
            FirstName = m.Person.FirstName,
            LastName = m.Person.LastName,
            FullName = m.Person.FullName,
            Email = m.Person.Email,
            Phone = m.Person.PhoneNumbers
                .Where(pn => pn.NumberTypeValue != null)
                .OrderBy(pn => pn.NumberTypeValue!.Order)
                .FirstOrDefault()?.Number,
            PhotoUrl = null, // Photo URLs will be implemented in future work unit
            Age = m.Person.BirthDate.HasValue
                ? DateTime.UtcNow.Year - m.Person.BirthDate.Value.Year
                : null,
            Gender = m.Person.Gender.ToString(),
            Role = new GroupTypeRoleDto
            {
                IdKey = m.GroupRole!.IdKey,
                Name = m.GroupRole.Name,
                IsLeader = m.GroupRole.IsLeader
            },
            Status = m.GroupMemberStatus.ToString(),
            DateTimeAdded = m.DateTimeAdded,
            InactiveDateTime = m.InactiveDateTime,
            Note = m.Note
        }).ToList();

        logger.LogInformation(
            "Retrieved {Count} members with contact info for group {GroupIdKey}",
            memberDtos.Count, groupIdKey);

        return Result<IReadOnlyList<GroupMemberDetailDto>>.Success(memberDtos);
    }

    public async Task<Result<GroupMemberDetailDto>> UpdateGroupMemberAsync(
        string groupIdKey,
        string memberIdKey,
        UpdateGroupMemberRequest request,
        CancellationToken ct = default)
    {
        // Validate
        var validation = await updateMemberValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return Result<GroupMemberDetailDto>.Failure(Error.FromFluentValidation(validation));
        }

        if (!IdKeyHelper.TryDecode(groupIdKey, out int groupId))
        {
            return Result<GroupMemberDetailDto>.Failure(Error.NotFound("Group", groupIdKey));
        }

        if (!IdKeyHelper.TryDecode(memberIdKey, out int memberId))
        {
            return Result<GroupMemberDetailDto>.Failure(Error.NotFound("GroupMember", memberIdKey));
        }

        // Verify user is a leader of this group
        if (!await IsGroupLeaderAsync(groupId, ct))
        {
            return Result<GroupMemberDetailDto>.Failure(
                Error.Forbidden("You must be a leader of this group to update members"));
        }

        // Get the group member
        var groupMember = await context.GroupMembers
            .Include(gm => gm.Group)
                .ThenInclude(g => g!.GroupType)
            .Include(gm => gm.Person)
                .ThenInclude(p => p!.PhoneNumbers)
            .Include(gm => gm.GroupRole)
            .FirstOrDefaultAsync(gm => gm.Id == memberId && gm.GroupId == groupId, ct);

        if (groupMember is null)
        {
            return Result<GroupMemberDetailDto>.Failure(Error.NotFound("GroupMember", memberIdKey));
        }

        // Update role if provided
        if (!string.IsNullOrWhiteSpace(request.RoleId))
        {
            if (!IdKeyHelper.TryDecode(request.RoleId, out int roleId))
            {
                return Result<GroupMemberDetailDto>.Failure(Error.NotFound("Role", request.RoleId));
            }

            var role = await context.GroupTypeRoles
                .FirstOrDefaultAsync(r => r.Id == roleId && r.GroupTypeId == groupMember.Group!.GroupTypeId, ct);

            if (role is null)
            {
                return Result<GroupMemberDetailDto>.Failure(
                    Error.UnprocessableEntity("Role is not valid for this group type"));
            }

            groupMember.GroupRoleId = roleId;
        }

        // Update status if provided
        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            if (!Enum.TryParse<GroupMemberStatus>(request.Status, out var status))
            {
                return Result<GroupMemberDetailDto>.Failure(
                    Error.UnprocessableEntity("Invalid status value"));
            }

            groupMember.GroupMemberStatus = status;

            if (status == GroupMemberStatus.Inactive && groupMember.InactiveDateTime is null)
            {
                groupMember.InactiveDateTime = DateTime.UtcNow;
            }
            else if (status == GroupMemberStatus.Active)
            {
                groupMember.InactiveDateTime = null;
            }
        }

        // Update note if provided
        if (request.Note != null)
        {
            groupMember.Note = request.Note;
        }

        groupMember.ModifiedDateTime = DateTime.UtcNow;

        await context.SaveChangesAsync(ct);

        logger.LogInformation(
            "Updated group member {MemberIdKey} in group {GroupIdKey}",
            memberIdKey, groupIdKey);

        // Map from the already-tracked entity instead of re-querying
        var memberDto = new GroupMemberDetailDto
        {
            IdKey = groupMember.IdKey,
            PersonIdKey = groupMember.Person!.IdKey,
            FirstName = groupMember.Person.FirstName,
            LastName = groupMember.Person.LastName,
            FullName = groupMember.Person.FullName,
            Email = groupMember.Person.Email,
            Phone = groupMember.Person.PhoneNumbers
                .Where(pn => pn.NumberTypeValue != null)
                .OrderBy(pn => pn.NumberTypeValue!.Order)
                .FirstOrDefault()?.Number,
            PhotoUrl = null,
            Age = groupMember.Person.BirthDate.HasValue
                ? DateTime.UtcNow.Year - groupMember.Person.BirthDate.Value.Year
                : null,
            Gender = groupMember.Person.Gender.ToString(),
            Role = new GroupTypeRoleDto
            {
                IdKey = groupMember.GroupRole!.IdKey,
                Name = groupMember.GroupRole.Name,
                IsLeader = groupMember.GroupRole.IsLeader
            },
            Status = groupMember.GroupMemberStatus.ToString(),
            DateTimeAdded = groupMember.DateTimeAdded,
            InactiveDateTime = groupMember.InactiveDateTime,
            Note = groupMember.Note
        };

        return Result<GroupMemberDetailDto>.Success(memberDto);
    }

    public async Task<Result> RemoveGroupMemberAsync(
        string groupIdKey,
        string memberIdKey,
        CancellationToken ct = default)
    {
        if (!IdKeyHelper.TryDecode(groupIdKey, out int groupId))
        {
            return Result.Failure(Error.NotFound("Group", groupIdKey));
        }

        if (!IdKeyHelper.TryDecode(memberIdKey, out int memberId))
        {
            return Result.Failure(Error.NotFound("GroupMember", memberIdKey));
        }

        // Verify user is a leader of this group
        if (!await IsGroupLeaderAsync(groupId, ct))
        {
            return Result.Failure(
                Error.Forbidden("You must be a leader of this group to remove members"));
        }

        // Get the group member
        var groupMember = await context.GroupMembers
            .FirstOrDefaultAsync(gm => gm.Id == memberId && gm.GroupId == groupId, ct);

        if (groupMember is null)
        {
            return Result.Failure(Error.NotFound("GroupMember", memberIdKey));
        }

        // Soft delete by marking as inactive
        groupMember.GroupMemberStatus = GroupMemberStatus.Inactive;
        groupMember.InactiveDateTime = DateTime.UtcNow;
        groupMember.ModifiedDateTime = DateTime.UtcNow;

        await context.SaveChangesAsync(ct);

        logger.LogInformation(
            "Removed group member {MemberIdKey} from group {GroupIdKey}",
            memberIdKey, groupIdKey);

        return Result.Success();
    }

    public async Task<Result> RecordAttendanceAsync(
        string groupIdKey,
        RecordAttendanceRequest request,
        CancellationToken ct = default)
    {
        // Validate
        var validation = await recordAttendanceValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return Result.Failure(Error.FromFluentValidation(validation));
        }

        if (!IdKeyHelper.TryDecode(groupIdKey, out int groupId))
        {
            return Result.Failure(Error.NotFound("Group", groupIdKey));
        }

        // Verify user is a leader of this group
        if (!await IsGroupLeaderAsync(groupId, ct))
        {
            return Result.Failure(
                Error.Forbidden("You must be a leader of this group to record attendance"));
        }

        // Calculate Sunday date for the occurrence
        var occurrenceDateTime = request.OccurrenceDate.ToDateTime(TimeOnly.MinValue);
        var daysSinceSunday = ((int)occurrenceDateTime.DayOfWeek + 7) % 7;
        var sundayDate = DateOnly.FromDateTime(occurrenceDateTime.AddDays(-daysSinceSunday));

        // Check if occurrence already exists
        var occurrence = await context.AttendanceOccurrences
            .FirstOrDefaultAsync(ao => ao.GroupId == groupId && ao.OccurrenceDate == request.OccurrenceDate, ct);

        if (occurrence is null)
        {
            // Create new occurrence
            occurrence = new AttendanceOccurrence
            {
                GroupId = groupId,
                OccurrenceDate = request.OccurrenceDate,
                SundayDate = sundayDate,
                Notes = request.Notes,
                CreatedDateTime = DateTime.UtcNow
            };

            await context.AttendanceOccurrences.AddAsync(occurrence, ct);
            await context.SaveChangesAsync(ct);
        }
        else
        {
            // Update notes if provided
            if (request.Notes != null)
            {
                occurrence.Notes = request.Notes;
                occurrence.ModifiedDateTime = DateTime.UtcNow;
            }
        }

        // Get PersonAlias IDs for attendees
        var attendedPersonIds = new List<int>();
        foreach (var personIdKey in request.AttendedPersonIds)
        {
            if (IdKeyHelper.TryDecode(personIdKey, out int personId))
            {
                attendedPersonIds.Add(personId);
            }
        }

        // Get primary PersonAlias for each person
        var personAliasMap = await context.PersonAliases
            .Where(pa => attendedPersonIds.Contains(pa.PersonId!.Value) && pa.AliasPersonId == pa.PersonId)
            .ToDictionaryAsync(pa => pa.PersonId!.Value, pa => pa.Id, ct);

        // Get existing attendance records for this occurrence
        var existingAttendance = await context.Attendances
            .Where(a => a.OccurrenceId == occurrence.Id)
            .ToListAsync(ct);

        // Mark all existing as not attended
        foreach (var existing in existingAttendance)
        {
            existing.DidAttend = false;
            existing.ModifiedDateTime = DateTime.UtcNow;
        }

        // Create or update attendance records for attendees
        foreach (var personId in attendedPersonIds)
        {
            if (!personAliasMap.TryGetValue(personId, out int personAliasId))
            {
                continue;
            }

            var attendance = existingAttendance.FirstOrDefault(a => a.PersonAliasId == personAliasId);

            if (attendance is null)
            {
                attendance = new Attendance
                {
                    OccurrenceId = occurrence.Id,
                    PersonAliasId = personAliasId,
                    StartDateTime = occurrenceDateTime,
                    DidAttend = true,
                    CreatedDateTime = DateTime.UtcNow
                };

                await context.Attendances.AddAsync(attendance, ct);
            }
            else
            {
                attendance.DidAttend = true;
                attendance.ModifiedDateTime = DateTime.UtcNow;
            }
        }

        await context.SaveChangesAsync(ct);

        logger.LogInformation(
            "Recorded attendance for group {GroupIdKey} on {OccurrenceDate} with {Count} attendees",
            groupIdKey, request.OccurrenceDate, attendedPersonIds.Count);

        return Result.Success();
    }

    private async Task<bool> IsGroupLeaderAsync(int groupId, CancellationToken ct)
    {
        var currentPersonId = userContext.CurrentPersonId;
        if (!currentPersonId.HasValue)
        {
            return false;
        }

        // Check if user is a staff/admin role (can access all groups)
        if (userContext.IsInRole(Roles.Staff) || userContext.IsInRole(Roles.Admin))
        {
            return true;
        }

        // Check if user is a leader of this specific group
        return await context.GroupMembers
            .AsNoTracking()
            .Include(gm => gm.GroupRole)
            .AnyAsync(gm => gm.GroupId == groupId
                && gm.PersonId == currentPersonId.Value
                && gm.GroupMemberStatus == GroupMemberStatus.Active
                && gm.GroupRole!.IsLeader, ct);
    }
}
