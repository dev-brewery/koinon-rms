using Koinon.Application.DTOs.GroupMeeting;
using Koinon.Application.Interfaces;
using Koinon.Domain.Data;
using Koinon.Domain.Entities;
using Koinon.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Koinon.Application.Services;

/// <summary>
/// Service for managing group meeting RSVPs.
/// </summary>
public class GroupMeetingService(
    IApplicationDbContext context,
    ILogger<GroupMeetingService> logger)
{
    /// <summary>
    /// Send RSVP requests to all active members of a group for a specific meeting date.
    /// Creates RSVP records with NoResponse status for members who don't already have an RSVP.
    /// </summary>
    public async Task<int> SendRsvpRequestsAsync(string groupIdKey, DateOnly meetingDate, CancellationToken ct = default)
    {
        if (!IdKeyHelper.TryDecode(groupIdKey, out int groupId))
        {
            throw new ArgumentException("Invalid group IdKey", nameof(groupIdKey));
        }

        // Get all active group members
        var activeMembers = await context.GroupMembers
            .Where(gm => gm.GroupId == groupId && gm.GroupMemberStatus == GroupMemberStatus.Active)
            .Select(gm => gm.PersonId)
            .ToListAsync(ct);

        if (activeMembers.Count == 0)
        {
            logger.LogWarning("No active members found for group {GroupId}", groupId);
            return 0;
        }

        // Get existing RSVPs for this meeting
        var existingRsvps = await context.GroupMeetingRsvps
            .Where(r => r.GroupId == groupId && r.MeetingDate == meetingDate)
            .Select(r => r.PersonId)
            .ToListAsync(ct);

        // Create RSVP records for members who don't already have one
        var membersNeedingRsvp = activeMembers.Except(existingRsvps).ToList();

        var newRsvps = membersNeedingRsvp.Select(personId => new GroupMeetingRsvp
        {
            GroupId = groupId,
            MeetingDate = meetingDate,
            PersonId = personId,
            Status = RsvpStatus.NoResponse,
            CreatedDateTime = DateTime.UtcNow
        }).ToList();

        if (newRsvps.Count > 0)
        {
            context.GroupMeetingRsvps.AddRange(newRsvps);
            await context.SaveChangesAsync(ct);

            logger.LogInformation(
                "Created {Count} RSVP requests for group {GroupId} meeting on {MeetingDate}",
                newRsvps.Count, groupId, meetingDate);
        }

        return newRsvps.Count;
    }

    /// <summary>
    /// Get RSVP summary for a specific meeting.
    /// </summary>
    public async Task<MeetingRsvpSummaryDto?> GetRsvpsAsync(string groupIdKey, DateOnly meetingDate, CancellationToken ct = default)
    {
        if (!IdKeyHelper.TryDecode(groupIdKey, out int groupId))
        {
            return null;
        }

        var rsvps = await context.GroupMeetingRsvps
            .AsNoTracking()
            .Include(r => r.Person)
            .Where(r => r.GroupId == groupId && r.MeetingDate == meetingDate)
            .OrderBy(r => r.Person!.LastName)
            .ThenBy(r => r.Person!.FirstName)
            .Select(r => new RsvpDto
            {
                IdKey = IdKeyHelper.Encode(r.Id),
                PersonIdKey = IdKeyHelper.Encode(r.PersonId),
                PersonName = r.Person!.FullName,
                Status = r.Status,
                Note = r.Note,
                RespondedDateTime = r.RespondedDateTime
            })
            .ToListAsync(ct);

        var summary = new MeetingRsvpSummaryDto
        {
            MeetingDate = meetingDate,
            Attending = rsvps.Count(r => r.Status == RsvpStatus.Attending),
            NotAttending = rsvps.Count(r => r.Status == RsvpStatus.NotAttending),
            Maybe = rsvps.Count(r => r.Status == RsvpStatus.Maybe),
            NoResponse = rsvps.Count(r => r.Status == RsvpStatus.NoResponse),
            TotalInvited = rsvps.Count,
            Rsvps = rsvps
        };

        return summary;
    }

    /// <summary>
    /// Update a person's RSVP for a specific meeting.
    /// </summary>
    public async Task<bool> UpdateRsvpAsync(
        int personId,
        string groupIdKey,
        DateOnly meetingDate,
        RsvpStatus status,
        string? note = null,
        CancellationToken ct = default)
    {
        if (!IdKeyHelper.TryDecode(groupIdKey, out int groupId))
        {
            return false;
        }

        // Verify person is a member of the group
        var isMember = await context.GroupMembers
            .AnyAsync(gm => gm.GroupId == groupId && gm.PersonId == personId && gm.GroupMemberStatus == GroupMemberStatus.Active, ct);

        if (!isMember)
        {
            return false;
        }

        try
        {
            var rsvp = await context.GroupMeetingRsvps
                .FirstOrDefaultAsync(r => r.GroupId == groupId && r.MeetingDate == meetingDate && r.PersonId == personId, ct);

            if (rsvp == null)
            {
                rsvp = new GroupMeetingRsvp
                {
                    GroupId = groupId,
                    MeetingDate = meetingDate,
                    PersonId = personId,
                    Status = status,
                    Note = note,
                    RespondedDateTime = DateTime.UtcNow,
                    CreatedDateTime = DateTime.UtcNow
                };
                context.GroupMeetingRsvps.Add(rsvp);
            }
            else
            {
                rsvp.Status = status;
                rsvp.Note = note;
                rsvp.RespondedDateTime = DateTime.UtcNow;
                rsvp.ModifiedDateTime = DateTime.UtcNow;
            }

            await context.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // Race condition: another request created the RSVP first
            // Retry with update
            var rsvp = await context.GroupMeetingRsvps
                .FirstOrDefaultAsync(r => r.GroupId == groupId && r.MeetingDate == meetingDate && r.PersonId == personId, ct);

            if (rsvp == null)
            {
                return false;
            }

            rsvp.Status = status;
            rsvp.Note = note;
            rsvp.RespondedDateTime = DateTime.UtcNow;
            rsvp.ModifiedDateTime = DateTime.UtcNow;
            await context.SaveChangesAsync(ct);
        }

        logger.LogInformation(
            "Person {PersonId} updated RSVP for group {GroupId} meeting on {MeetingDate} to {Status}",
            personId, groupId, meetingDate, status);

        return true;
    }

    /// <summary>
    /// Get all pending and upcoming RSVPs for a person.
    /// </summary>
    public async Task<List<MyRsvpDto>> GetMyRsvpsAsync(
        int personId,
        DateOnly? startDate = null,
        DateOnly? endDate = null,
        CancellationToken ct = default)
    {
        var query = context.GroupMeetingRsvps
            .AsNoTracking()
            .Include(r => r.Group)
            .Where(r => r.PersonId == personId);

        // Default to today onwards if no start date specified
        var fromDate = startDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        query = query.Where(r => r.MeetingDate >= fromDate);

        if (endDate.HasValue)
        {
            query = query.Where(r => r.MeetingDate <= endDate.Value);
        }

        var rsvps = await query
            .OrderBy(r => r.MeetingDate)
            .ThenBy(r => r.Group!.Name)
            .Select(r => new MyRsvpDto
            {
                GroupIdKey = IdKeyHelper.Encode(r.GroupId),
                GroupName = r.Group!.Name,
                MeetingDate = r.MeetingDate,
                Status = r.Status,
                Note = r.Note
            })
            .ToListAsync(ct);

        return rsvps;
    }

    /// <summary>
    /// Check if a person is a leader of the specified group.
    /// </summary>
    public async Task<bool> IsGroupLeaderAsync(int personId, string groupIdKey, CancellationToken ct = default)
    {
        if (!IdKeyHelper.TryDecode(groupIdKey, out int groupId))
        {
            return false;
        }

        return await context.GroupMembers
            .AnyAsync(gm =>
                gm.GroupId == groupId &&
                gm.PersonId == personId &&
                gm.GroupMemberStatus == GroupMemberStatus.Active &&
                gm.GroupRole != null && gm.GroupRole.IsLeader, ct);
    }
}
