using AutoMapper;
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
/// Service for family (household) management operations.
/// </summary>
public class FamilyService(
    IApplicationDbContext context,
    IMapper mapper,
    IUserContext userContext,
    IValidator<CreateFamilyRequest> createFamilyValidator,
    IValidator<AddFamilyMemberRequest> addMemberValidator,
    ILogger<FamilyService> logger) : IFamilyService
{
    private const string FamilyGroupTypeNotFoundMessage = "Family group type not found in system";

    public async Task<FamilyDto?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        // Authorization check - throws if user doesn't have access
        await AuthorizeFamilyAccessAsync(id, nameof(GetByIdAsync), ct);

        var family = await context.Families
            .AsNoTracking()
            .Include(f => f.Campus)
            .Include(f => f.Members)
                .ThenInclude(m => m.Person)
            .Include(f => f.Members)
                .ThenInclude(m => m.FamilyRole)
            .FirstOrDefaultAsync(f => f.Id == id, ct);

        if (family is null)
        {
            return null;
        }

        return await MapToFamilyDtoAsync(family, ct);
    }

    public async Task<FamilyDto?> GetByIdKeyAsync(string idKey, CancellationToken ct = default)
    {
        if (!IdKeyHelper.TryDecode(idKey, out int id))
        {
            return null;
        }

        return await GetByIdAsync(id, ct);
    }

    public async Task<PagedResult<FamilySummaryDto>> SearchAsync(
        string? searchTerm,
        string? campusIdKey,
        bool includeInactive,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = context.Families
            .AsNoTracking()
            .Include(f => f.Members)
            .Where(f => true);

        // Filter by active status
        if (!includeInactive)
        {
            query = query.Where(f => f.IsActive);
        }

        // Filter by campus
        if (!string.IsNullOrWhiteSpace(campusIdKey) && IdKeyHelper.TryDecode(campusIdKey, out int campusId))
        {
            query = query.Where(f => f.CampusId == campusId);
        }

        // Filter by search term (search in name)
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(f => f.Name.Contains(searchTerm));
        }

        // Get total count
        var totalCount = await query.CountAsync(ct);

        // Apply pagination
        var families = await query
            .OrderBy(f => f.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(f => new FamilySummaryDto
            {
                IdKey = f.IdKey,
                Name = f.Name,
                MemberCount = f.Members.Count
            })
            .ToListAsync(ct);

        logger.LogInformation(
            "Family search completed: SearchTerm={SearchTerm}, CampusIdKey={CampusIdKey}, Page={Page}, PageSize={PageSize}, TotalCount={TotalCount}",
            searchTerm, campusIdKey, page, pageSize, totalCount);

        return new PagedResult<FamilySummaryDto>(families, totalCount, page, pageSize);
    }

    public async Task<Result<FamilyDto>> CreateFamilyAsync(
        CreateFamilyRequest request,
        CancellationToken ct = default)
    {
        // Validate
        var validation = await createFamilyValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return Result<FamilyDto>.Failure(Error.FromFluentValidation(validation));
        }

        // Map to entity
        var family = mapper.Map<Family>(request);
        family.CreatedDateTime = DateTime.UtcNow;

        // Decode and set campus ID if provided
        if (!string.IsNullOrWhiteSpace(request.CampusId))
        {
            if (IdKeyHelper.TryDecode(request.CampusId, out int campusId))
            {
                family.CampusId = campusId;
            }
        }

        // Create address if provided
        Location? location = null;
        if (request.Address != null)
        {
            location = mapper.Map<Location>(request.Address);
            location.CreatedDateTime = DateTime.UtcNow;
            await context.Locations.AddAsync(location, ct);
        }

        // Add to database
        await context.Families.AddAsync(family, ct);
        await context.SaveChangesAsync(ct);

        logger.LogInformation("Created family {FamilyId}: {Name}", family.Id, family.Name);

        // Fetch full family with includes (AsNoTracking to avoid cache conflicts)
        var createdFamily = await GetByIdAsync(family.Id, ct);
        return createdFamily != null
            ? Result<FamilyDto>.Success(createdFamily)
            : Result<FamilyDto>.Failure(Error.UnprocessableEntity("Failed to retrieve created family"));
    }

    public async Task<Result<FamilyMemberDto>> AddFamilyMemberAsync(
        string familyIdKey,
        AddFamilyMemberRequest request,
        CancellationToken ct = default)
    {
        // Validate
        var validation = await addMemberValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return Result<FamilyMemberDto>.Failure(Error.FromFluentValidation(validation));
        }

        // Get family
        if (!IdKeyHelper.TryDecode(familyIdKey, out int familyId))
        {
            return Result<FamilyMemberDto>.Failure(Error.NotFound("Family", familyIdKey));
        }

        // Authorization check - throws if user doesn't have access
        try
        {
            await AuthorizeFamilyAccessAsync(familyId, nameof(AddFamilyMemberAsync), ct);
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogWarning(ex, "Unauthorized attempt to add member to family {FamilyId}", familyId);
            return Result<FamilyMemberDto>.Failure(
                Error.Forbidden("Not authorized to modify this family"));
        }

        var family = await context.Families
            .FirstOrDefaultAsync(f => f.Id == familyId, ct);

        if (family is null)
        {
            return Result<FamilyMemberDto>.Failure(Error.NotFound("Family", familyIdKey));
        }

        // Get person
        if (!IdKeyHelper.TryDecode(request.PersonId, out int personId))
        {
            return Result<FamilyMemberDto>.Failure(Error.NotFound("Person", request.PersonId));
        }

        var person = await context.People.FindAsync(new object[] { personId }, ct);
        if (person is null)
        {
            return Result<FamilyMemberDto>.Failure(Error.NotFound("Person", request.PersonId));
        }

        // Get role
        if (!IdKeyHelper.TryDecode(request.RoleId, out int roleId))
        {
            return Result<FamilyMemberDto>.Failure(Error.NotFound("Role", request.RoleId));
        }

        var role = await context.GroupTypeRoles
            .FirstOrDefaultAsync(r => r.Id == roleId, ct);

        if (role is null)
        {
            return Result<FamilyMemberDto>.Failure(
                Error.NotFound("FamilyRole", request.RoleId));
        }

        // Check if person is already a member
        var existingMember = await context.FamilyMembers
            .FirstOrDefaultAsync(fm => fm.FamilyId == familyId && fm.PersonId == personId, ct);

        if (existingMember != null)
        {
            return Result<FamilyMemberDto>.Failure(
                Error.Conflict("Person is already a member of this family"));
        }

        // Create family member
        var familyMember = new FamilyMember
        {
            FamilyId = familyId,
            PersonId = personId,
            FamilyRoleId = roleId,
            IsPrimary = false, // Can be changed via SetPrimaryFamilyAsync
            DateAdded = DateTime.UtcNow,
            CreatedDateTime = DateTime.UtcNow
        };

        await context.FamilyMembers.AddAsync(familyMember, ct);
        await context.SaveChangesAsync(ct);

        logger.LogInformation(
            "Added person {PersonId} to family {FamilyId} with role {RoleId}",
            personId, familyId, roleId);

        // Fetch full member with includes
        var createdMember = await context.FamilyMembers
            .AsNoTracking()
            .Include(fm => fm.Person)
            .Include(fm => fm.FamilyRole)
            .FirstOrDefaultAsync(fm => fm.Id == familyMember.Id, ct);

        if (createdMember is null)
        {
            return Result<FamilyMemberDto>.Failure(
                Error.UnprocessableEntity("Failed to retrieve created family member"));
        }

        var memberDto = mapper.Map<FamilyMemberDto>(createdMember);
        return Result<FamilyMemberDto>.Success(memberDto);
    }

    public async Task<Result> RemoveFamilyMemberAsync(
        string familyIdKey,
        string personIdKey,
        CancellationToken ct = default)
    {
        // Get family
        if (!IdKeyHelper.TryDecode(familyIdKey, out int familyId))
        {
            return Result.Failure(Error.NotFound("Family", familyIdKey));
        }

        // Authorization check - throws if user doesn't have access
        try
        {
            await AuthorizeFamilyAccessAsync(familyId, nameof(RemoveFamilyMemberAsync), ct);
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogWarning(ex, "Unauthorized attempt to remove member from family {FamilyId}", familyId);
            return Result.Failure(
                Error.Forbidden("Not authorized to modify this family"));
        }

        var family = await context.Families
            .FirstOrDefaultAsync(f => f.Id == familyId, ct);

        if (family is null)
        {
            return Result.Failure(Error.NotFound("Family", familyIdKey));
        }

        // Get person
        if (!IdKeyHelper.TryDecode(personIdKey, out int personId))
        {
            return Result.Failure(Error.NotFound("Person", personIdKey));
        }

        // Find family member
        var familyMember = await context.FamilyMembers
            .FirstOrDefaultAsync(fm => fm.FamilyId == familyId && fm.PersonId == personId, ct);

        if (familyMember is null)
        {
            return Result.Failure(
                Error.NotFound("FamilyMember", $"Person {personIdKey} in Family {familyIdKey}"));
        }

        // Hard delete the family member
        context.FamilyMembers.Remove(familyMember);

        await context.SaveChangesAsync(ct);

        logger.LogInformation(
            "Removed person {PersonId} from family {FamilyId}",
            personId, familyId);

        return Result.Success();
    }

    public async Task<Result> SetPrimaryFamilyAsync(
        string personIdKey,
        string familyIdKey,
        CancellationToken ct = default)
    {
        // Get person
        if (!IdKeyHelper.TryDecode(personIdKey, out int personId))
        {
            return Result.Failure(Error.NotFound("Person", personIdKey));
        }

        var person = await context.People.FindAsync(new object[] { personId }, ct);
        if (person is null)
        {
            return Result.Failure(Error.NotFound("Person", personIdKey));
        }

        // Get family
        if (!IdKeyHelper.TryDecode(familyIdKey, out int familyId))
        {
            return Result.Failure(Error.NotFound("Family", familyIdKey));
        }

        // Authorization check - throws if user doesn't have access
        try
        {
            await AuthorizeFamilyAccessAsync(familyId, nameof(SetPrimaryFamilyAsync), ct);
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogWarning(ex, "Unauthorized attempt to set primary family {FamilyId} for person {PersonId}", familyId, personId);
            return Result.Failure(
                Error.Forbidden("Not authorized to modify this family"));
        }

        var family = await context.Families
            .FirstOrDefaultAsync(f => f.Id == familyId, ct);

        if (family is null)
        {
            return Result.Failure(Error.NotFound("Family", familyIdKey));
        }

        // Check if person is a member of the family
        var isMember = await context.FamilyMembers
            .AnyAsync(fm => fm.FamilyId == familyId && fm.PersonId == personId, ct);

        if (!isMember)
        {
            return Result.Failure(
                Error.UnprocessableEntity("Person must be an active member of the family to set it as primary"));
        }

        // Set primary family by updating the IsPrimary flag on FamilyMember
        // First, clear any existing primary family
        var existingPrimaryMembership = await context.FamilyMembers
            .FirstOrDefaultAsync(fm => fm.PersonId == personId && fm.IsPrimary, ct);

        if (existingPrimaryMembership != null)
        {
            existingPrimaryMembership.IsPrimary = false;
            existingPrimaryMembership.ModifiedDateTime = DateTime.UtcNow;
        }

        // Set the new primary family
        var newPrimaryMembership = await context.FamilyMembers
            .FirstOrDefaultAsync(fm => fm.FamilyId == familyId && fm.PersonId == personId, ct);

        if (newPrimaryMembership != null)
        {
            newPrimaryMembership.IsPrimary = true;
            newPrimaryMembership.ModifiedDateTime = DateTime.UtcNow;
        }

        person.ModifiedDateTime = DateTime.UtcNow;

        await context.SaveChangesAsync(ct);

        logger.LogInformation(
            "Set family {FamilyId} as primary for person {PersonId}",
            familyId, personId);

        return Result.Success();
    }

    public Task<Result<FamilyDto>> UpdateAddressAsync(
        string familyIdKey,
        UpdateFamilyAddressRequest request,
        CancellationToken ct = default)
    {
        // Address management is not yet implemented
        return Task.FromResult(Result<FamilyDto>.Failure(
            Error.NotImplemented("Address management is not yet implemented")));
    }

    private Task<FamilyDto> MapToFamilyDtoAsync(Family family, CancellationToken ct)
    {
        var familyDto = mapper.Map<FamilyDto>(family);

        // Map members
        var memberDtos = family.Members
            .Select(m => mapper.Map<FamilyMemberDto>(m))
            .ToList();

        // Create new DTO with members populated
        var result = new FamilyDto
        {
            IdKey = familyDto.IdKey,
            Guid = familyDto.Guid,
            Name = familyDto.Name,
            Description = familyDto.Description,
            IsActive = familyDto.IsActive,
            Campus = familyDto.Campus,
            Address = null,
            Members = memberDtos,
            CreatedDateTime = familyDto.CreatedDateTime,
            ModifiedDateTime = familyDto.ModifiedDateTime
        };

        return Task.FromResult(result);
    }

    /// <summary>
    /// Checks if the current user can access the specified family.
    /// Returns true if: user is Admin/Staff, or user is a member of the family.
    /// </summary>
    private async Task<bool> CanUserAccessFamilyAsync(int familyId, CancellationToken ct)
    {
        // First check role-based authorization (Admin, Staff)
        if (userContext.CanAccessFamily(familyId))
        {
            return true;
        }

        // Not authenticated or doesn't have role-based access
        if (!userContext.IsAuthenticated || !userContext.CurrentPersonId.HasValue)
        {
            return false;
        }

        // Check if current user is a member of this family
        var isMember = await context.FamilyMembers
            .AnyAsync(
                fm => fm.FamilyId == familyId
                    && fm.PersonId == userContext.CurrentPersonId.Value,
                ct);

        return isMember;
    }

    /// <summary>
    /// Throws UnauthorizedAccessException if user cannot access the family.
    /// </summary>
    private async Task AuthorizeFamilyAccessAsync(int familyId, string operationName, CancellationToken ct)
    {
        if (!await CanUserAccessFamilyAsync(familyId, ct))
        {
            logger.LogWarning(
                "Authorization denied: User {UserId} denied access for {Operation} on family {FamilyId}",
                userContext.CurrentPersonId, operationName, familyId);
            throw new UnauthorizedAccessException("Not authorized to access this family");
        }
    }
}
