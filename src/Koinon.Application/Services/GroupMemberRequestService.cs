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
/// Service for managing group membership requests.
/// Handles the workflow of users requesting to join groups and leaders approving/denying requests.
/// </summary>
public class GroupMemberRequestService(
    IApplicationDbContext context,
    IUserContext userContext,
    IMapper mapper,
    IValidator<SubmitMembershipRequestDto> submitValidator,
    IValidator<ProcessMembershipRequestDto> processValidator,
    ILogger<GroupMemberRequestService> logger) : IGroupMemberRequestService
{
    public async Task<Result<GroupMemberRequestDto>> SubmitRequestAsync(
        string groupIdKey,
        SubmitMembershipRequestDto request,
        CancellationToken ct = default)
    {
        // Validate input
        var validationResult = await submitValidator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
        {
            return Result<GroupMemberRequestDto>.Failure(Error.FromFluentValidation(validationResult));
        }

        // Ensure user is authenticated
        if (!userContext.CurrentPersonId.HasValue)
        {
            logger.LogWarning("Unauthenticated user attempted to submit membership request");
            return Result<GroupMemberRequestDto>.Failure(
                Error.Forbidden("You must be logged in to request group membership"));
        }

        // Decode and validate group
        if (!IdKeyHelper.TryDecode(groupIdKey, out int groupId))
        {
            return Result<GroupMemberRequestDto>.Failure(
                Error.NotFound("Group", groupIdKey));
        }

        var group = await context.Groups
            .Include(g => g.GroupType)
            .FirstOrDefaultAsync(g => g.Id == groupId && !g.IsArchived, ct);

        if (group is null)
        {
            return Result<GroupMemberRequestDto>.Failure(
                Error.NotFound("Group", groupIdKey));
        }

        var currentPersonId = userContext.CurrentPersonId.Value;

        // Check if person is already a member
        var isAlreadyMember = await context.GroupMembers
            .AnyAsync(gm => gm.GroupId == groupId
                && gm.PersonId == currentPersonId
                && gm.GroupMemberStatus == GroupMemberStatus.Active,
                ct);

        if (isAlreadyMember)
        {
            return Result<GroupMemberRequestDto>.Failure(
                Error.Conflict("You are already a member of this group"));
        }

        // Check for existing pending request
        var existingRequest = await context.GroupMemberRequests
            .FirstOrDefaultAsync(gmr => gmr.GroupId == groupId
                && gmr.PersonId == currentPersonId
                && gmr.Status == GroupMemberRequestStatus.Pending,
                ct);

        if (existingRequest is not null)
        {
            return Result<GroupMemberRequestDto>.Failure(
                Error.Conflict("You already have a pending request for this group"));
        }

        // Create the request
        var memberRequest = new GroupMemberRequest
        {
            GroupId = groupId,
            PersonId = currentPersonId,
            Status = GroupMemberRequestStatus.Pending,
            RequestNote = request.Note,
            CreatedDateTime = DateTime.UtcNow
        };

        context.GroupMemberRequests.Add(memberRequest);
        await context.SaveChangesAsync(ct);

        logger.LogInformation(
            "Person {PersonId} submitted membership request {RequestId} for group {GroupId}",
            currentPersonId, memberRequest.Id, groupId);

        // Reload with navigation properties for mapping
        var createdRequest = await context.GroupMemberRequests
            .AsNoTracking()
            .Include(gmr => gmr.Person)
            .Include(gmr => gmr.Group)
                .ThenInclude(g => g!.GroupType)
            .FirstAsync(gmr => gmr.Id == memberRequest.Id, ct);

        var dto = mapper.Map<GroupMemberRequestDto>(createdRequest);
        return Result<GroupMemberRequestDto>.Success(dto);
    }

    public async Task<Result<IReadOnlyList<GroupMemberRequestDto>>> GetPendingRequestsAsync(
        string groupIdKey,
        CancellationToken ct = default)
    {
        // Decode and validate group
        if (!IdKeyHelper.TryDecode(groupIdKey, out int groupId))
        {
            return Result<IReadOnlyList<GroupMemberRequestDto>>.Failure(
                Error.NotFound("Group", groupIdKey));
        }

        // Check if user is a leader
        if (!await IsGroupLeaderAsync(groupId, ct))
        {
            logger.LogWarning(
                "Person {PersonId} attempted to view requests for group {GroupId} without leader permissions",
                userContext.CurrentPersonId, groupId);
            return Result<IReadOnlyList<GroupMemberRequestDto>>.Failure(
                Error.Forbidden("Only group leaders can view membership requests"));
        }

        var requests = await context.GroupMemberRequests
            .AsNoTracking()
            .Include(gmr => gmr.Person)
            .Include(gmr => gmr.Group)
                .ThenInclude(g => g!.GroupType)
            .Where(gmr => gmr.GroupId == groupId
                && gmr.Status == GroupMemberRequestStatus.Pending)
            .OrderBy(gmr => gmr.CreatedDateTime)
            .ProjectTo<GroupMemberRequestDto>(mapper.ConfigurationProvider)
            .ToListAsync(ct);

        return Result<IReadOnlyList<GroupMemberRequestDto>>.Success(requests);
    }

    public async Task<Result<GroupMemberRequestDto>> ProcessRequestAsync(
        string groupIdKey,
        string requestIdKey,
        ProcessMembershipRequestDto request,
        CancellationToken ct = default)
    {
        // Validate input
        var validationResult = await processValidator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
        {
            return Result<GroupMemberRequestDto>.Failure(Error.FromFluentValidation(validationResult));
        }

        // Decode and validate group
        if (!IdKeyHelper.TryDecode(groupIdKey, out int groupId))
        {
            return Result<GroupMemberRequestDto>.Failure(
                Error.NotFound("Group", groupIdKey));
        }

        // Decode and validate request
        if (!IdKeyHelper.TryDecode(requestIdKey, out int requestId))
        {
            return Result<GroupMemberRequestDto>.Failure(
                Error.NotFound("Request", requestIdKey));
        }

        // Check if user is a leader
        if (!await IsGroupLeaderAsync(groupId, ct))
        {
            logger.LogWarning(
                "Person {PersonId} attempted to process request {RequestId} for group {GroupId} without leader permissions",
                userContext.CurrentPersonId, requestId, groupId);
            return Result<GroupMemberRequestDto>.Failure(
                Error.Forbidden("Only group leaders can process membership requests"));
        }

        // Load the request
        var memberRequest = await context.GroupMemberRequests
            .Include(gmr => gmr.Group)
                .ThenInclude(g => g!.GroupType)
            .FirstOrDefaultAsync(gmr => gmr.Id == requestId && gmr.GroupId == groupId, ct);

        if (memberRequest is null)
        {
            return Result<GroupMemberRequestDto>.Failure(
                Error.NotFound("Request", requestIdKey));
        }

        if (memberRequest.Status != GroupMemberRequestStatus.Pending)
        {
            return Result<GroupMemberRequestDto>.Failure(
                Error.Conflict("This request has already been processed"));
        }

        // Parse the status
        var status = request.Status switch
        {
            "Approved" => GroupMemberRequestStatus.Approved,
            "Denied" => GroupMemberRequestStatus.Denied,
            _ => GroupMemberRequestStatus.Pending // Should never happen due to validation
        };

        // Update the request
        memberRequest.Status = status;
        memberRequest.ResponseNote = request.Note;
        memberRequest.ProcessedByPersonId = userContext.CurrentPersonId;
        memberRequest.ProcessedDateTime = DateTime.UtcNow;
        memberRequest.ModifiedDateTime = DateTime.UtcNow;

        // If approved, create a group member
        if (status == GroupMemberRequestStatus.Approved)
        {
            // Get the default role for this group type
            var defaultRole = await context.GroupTypeRoles
                .FirstOrDefaultAsync(gtr => gtr.GroupTypeId == memberRequest.Group!.GroupTypeId
                    && !gtr.IsLeader, ct);

            if (defaultRole is null)
            {
                logger.LogWarning(
                    "No default role found for group type {GroupTypeId}, cannot create member",
                    memberRequest.Group!.GroupTypeId);
                return Result<GroupMemberRequestDto>.Failure(
                    Error.UnprocessableEntity("Cannot add member: No default role configured for this group type"));
            }

            // Check if person is already a member (edge case: might have been added manually)
            var existingMember = await context.GroupMembers
                .FirstOrDefaultAsync(gm => gm.GroupId == groupId
                    && gm.PersonId == memberRequest.PersonId
                    && gm.GroupMemberStatus == GroupMemberStatus.Active, ct);

            if (existingMember is null)
            {
                var newMember = new GroupMember
                {
                    GroupId = groupId,
                    PersonId = memberRequest.PersonId,
                    GroupRoleId = defaultRole.Id,
                    GroupMemberStatus = GroupMemberStatus.Active,
                    DateTimeAdded = DateTime.UtcNow,
                    CreatedDateTime = DateTime.UtcNow
                };

                context.GroupMembers.Add(newMember);

                logger.LogInformation(
                    "Created group member {MemberId} for person {PersonId} in group {GroupId} from approved request {RequestId}",
                    newMember.Id, memberRequest.PersonId, groupId, requestId);
            }
            else
            {
                logger.LogInformation(
                    "Person {PersonId} already a member of group {GroupId}, skipping member creation",
                    memberRequest.PersonId, groupId);
            }
        }

        await context.SaveChangesAsync(ct);

        logger.LogInformation(
            "Person {ProcessorId} {Status} membership request {RequestId} for group {GroupId}",
            userContext.CurrentPersonId, status, requestId, groupId);

        // Reload with all navigation properties
        var processedRequest = await context.GroupMemberRequests
            .AsNoTracking()
            .Include(gmr => gmr.Person)
            .Include(gmr => gmr.Group)
                .ThenInclude(g => g!.GroupType)
            .Include(gmr => gmr.ProcessedByPerson)
            .FirstAsync(gmr => gmr.Id == requestId, ct);

        var dto = mapper.Map<GroupMemberRequestDto>(processedRequest);
        return Result<GroupMemberRequestDto>.Success(dto);
    }

    /// <summary>
    /// Checks if the current user is a leader of the specified group.
    /// Staff and Admin roles automatically have leader permissions.
    /// </summary>
    private async Task<bool> IsGroupLeaderAsync(int groupId, CancellationToken ct)
    {
        var currentPersonId = userContext.CurrentPersonId;
        if (!currentPersonId.HasValue)
        {
            return false;
        }

        // Staff and Admin roles have leader permissions
        if (userContext.IsInRole("Staff") || userContext.IsInRole("Admin"))
        {
            return true;
        }

        // Check if user is an active leader of this group
        return await context.GroupMembers
            .AsNoTracking()
            .Include(gm => gm.GroupRole)
            .AnyAsync(gm => gm.GroupId == groupId
                && gm.PersonId == currentPersonId.Value
                && gm.GroupMemberStatus == GroupMemberStatus.Active
                && gm.GroupRole!.IsLeader, ct);
    }
}
