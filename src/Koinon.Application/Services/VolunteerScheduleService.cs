using Koinon.Application.Common;
using Koinon.Application.DTOs.VolunteerSchedule;
using Koinon.Application.Interfaces;
using Koinon.Domain.Data;
using Koinon.Domain.Entities;
using Koinon.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Koinon.Application.Services;

/// <summary>
/// Service for managing volunteer schedule assignments.
/// Handles creating assignments, tracking confirmations/declines, and preventing double-booking.
/// </summary>
public class VolunteerScheduleService(
    IApplicationDbContext context,
    ILogger<VolunteerScheduleService> logger) : IVolunteerScheduleService
{
    public async Task<Result<List<ScheduleAssignmentDto>>> CreateAssignmentsAsync(
        string groupIdKey,
        CreateScheduleAssignmentsRequest request,
        CancellationToken ct = default)
    {
        // Decode group IdKey
        if (!IdKeyHelper.TryDecode(groupIdKey, out int groupId))
        {
            return Result<List<ScheduleAssignmentDto>>.Failure(Error.NotFound("Group", groupIdKey));
        }

        // Decode schedule IdKey
        if (!IdKeyHelper.TryDecode(request.ScheduleIdKey, out int scheduleId))
        {
            return Result<List<ScheduleAssignmentDto>>.Failure(Error.NotFound("Schedule", request.ScheduleIdKey));
        }

        // Verify group exists
        var groupExists = await context.Groups
            .AsNoTracking()
            .AnyAsync(g => g.Id == groupId, ct);

        if (!groupExists)
        {
            return Result<List<ScheduleAssignmentDto>>.Failure(Error.NotFound("Group", groupIdKey));
        }

        // Verify schedule exists
        var schedule = await context.Schedules
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == scheduleId, ct);

        if (schedule == null)
        {
            return Result<List<ScheduleAssignmentDto>>.Failure(Error.NotFound("Schedule", request.ScheduleIdKey));
        }

        // Decode member IdKeys
        var memberIds = new List<int>();
        foreach (var memberIdKey in request.MemberIdKeys)
        {
            if (!IdKeyHelper.TryDecode(memberIdKey, out int memberId))
            {
                return Result<List<ScheduleAssignmentDto>>.Failure(Error.NotFound("GroupMember", memberIdKey));
            }
            memberIds.Add(memberId);
        }

        // Verify all members exist and belong to the group
        var members = await context.GroupMembers
            .Include(gm => gm.Person)
            .Where(gm => memberIds.Contains(gm.Id) && gm.GroupId == groupId)
            .ToListAsync(ct);

        if (members.Count != memberIds.Count)
        {
            return Result<List<ScheduleAssignmentDto>>.Failure(
                Error.UnprocessableEntity("One or more members not found in the specified group"));
        }

        // Create assignments
        var assignments = new List<VolunteerScheduleAssignment>();
        foreach (var member in members)
        {
            foreach (var date in request.Dates)
            {
                var assignment = new VolunteerScheduleAssignment
                {
                    GroupMemberId = member.Id,
                    ScheduleId = scheduleId,
                    AssignedDate = date,
                    Status = VolunteerScheduleStatus.Scheduled,
                    CreatedDateTime = DateTime.UtcNow
                };
                assignments.Add(assignment);
            }
        }

        context.VolunteerScheduleAssignments.AddRange(assignments);

        // Rely on database unique constraint to prevent double-booking
        // Catch constraint violation and return proper error
        try
        {
            await context.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex)
        {
            // Check if this is a unique constraint violation (double-booking)
            // PostgreSQL error code 23505 for unique constraint violation
            var isUniqueViolation = ex.InnerException?.Message.Contains("duplicate key") == true ||
                                     ex.InnerException?.Message.Contains("unique constraint") == true ||
                                     ex.InnerException?.Message.Contains("23505") == true;

            if (isUniqueViolation)
            {
                logger.LogWarning(
                    "Double-booking detected for group {GroupId}, schedule {ScheduleId}: {Message}",
                    groupId, scheduleId, ex.InnerException?.Message);

                return Result<List<ScheduleAssignmentDto>>.Failure(
                    Error.UnprocessableEntity("One or more assignments already exist for the specified dates. Please check for existing assignments."));
            }

            // Re-throw if it's a different type of database error
            throw;
        }

        logger.LogInformation(
            "Created {Count} volunteer schedule assignments for group {GroupId}, schedule {ScheduleId}",
            assignments.Count, groupId, scheduleId);

        // Build DTOs
        var dtos = assignments.Select(a =>
        {
            var member = members.First(m => m.Id == a.GroupMemberId);
            return new ScheduleAssignmentDto
            {
                IdKey = a.IdKey,
                MemberIdKey = member.IdKey,
                MemberName = member.Person?.FullName ?? "Unknown",
                ScheduleIdKey = schedule.IdKey,
                ScheduleName = schedule.Name,
                AssignedDate = a.AssignedDate,
                Status = a.Status,
                DeclineReason = a.DeclineReason,
                RespondedDateTime = a.RespondedDateTime,
                Note = a.Note
            };
        }).ToList();

        return Result<List<ScheduleAssignmentDto>>.Success(dtos);
    }

    public async Task<List<ScheduleAssignmentDto>> GetAssignmentsAsync(
        string groupIdKey,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken ct = default)
    {
        if (!IdKeyHelper.TryDecode(groupIdKey, out int groupId))
        {
            logger.LogWarning("Invalid group IdKey: {IdKey}", groupIdKey);
            return new List<ScheduleAssignmentDto>();
        }

        var assignments = await context.VolunteerScheduleAssignments
            .AsNoTracking()
            .Include(vsa => vsa.GroupMember)
                .ThenInclude(gm => gm!.Person)
            .Include(vsa => vsa.Schedule)
            .Where(vsa => vsa.GroupMember!.GroupId == groupId &&
                         vsa.AssignedDate >= startDate &&
                         vsa.AssignedDate <= endDate)
            .OrderBy(vsa => vsa.AssignedDate)
            .ThenBy(vsa => vsa.GroupMember!.Person!.LastName)
            .ToListAsync(ct);

        var dtos = assignments
            .Where(a => a.GroupMember?.Person != null && a.Schedule != null)
            .Select(a => new ScheduleAssignmentDto
            {
                IdKey = a.IdKey,
                MemberIdKey = a.GroupMember!.IdKey,
                MemberName = a.GroupMember.Person!.FullName,
                ScheduleIdKey = a.Schedule!.IdKey,
                ScheduleName = a.Schedule.Name,
                AssignedDate = a.AssignedDate,
                Status = a.Status,
                DeclineReason = a.DeclineReason,
                RespondedDateTime = a.RespondedDateTime,
                Note = a.Note
            })
            .ToList();

        logger.LogInformation(
            "Retrieved {Count} assignments for group {GroupId} from {StartDate} to {EndDate}",
            dtos.Count, groupIdKey, startDate, endDate);

        return dtos;
    }

    public async Task<Result<ScheduleAssignmentDto>> UpdateAssignmentStatusAsync(
        string assignmentIdKey,
        UpdateAssignmentStatusRequest request,
        int currentPersonId,
        CancellationToken ct = default)
    {
        if (!IdKeyHelper.TryDecode(assignmentIdKey, out int assignmentId))
        {
            return Result<ScheduleAssignmentDto>.Failure(Error.NotFound("VolunteerScheduleAssignment", assignmentIdKey));
        }

        var assignment = await context.VolunteerScheduleAssignments
            .Include(vsa => vsa.GroupMember)
                .ThenInclude(gm => gm!.Person)
            .Include(vsa => vsa.Schedule)
            .FirstOrDefaultAsync(vsa => vsa.Id == assignmentId, ct);

        if (assignment == null)
        {
            return Result<ScheduleAssignmentDto>.Failure(Error.NotFound("VolunteerScheduleAssignment", assignmentIdKey));
        }

        // Verify ownership: assignment must belong to the current user
        if (assignment.GroupMember?.PersonId != currentPersonId)
        {
            logger.LogWarning(
                "Unauthorized attempt to update assignment {AssignmentId} by person {PersonId}",
                assignmentId, currentPersonId);

            return Result<ScheduleAssignmentDto>.Failure(Error.NotFound("VolunteerScheduleAssignment", assignmentIdKey));
        }

        // Validate decline reason if status is Declined
        if (request.Status == VolunteerScheduleStatus.Declined && string.IsNullOrWhiteSpace(request.DeclineReason))
        {
            return Result<ScheduleAssignmentDto>.Failure(
                Error.UnprocessableEntity("Decline reason is required when declining an assignment"));
        }

        var previousStatus = assignment.Status;
        assignment.Status = request.Status;
        assignment.DeclineReason = request.DeclineReason;
        assignment.RespondedDateTime = DateTime.UtcNow;
        assignment.ModifiedDateTime = DateTime.UtcNow;

        await context.SaveChangesAsync(ct);

        logger.LogInformation(
            "Updated assignment {AssignmentId} status from {OldStatus} to {NewStatus}",
            assignmentId, previousStatus, request.Status);

        // Build DTO
        if (assignment.GroupMember?.Person == null || assignment.Schedule == null)
        {
            return Result<ScheduleAssignmentDto>.Failure(
                Error.UnprocessableEntity("Assignment missing required relationships"));
        }

        var dto = new ScheduleAssignmentDto
        {
            IdKey = assignment.IdKey,
            MemberIdKey = assignment.GroupMember.IdKey,
            MemberName = assignment.GroupMember.Person.FullName,
            ScheduleIdKey = assignment.Schedule.IdKey,
            ScheduleName = assignment.Schedule.Name,
            AssignedDate = assignment.AssignedDate,
            Status = assignment.Status,
            DeclineReason = assignment.DeclineReason,
            RespondedDateTime = assignment.RespondedDateTime,
            Note = assignment.Note
        };

        return Result<ScheduleAssignmentDto>.Success(dto);
    }

    public async Task<List<MyScheduleDto>> GetMyScheduleAsync(
        int personId,
        DateOnly? startDate = null,
        DateOnly? endDate = null,
        CancellationToken ct = default)
    {
        var start = startDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var end = endDate ?? start.AddDays(90);

        var assignments = await context.VolunteerScheduleAssignments
            .AsNoTracking()
            .Include(vsa => vsa.GroupMember)
                .ThenInclude(gm => gm!.Person)
            .Include(vsa => vsa.Schedule)
            .Where(vsa => vsa.GroupMember != null &&
                         vsa.GroupMember.Person != null &&
                         vsa.Schedule != null &&
                         vsa.GroupMember.PersonId == personId &&
                         vsa.AssignedDate >= start &&
                         vsa.AssignedDate <= end)
            .OrderBy(vsa => vsa.AssignedDate)
            .ThenBy(vsa => vsa.Schedule!.Name)
            .ToListAsync(ct);

        // Group by date
        var grouped = assignments
            .GroupBy(a => a.AssignedDate)
            .Select(g => new MyScheduleDto
            {
                Date = g.Key,
                Assignments = g.Select(a => new ScheduleAssignmentDto
                {
                    IdKey = a.IdKey,
                    MemberIdKey = a.GroupMember!.IdKey,
                    MemberName = a.GroupMember.Person!.FullName,
                    ScheduleIdKey = a.Schedule!.IdKey,
                    ScheduleName = a.Schedule.Name,
                    AssignedDate = a.AssignedDate,
                    Status = a.Status,
                    DeclineReason = a.DeclineReason,
                    RespondedDateTime = a.RespondedDateTime,
                    Note = a.Note
                }).ToList()
            })
            .OrderBy(m => m.Date)
            .ToList();

        logger.LogInformation(
            "Retrieved schedule for person {PersonId}: {Count} dates with assignments from {StartDate} to {EndDate}",
            personId, grouped.Count, start, end);

        return grouped;
    }
}
