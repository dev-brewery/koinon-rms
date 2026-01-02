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
/// Service for communication management operations.
/// </summary>
public class CommunicationService(
    IApplicationDbContext context,
    IMapper mapper,
    IUserContext userContext,
    ILogger<CommunicationService> logger) : ICommunicationService
{
    private const string RoleStaff = "Staff";
    private const string RoleAdmin = "Admin";
    public async Task<CommunicationDto?> GetByIdKeyAsync(string idKey, CancellationToken ct = default)
    {
        if (!IdKeyHelper.TryDecode(idKey, out int id))
        {
            return null;
        }

        var communication = await context.Communications
            .AsNoTracking()
            .Include(c => c.Recipients)
            .FirstOrDefaultAsync(c => c.Id == id, ct);

        if (communication is null)
        {
            return null;
        }

        // AUTHORIZATION: Verify user created the communication OR is Staff/Admin
        if (!await UserCanAccessCommunicationAsync(communication, ct))
        {
            logger.LogWarning(
                "Unauthorized access attempt to communication {CommunicationId} by user {UserId}",
                idKey,
                userContext.CurrentPersonId);
            return null;
        }

        return mapper.Map<CommunicationDto>(communication);
    }

    public async Task<PagedResult<CommunicationSummaryDto>> SearchAsync(
        int page = 1,
        int pageSize = 20,
        string? status = null,
        CancellationToken ct = default)
    {
        var query = context.Communications
            .AsNoTracking();

        // AUTHORIZATION: Non-staff users can only see their own communications
        if (!userContext.IsInRole(RoleStaff) && !userContext.IsInRole(RoleAdmin))
        {
            var currentUserAliasId = await GetCurrentPersonAliasIdAsync(ct);
            query = query.Where(c => c.CreatedByPersonAliasId == currentUserAliasId);
        }

        // Filter by status if provided
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<CommunicationStatus>(status, true, out var statusEnum))
        {
            query = query.Where(c => c.Status == statusEnum);
        }

        // Get total count
        var totalCount = await query.CountAsync(ct);

        // Get paged results
        var communications = await query
            .OrderByDescending(c => c.CreatedDateTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var items = communications.Select(c => mapper.Map<CommunicationSummaryDto>(c)).ToList();

        return new PagedResult<CommunicationSummaryDto>(
            items,
            totalCount,
            page,
            pageSize);
    }

    public async Task<Result<CommunicationDto>> CreateAsync(
        CreateCommunicationDto dto,
        CancellationToken ct = default)
    {
        // Parse communication type
        if (!Enum.TryParse<CommunicationType>(dto.CommunicationType, true, out var communicationType))
        {
            return Result<CommunicationDto>.Failure(
                new Error("INVALID_TYPE", $"Invalid communication type: {dto.CommunicationType}"));
        }

        // BLOCKER #2: Validate group count before decode
        if (dto.GroupIdKeys.Count > 50)
        {
            return Result<CommunicationDto>.Failure(
                Error.UnprocessableEntity("Cannot specify more than 50 groups"));
        }

        // Decode group IdKeys
        var groupIds = new List<int>();
        foreach (var groupIdKey in dto.GroupIdKeys)
        {
            if (!IdKeyHelper.TryDecode(groupIdKey, out int groupId))
            {
                return Result<CommunicationDto>.Failure(
                    Error.NotFound("Group", groupIdKey));
            }
            groupIds.Add(groupId);
        }

        // Authorize access to groups
        var authResult = await AuthorizeGroupAccessAsync(groupIds, ct);
        if (!authResult.IsSuccess)
        {
            return Result<CommunicationDto>.Failure(authResult.Error!);
        }

        // Get recipients from groups
        var recipients = await ResolveRecipientsAsync(groupIds, communicationType, ct);
        if (recipients.Count == 0)
        {
            return Result<CommunicationDto>.Failure(
                new Error("NO_RECIPIENTS", "No active members found in the specified groups"));
        }

        // Get PersonAliasId for audit trail
        var personAliasId = await GetCurrentPersonAliasIdAsync(ct);

        // Determine initial status based on ScheduledDateTime
        var status = CommunicationStatus.Draft;
        DateTime? scheduledDateTime = null;

        if (dto.ScheduledDateTime.HasValue)
        {
            // Validate scheduled time is in the future (minimum 5 minutes)
            var minScheduledTime = DateTime.UtcNow.AddMinutes(5);
            if (dto.ScheduledDateTime.Value <= minScheduledTime)
            {
                return Result<CommunicationDto>.Failure(
                    Error.UnprocessableEntity(
                        $"ScheduledDateTime must be at least 5 minutes in the future (minimum: {minScheduledTime:O})"));
            }

            // Validate scheduled time is not too far in the future (maximum 1 year)
            var maxScheduledTime = DateTime.UtcNow.AddYears(1);
            if (dto.ScheduledDateTime.Value > maxScheduledTime)
            {
                return Result<CommunicationDto>.Failure(
                    Error.UnprocessableEntity(
                        $"ScheduledDateTime cannot be more than 1 year in the future (maximum: {maxScheduledTime:O})"));
            }

            status = CommunicationStatus.Scheduled;
            scheduledDateTime = dto.ScheduledDateTime.Value;
        }

        // Create communication
        var communication = new Communication
        {
            CommunicationType = communicationType,
            Status = status,
            Subject = dto.Subject,
            Body = dto.Body,
            FromEmail = dto.FromEmail,
            FromName = dto.FromName,
            ReplyToEmail = dto.ReplyToEmail,
            ScheduledDateTime = scheduledDateTime,
            Note = dto.Note,
            CreatedDateTime = DateTime.UtcNow,
            CreatedByPersonAliasId = personAliasId,
            RecipientCount = recipients.Count,
            DeliveredCount = 0,
            FailedCount = 0,
            OpenedCount = 0
        };

        // Add recipients to communication
        foreach (var recipient in recipients)
        {
            communication.Recipients.Add(recipient);
        }

        context.Communications.Add(communication);
        await context.SaveChangesAsync(ct);

        logger.LogInformation(
            "Created communication {CommunicationId} with {RecipientCount} recipients",
            communication.IdKey,
            communication.RecipientCount);

        return Result<CommunicationDto>.Success(mapper.Map<CommunicationDto>(communication));
    }

    public async Task<Result<CommunicationDto>> UpdateAsync(
        string idKey,
        UpdateCommunicationDto dto,
        CancellationToken ct = default)
    {
        if (!IdKeyHelper.TryDecode(idKey, out int id))
        {
            return Result<CommunicationDto>.Failure(Error.NotFound("Communication", idKey));
        }

        var communication = await context.Communications
            .Include(c => c.Recipients)
            .FirstOrDefaultAsync(c => c.Id == id, ct);

        if (communication is null)
        {
            return Result<CommunicationDto>.Failure(Error.NotFound("Communication", idKey));
        }

        // AUTHORIZATION: Verify user owns the communication OR is Staff/Admin
        if (!await UserCanAccessCommunicationAsync(communication, ct))
        {
            return Result<CommunicationDto>.Failure(
                Error.Forbidden("User does not have permission to update this communication"));
        }

        // Only allow updates if status is Draft
        if (communication.Status != CommunicationStatus.Draft)
        {
            return Result<CommunicationDto>.Failure(
                Error.UnprocessableEntity("Only draft communications can be updated"));
        }

        // Update properties
        if (dto.Subject is not null)
        {
            communication.Subject = dto.Subject;
        }

        if (dto.Body is not null)
        {
            communication.Body = dto.Body;
        }

        if (dto.FromEmail is not null)
        {
            communication.FromEmail = dto.FromEmail;
        }

        if (dto.FromName is not null)
        {
            communication.FromName = dto.FromName;
        }

        if (dto.ReplyToEmail is not null)
        {
            communication.ReplyToEmail = dto.ReplyToEmail;
        }

        if (dto.Note is not null)
        {
            communication.Note = dto.Note;
        }

        communication.ModifiedDateTime = DateTime.UtcNow;
        communication.ModifiedByPersonAliasId = await GetCurrentPersonAliasIdAsync(ct);

        await context.SaveChangesAsync(ct);

        logger.LogInformation("Updated communication {CommunicationId}", communication.IdKey);

        return Result<CommunicationDto>.Success(mapper.Map<CommunicationDto>(communication));
    }

    public async Task<Result> DeleteAsync(string idKey, CancellationToken ct = default)
    {
        if (!IdKeyHelper.TryDecode(idKey, out int id))
        {
            return Result.Failure(Error.NotFound("Communication", idKey));
        }

        var communication = await context.Communications
            .FirstOrDefaultAsync(c => c.Id == id, ct);

        if (communication is null)
        {
            return Result.Failure(Error.NotFound("Communication", idKey));
        }

        // AUTHORIZATION: Verify user owns the communication OR is Staff/Admin
        if (!await UserCanAccessCommunicationAsync(communication, ct))
        {
            return Result.Failure(
                Error.Forbidden("User does not have permission to delete this communication"));
        }

        // Only allow deletion if status is Draft
        if (communication.Status != CommunicationStatus.Draft)
        {
            return Result.Failure(
                Error.UnprocessableEntity("Only draft communications can be deleted"));
        }

        context.Communications.Remove(communication);
        await context.SaveChangesAsync(ct);

        logger.LogInformation("Deleted communication {CommunicationId}", idKey);

        return Result.Success();
    }

    public async Task<Result<CommunicationDto>> SendAsync(string idKey, CancellationToken ct = default)
    {
        if (!IdKeyHelper.TryDecode(idKey, out int id))
        {
            return Result<CommunicationDto>.Failure(Error.NotFound("Communication", idKey));
        }

        var communication = await context.Communications
            .Include(c => c.Recipients)
            .FirstOrDefaultAsync(c => c.Id == id, ct);

        if (communication is null)
        {
            return Result<CommunicationDto>.Failure(Error.NotFound("Communication", idKey));
        }

        // AUTHORIZATION: Verify user owns the communication OR is Staff/Admin
        if (!await UserCanAccessCommunicationAsync(communication, ct))
        {
            return Result<CommunicationDto>.Failure(
                Error.Forbidden("User does not have permission to send this communication"));
        }

        // Only allow sending if status is Draft
        if (communication.Status != CommunicationStatus.Draft)
        {
            return Result<CommunicationDto>.Failure(
                Error.UnprocessableEntity("Only draft communications can be sent"));
        }

        if (communication.RecipientCount == 0)
        {
            return Result<CommunicationDto>.Failure(
                Error.UnprocessableEntity("Cannot send communication with no recipients"));
        }

        // BLOCKER #1: Re-authorize group access before sending
        // Get distinct group IDs from recipients and verify user still has leader permissions
        var groupIds = communication.Recipients
            .Where(r => r.GroupId.HasValue)
            .Select(r => r.GroupId!.Value)
            .Distinct()
            .ToList();

        if (groupIds.Any())
        {
            var authResult = await AuthorizeGroupAccessAsync(groupIds, ct);
            if (!authResult.IsSuccess)
            {
                return Result<CommunicationDto>.Failure(authResult.Error!);
            }
        }

        // Change status to Pending (will be picked up by background job in the future)
        communication.Status = CommunicationStatus.Pending;
        communication.ModifiedDateTime = DateTime.UtcNow;
        communication.ModifiedByPersonAliasId = await GetCurrentPersonAliasIdAsync(ct);

        await context.SaveChangesAsync(ct);

        logger.LogInformation(
            "Queued communication {CommunicationId} for sending to {RecipientCount} recipients",
            communication.IdKey,
            communication.RecipientCount);

        return Result<CommunicationDto>.Success(mapper.Map<CommunicationDto>(communication));
    }

    public async Task<Result<CommunicationDto>> ScheduleAsync(
        string idKey,
        DateTime scheduledDateTime,
        CancellationToken ct = default)
    {
        if (!IdKeyHelper.TryDecode(idKey, out int id))
        {
            return Result<CommunicationDto>.Failure(Error.NotFound("Communication", idKey));
        }

        var communication = await context.Communications
            .Include(c => c.Recipients)
            .FirstOrDefaultAsync(c => c.Id == id, ct);

        if (communication is null)
        {
            return Result<CommunicationDto>.Failure(Error.NotFound("Communication", idKey));
        }

        // AUTHORIZATION: Verify user owns the communication OR is Staff/Admin
        if (!await UserCanAccessCommunicationAsync(communication, ct))
        {
            return Result<CommunicationDto>.Failure(
                Error.Forbidden("User does not have permission to schedule this communication"));
        }

        // Only allow scheduling if status is Draft
        if (communication.Status != CommunicationStatus.Draft)
        {
            return Result<CommunicationDto>.Failure(
                Error.UnprocessableEntity("Only draft communications can be scheduled"));
        }

        if (communication.RecipientCount == 0)
        {
            return Result<CommunicationDto>.Failure(
                Error.UnprocessableEntity("Cannot schedule communication with no recipients"));
        }

        // Validate scheduled time is in the future (minimum 5 minutes)
        var minScheduledTime = DateTime.UtcNow.AddMinutes(5);
        if (scheduledDateTime <= minScheduledTime)
        {
            return Result<CommunicationDto>.Failure(
                Error.UnprocessableEntity(
                    $"ScheduledDateTime must be at least 5 minutes in the future (minimum: {minScheduledTime:O})"));
        }

        // Validate scheduled time is not too far in the future (maximum 1 year)
        var maxScheduledTime = DateTime.UtcNow.AddYears(1);
        if (scheduledDateTime > maxScheduledTime)
        {
            return Result<CommunicationDto>.Failure(
                Error.UnprocessableEntity(
                    $"ScheduledDateTime cannot be more than 1 year in the future (maximum: {maxScheduledTime:O})"));
        }

        // Re-authorize group access before scheduling
        var groupIds = communication.Recipients
            .Where(r => r.GroupId.HasValue)
            .Select(r => r.GroupId!.Value)
            .Distinct()
            .ToList();

        if (groupIds.Any())
        {
            var authResult = await AuthorizeGroupAccessAsync(groupIds, ct);
            if (!authResult.IsSuccess)
            {
                return Result<CommunicationDto>.Failure(authResult.Error!);
            }
        }

        // Update status and scheduled time
        communication.Status = CommunicationStatus.Scheduled;
        communication.ScheduledDateTime = scheduledDateTime;
        communication.ModifiedDateTime = DateTime.UtcNow;
        communication.ModifiedByPersonAliasId = await GetCurrentPersonAliasIdAsync(ct);

        await context.SaveChangesAsync(ct);

        logger.LogInformation(
            "Scheduled communication {CommunicationId} for {ScheduledDateTime}",
            communication.IdKey,
            scheduledDateTime);

        return Result<CommunicationDto>.Success(mapper.Map<CommunicationDto>(communication));
    }

    public async Task<Result<CommunicationDto>> CancelScheduleAsync(
        string idKey,
        CancellationToken ct = default)
    {
        if (!IdKeyHelper.TryDecode(idKey, out int id))
        {
            return Result<CommunicationDto>.Failure(Error.NotFound("Communication", idKey));
        }

        var communication = await context.Communications
            .Include(c => c.Recipients)
            .FirstOrDefaultAsync(c => c.Id == id, ct);

        if (communication is null)
        {
            return Result<CommunicationDto>.Failure(Error.NotFound("Communication", idKey));
        }

        // AUTHORIZATION: Verify user owns the communication OR is Staff/Admin
        if (!await UserCanAccessCommunicationAsync(communication, ct))
        {
            return Result<CommunicationDto>.Failure(
                Error.Forbidden("User does not have permission to cancel this scheduled communication"));
        }

        // Only allow canceling if status is Scheduled
        if (communication.Status != CommunicationStatus.Scheduled)
        {
            return Result<CommunicationDto>.Failure(
                Error.UnprocessableEntity("Only scheduled communications can be canceled"));
        }

        // Revert to Draft status and clear scheduled time
        communication.Status = CommunicationStatus.Draft;
        communication.ScheduledDateTime = null;
        communication.ModifiedDateTime = DateTime.UtcNow;
        communication.ModifiedByPersonAliasId = await GetCurrentPersonAliasIdAsync(ct);

        await context.SaveChangesAsync(ct);

        logger.LogInformation(
            "Canceled scheduled communication {CommunicationId}",
            communication.IdKey);

        return Result<CommunicationDto>.Success(mapper.Map<CommunicationDto>(communication));
    }

    /// <summary>
    /// Checks if the current user can access a communication (creator OR Staff/Admin).
    /// </summary>
    private async Task<bool> UserCanAccessCommunicationAsync(Communication communication, CancellationToken ct)
    {
        if (userContext.IsInRole(RoleStaff) || userContext.IsInRole(RoleAdmin))
        {
            return true;
        }

        var currentUserAliasId = await GetCurrentPersonAliasIdAsync(ct);
        return communication.CreatedByPersonAliasId == currentUserAliasId;
    }

    /// <summary>
    /// Gets the PersonAliasId for the current user.
    /// </summary>
    private async Task<int?> GetCurrentPersonAliasIdAsync(CancellationToken ct)
    {
        if (!userContext.CurrentPersonId.HasValue)
        {
            return null;
        }

        return await context.PersonAliases
            .Where(pa => pa.PersonId == userContext.CurrentPersonId.Value)
            .Select(pa => pa.Id)
            .FirstOrDefaultAsync(ct);
    }

    /// <summary>
    /// Authorizes that the user can send communications to all specified groups.
    /// User must be a leader of ALL groups OR be Staff/Admin.
    /// </summary>
    private async Task<Result> AuthorizeGroupAccessAsync(List<int> groupIds, CancellationToken ct)
    {
        // Staff/Admin can email any group
        if (userContext.IsInRole(RoleStaff) || userContext.IsInRole(RoleAdmin))
        {
            return Result.Success();
        }

        if (!userContext.CurrentPersonId.HasValue)
        {
            return Result.Failure(Error.Forbidden("User must be authenticated to send communications"));
        }

        // Batch query: Check if user is a leader in ALL specified groups
        var leaderGroupIds = await context.GroupMembers
            .Where(gm => groupIds.Contains(gm.GroupId)
                && gm.PersonId == userContext.CurrentPersonId.Value
                && gm.GroupRole != null
                && gm.GroupRole.IsLeader)
            .Select(gm => gm.GroupId)
            .Distinct()
            .ToListAsync(ct);

        if (leaderGroupIds.Count != groupIds.Count)
        {
            return Result.Failure(Error.Forbidden("User is not a leader of all specified groups"));
        }

        return Result.Success();
    }

    /// <summary>
    /// Resolves recipients from groups, deduplicates by person, and snapshots contact info.
    /// </summary>
    private async Task<List<CommunicationRecipient>> ResolveRecipientsAsync(
        List<int> groupIds,
        CommunicationType communicationType,
        CancellationToken ct)
    {
        // Get active members from the specified groups
        var groupMembers = await context.GroupMembers
            .AsNoTracking()
            .Include(gm => gm.Person)
                .ThenInclude(p => p!.PhoneNumbers)
            .Where(gm => groupIds.Contains(gm.GroupId) && gm.GroupMemberStatus == GroupMemberStatus.Active)
            .ToListAsync(ct);

        // CRITICAL #1: Build lookup dictionary to avoid N+1 query pattern
        // Create a dictionary mapping PersonId to the first GroupMember for that person
        var personLookup = groupMembers
            .GroupBy(gm => gm.PersonId)
            .ToDictionary(g => g.Key, g => g.First());

        var recipients = new List<CommunicationRecipient>();

        foreach (var kvp in personLookup)
        {
            var personId = kvp.Key;
            var member = kvp.Value;
            var person = member.Person;

            if (person is null)
            {
                continue;
            }

            // Get contact address based on communication type
            var address = communicationType == CommunicationType.Email
                ? person.Email
                : person.PhoneNumbers.FirstOrDefault()?.Number;

            if (string.IsNullOrWhiteSpace(address))
            {
                continue;
            }

            // Validate email addresses to prevent SMTP failures
            if (communicationType == CommunicationType.Email && !IsValidEmail(address))
            {
                logger.LogWarning(
                    "Skipping invalid email address for person {PersonId}: {Email}",
                    personId,
                    address);
                continue;
            }

            recipients.Add(new CommunicationRecipient
            {
                CommunicationId = 0, // Will be set by EF Core when added to communication
                PersonId = personId,
                Address = address,
                RecipientName = $"{person.FirstName} {person.LastName}",
                Status = CommunicationRecipientStatus.Pending,
                GroupId = member.GroupId,
                CreatedDateTime = DateTime.UtcNow
            });
        }

        return recipients;
    }

    /// <summary>
    /// Validates email address format using basic regex pattern.
    /// </summary>
    private static bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return false;
        }

        try
        {
            // Use MailAddress for basic validation
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch (FormatException)
        {
            // WARNING #2: Catch specific exception for malformed email addresses
            return false;
        }
    }
}
