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
/// Service for managing follow-up tasks for first-time visitors and attendees.
/// Tracks status, assignment, and completion of follow-up activities.
/// </summary>
public class FollowUpService(
    IApplicationDbContext context,
    ILogger<FollowUpService> logger) : IFollowUpService
{
    public async Task<FollowUp> CreateFollowUpAsync(
        int personId,
        int? attendanceId,
        CancellationToken ct = default)
    {
        var followUp = new FollowUp
        {
            PersonId = personId,
            AttendanceId = attendanceId,
            Status = FollowUpStatus.Pending,
            CreatedDateTime = DateTime.UtcNow
        };

        context.FollowUps.Add(followUp);
        await context.SaveChangesAsync(ct);

        logger.LogInformation(
            "Created follow-up {FollowUpId} for person {PersonId}",
            followUp.Id, personId);

        return followUp;
    }

    public async Task<IReadOnlyList<FollowUpDto>> GetPendingFollowUpsAsync(
        string? assignedToIdKey = null,
        CancellationToken ct = default)
    {
        int? assignedToId = null;
        if (!string.IsNullOrWhiteSpace(assignedToIdKey))
        {
            if (!IdKeyHelper.TryDecode(assignedToIdKey, out int decodedId))
            {
                logger.LogWarning("Invalid assignedTo IdKey provided: {IdKey}", assignedToIdKey);
                return Array.Empty<FollowUpDto>();
            }
            assignedToId = decodedId;
        }

        var query = context.FollowUps
            .AsNoTracking()
            .Include(f => f.Person)
            .Include(f => f.AssignedToPerson)
            .Include(f => f.Attendance)
            .Where(f => f.Status == FollowUpStatus.Pending ||
                       f.Status == FollowUpStatus.Contacted ||
                       f.Status == FollowUpStatus.NoResponse);

        if (assignedToId.HasValue)
        {
            query = query.Where(f => f.AssignedToPersonId == assignedToId.Value);
        }

        var followUps = await query
            .OrderBy(f => f.CreatedDateTime)
            .ToListAsync(ct);

        var results = followUps
            .Where(f => f.Person != null)
            .Select(f => new FollowUpDto
            {
                IdKey = f.IdKey,
                PersonIdKey = f.Person!.IdKey,
                PersonName = f.Person.FullName,
                AttendanceIdKey = f.Attendance?.IdKey,
                Status = f.Status,
                Notes = f.Notes,
                AssignedToIdKey = f.AssignedToPerson?.IdKey,
                AssignedToName = f.AssignedToPerson?.FullName,
                ContactedDateTime = f.ContactedDateTime,
                CompletedDateTime = f.CompletedDateTime,
                CreatedDateTime = f.CreatedDateTime
            })
            .ToList();

        logger.LogInformation(
            "Retrieved {Count} pending follow-ups{Filter}",
            results.Count,
            assignedToId.HasValue ? $" for assignee {assignedToIdKey}" : "");

        return results;
    }

    public async Task<FollowUpDto?> GetByIdKeyAsync(
        string idKey,
        CancellationToken ct = default)
    {
        if (!IdKeyHelper.TryDecode(idKey, out int id))
        {
            logger.LogWarning("Invalid follow-up IdKey: {IdKey}", idKey);
            return null;
        }

        var followUp = await context.FollowUps
            .AsNoTracking()
            .Include(f => f.Person)
            .Include(f => f.AssignedToPerson)
            .Include(f => f.Attendance)
            .FirstOrDefaultAsync(f => f.Id == id, ct);

        if (followUp?.Person == null)
        {
            return null;
        }

        return new FollowUpDto
        {
            IdKey = followUp.IdKey,
            PersonIdKey = followUp.Person.IdKey,
            PersonName = followUp.Person.FullName,
            AttendanceIdKey = followUp.Attendance?.IdKey,
            Status = followUp.Status,
            Notes = followUp.Notes,
            AssignedToIdKey = followUp.AssignedToPerson?.IdKey,
            AssignedToName = followUp.AssignedToPerson?.FullName,
            ContactedDateTime = followUp.ContactedDateTime,
            CompletedDateTime = followUp.CompletedDateTime,
            CreatedDateTime = followUp.CreatedDateTime
        };
    }

    public async Task<Result<FollowUpDto>> UpdateStatusAsync(
        string idKey,
        FollowUpStatus status,
        string? notes = null,
        CancellationToken ct = default)
    {
        if (!IdKeyHelper.TryDecode(idKey, out int id))
        {
            return Result<FollowUpDto>.Failure(Error.NotFound("FollowUp", idKey));
        }

        var followUp = await context.FollowUps
            .Include(f => f.Person)
            .Include(f => f.AssignedToPerson)
            .Include(f => f.Attendance)
            .FirstOrDefaultAsync(f => f.Id == id, ct);

        if (followUp == null)
        {
            return Result<FollowUpDto>.Failure(Error.NotFound("FollowUp", idKey));
        }

        var previousStatus = followUp.Status;
        followUp.Status = status;
        followUp.ModifiedDateTime = DateTime.UtcNow;

        // Update notes if provided
        if (!string.IsNullOrWhiteSpace(notes))
        {
            followUp.Notes = string.IsNullOrWhiteSpace(followUp.Notes)
                ? notes
                : $"{followUp.Notes}\n{notes}";
        }

        // Set timestamps based on status
        if (status == FollowUpStatus.Contacted && !followUp.ContactedDateTime.HasValue)
        {
            followUp.ContactedDateTime = DateTime.UtcNow;
        }

        if ((status == FollowUpStatus.Connected || status == FollowUpStatus.Declined) &&
            !followUp.CompletedDateTime.HasValue)
        {
            followUp.CompletedDateTime = DateTime.UtcNow;
        }

        await context.SaveChangesAsync(ct);

        logger.LogInformation(
            "Updated follow-up {FollowUpId} status from {OldStatus} to {NewStatus}",
            followUp.Id, previousStatus, status);

        // Build DTO directly from tracked entity (navigation properties already loaded)
        if (followUp.Person == null)
        {
            return Result<FollowUpDto>.Failure(Error.UnprocessableEntity("Follow-up missing required Person relationship"));
        }

        var dto = new FollowUpDto
        {
            IdKey = followUp.IdKey,
            PersonIdKey = followUp.Person.IdKey,
            PersonName = followUp.Person.FullName,
            AttendanceIdKey = followUp.Attendance?.IdKey,
            Status = followUp.Status,
            Notes = followUp.Notes,
            AssignedToIdKey = followUp.AssignedToPerson?.IdKey,
            AssignedToName = followUp.AssignedToPerson?.FullName,
            ContactedDateTime = followUp.ContactedDateTime,
            CompletedDateTime = followUp.CompletedDateTime,
            CreatedDateTime = followUp.CreatedDateTime
        };

        return Result<FollowUpDto>.Success(dto);
    }

    public async Task<Result> AssignFollowUpAsync(
        string idKey,
        string assignedToIdKey,
        CancellationToken ct = default)
    {
        if (!IdKeyHelper.TryDecode(idKey, out int id))
        {
            return Result.Failure(Error.NotFound("FollowUp", idKey));
        }

        if (!IdKeyHelper.TryDecode(assignedToIdKey, out int assignedToId))
        {
            return Result.Failure(Error.NotFound("Person", assignedToIdKey));
        }

        var followUp = await context.FollowUps
            .FirstOrDefaultAsync(f => f.Id == id, ct);

        if (followUp == null)
        {
            return Result.Failure(Error.NotFound("FollowUp", idKey));
        }

        // Verify the assigned person exists
        var assignedPerson = await context.People
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == assignedToId, ct);

        if (assignedPerson == null)
        {
            return Result.Failure(Error.NotFound("Person", assignedToIdKey));
        }

        followUp.AssignedToPersonId = assignedToId;
        followUp.ModifiedDateTime = DateTime.UtcNow;

        await context.SaveChangesAsync(ct);

        logger.LogInformation(
            "Assigned follow-up {FollowUpId} to person {PersonId}",
            followUp.Id, assignedToId);

        return Result.Success();
    }
}
