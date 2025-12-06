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
    IValidator<CreateFamilyRequest> createFamilyValidator,
    IValidator<AddFamilyMemberRequest> addMemberValidator,
    ILogger<FamilyService> logger) : IFamilyService
{
    private const string FamilyGroupTypeNotFoundMessage = "Family group type not found in system";

    public async Task<FamilyDto?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var family = await context.Groups
            .AsNoTracking()
            .Include(g => g.GroupType)
            .Include(g => g.Campus)
            .Include(g => g.Members)
                .ThenInclude(m => m.Person)
            .Include(g => g.Members)
                .ThenInclude(m => m.GroupRole)
            .FirstOrDefaultAsync(g => g.Id == id && g.GroupType!.IsFamilyGroupType, ct);

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

        // Get family group type
        var familyGroupType = await context.GroupTypes
            .FirstOrDefaultAsync(gt => gt.IsFamilyGroupType, ct);

        if (familyGroupType is null)
        {
            return Result<FamilyDto>.Failure(
                Error.UnprocessableEntity(FamilyGroupTypeNotFoundMessage));
        }

        // Map to entity
        var family = mapper.Map<Group>(request);
        family.GroupTypeId = familyGroupType.Id;
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
        await context.Groups.AddAsync(family, ct);
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

        var family = await context.Groups
            .Include(g => g.GroupType)
            .FirstOrDefaultAsync(g => g.Id == familyId && g.GroupType!.IsFamilyGroupType, ct);

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
            .FirstOrDefaultAsync(r => r.Id == roleId && r.GroupTypeId == family.GroupTypeId, ct);

        if (role is null)
        {
            return Result<FamilyMemberDto>.Failure(
                Error.UnprocessableEntity("Role is not valid for this family group type"));
        }

        // Check if person is already a member
        var existingMember = await context.GroupMembers
            .FirstOrDefaultAsync(gm => gm.GroupId == familyId && gm.PersonId == personId, ct);

        if (existingMember != null)
        {
            return Result<FamilyMemberDto>.Failure(
                Error.Conflict("Person is already a member of this family"));
        }

        // Create group member
        var groupMember = new GroupMember
        {
            GroupId = familyId,
            PersonId = personId,
            GroupRoleId = roleId,
            GroupMemberStatus = GroupMemberStatus.Active,
            DateTimeAdded = DateTime.UtcNow,
            CreatedDateTime = DateTime.UtcNow
        };

        await context.GroupMembers.AddAsync(groupMember, ct);
        await context.SaveChangesAsync(ct);

        logger.LogInformation(
            "Added person {PersonId} to family {FamilyId} with role {RoleId}",
            personId, familyId, roleId);

        // Fetch full member with includes
        var createdMember = await context.GroupMembers
            .AsNoTracking()
            .Include(gm => gm.Person)
            .Include(gm => gm.GroupRole)
            .FirstOrDefaultAsync(gm => gm.Id == groupMember.Id, ct);

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

        var family = await context.Groups
            .Include(g => g.GroupType)
            .FirstOrDefaultAsync(g => g.Id == familyId && g.GroupType!.IsFamilyGroupType, ct);

        if (family is null)
        {
            return Result.Failure(Error.NotFound("Family", familyIdKey));
        }

        // Get person
        if (!IdKeyHelper.TryDecode(personIdKey, out int personId))
        {
            return Result.Failure(Error.NotFound("Person", personIdKey));
        }

        // Find group member
        var groupMember = await context.GroupMembers
            .FirstOrDefaultAsync(gm => gm.GroupId == familyId && gm.PersonId == personId, ct);

        if (groupMember is null)
        {
            return Result.Failure(
                Error.NotFound("GroupMember", $"Person {personIdKey} in Family {familyIdKey}"));
        }

        // Soft delete by marking as inactive
        groupMember.GroupMemberStatus = GroupMemberStatus.Inactive;
        groupMember.InactiveDateTime = DateTime.UtcNow;
        groupMember.ModifiedDateTime = DateTime.UtcNow;

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

        var family = await context.Groups
            .Include(g => g.GroupType)
            .FirstOrDefaultAsync(g => g.Id == familyId && g.GroupType!.IsFamilyGroupType, ct);

        if (family is null)
        {
            return Result.Failure(Error.NotFound("Family", familyIdKey));
        }

        // Check if person is a member of the family
        var isMember = await context.GroupMembers
            .AnyAsync(gm => gm.GroupId == familyId && gm.PersonId == personId
                && gm.GroupMemberStatus == GroupMemberStatus.Active, ct);

        if (!isMember)
        {
            return Result.Failure(
                Error.UnprocessableEntity("Person must be an active member of the family to set it as primary"));
        }

        // Set primary family
        person.PrimaryFamilyId = familyId;
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

    private Task<FamilyDto> MapToFamilyDtoAsync(Group group, CancellationToken ct)
    {
        var familyDto = mapper.Map<FamilyDto>(group);

        // Map members
        var memberDtos = group.Members
            .Where(m => m.GroupMemberStatus == GroupMemberStatus.Active)
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
}
