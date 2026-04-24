using FluentValidation;
using Koinon.Application.Common;
using Koinon.Application.DTOs.Security;
using Koinon.Application.Interfaces;
using Koinon.Domain.Data;
using Koinon.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Koinon.Application.Services;

/// <summary>
/// Administrative service for managing security roles, their claims, and their members.
/// Provides CRUD operations for the admin UI. Runtime authorization (HasClaim, etc.) is
/// handled separately by <see cref="SecurityClaimService"/>.
/// </summary>
public class SecurityRoleService(
    IApplicationDbContext context,
    IValidator<CreateSecurityRoleRequest> createValidator,
    IValidator<UpdateSecurityRoleRequest> updateValidator,
    IValidator<AssignClaimRequest> assignClaimValidator,
    IValidator<AssignPersonRequest> assignPersonValidator,
    ILogger<SecurityRoleService> logger) : ISecurityRoleService
{
    /// <inheritdoc />
    public async Task<Result<List<SecurityRoleDto>>> GetAllAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;

        var roles = await context.SecurityRoles
            .AsNoTracking()
            .OrderBy(r => r.Name)
            .Select(r => new
            {
                r.Id,
                r.Name,
                r.Description,
                r.IsSystemRole,
                r.IsActive,
                ClaimCount = r.RoleClaims.Count,
                MemberCount = r.PersonRoles.Count(psr =>
                    psr.ExpiresDateTime == null || psr.ExpiresDateTime > now)
            })
            .ToListAsync(ct);

        var dtos = roles
            .Select(r => new SecurityRoleDto
            {
                IdKey = IdKeyHelper.Encode(r.Id),
                Name = r.Name,
                Description = r.Description,
                IsSystemRole = r.IsSystemRole,
                IsActive = r.IsActive,
                ClaimCount = r.ClaimCount,
                MemberCount = r.MemberCount
            })
            .ToList();

        return Result<List<SecurityRoleDto>>.Success(dtos);
    }

    /// <inheritdoc />
    public async Task<Result<SecurityRoleDetailDto>> GetByIdKeyAsync(string idKey, CancellationToken ct = default)
    {
        if (!IdKeyHelper.TryDecode(idKey, out var roleId))
        {
            return Result<SecurityRoleDetailDto>.Failure(Error.NotFound("SecurityRole", idKey));
        }

        var role = await context.SecurityRoles
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == roleId, ct);

        if (role == null)
        {
            return Result<SecurityRoleDetailDto>.Failure(Error.NotFound("SecurityRole", idKey));
        }

        var claims = await context.RoleSecurityClaims
            .AsNoTracking()
            .Include(rsc => rsc.SecurityClaim)
            .Where(rsc => rsc.SecurityRoleId == roleId)
            .OrderBy(rsc => rsc.SecurityClaim.ClaimType)
            .ThenBy(rsc => rsc.SecurityClaim.ClaimValue)
            .Select(rsc => new
            {
                ClaimId = rsc.SecurityClaim.Id,
                rsc.SecurityClaim.ClaimType,
                rsc.SecurityClaim.ClaimValue,
                rsc.SecurityClaim.Description,
                rsc.AllowOrDeny
            })
            .ToListAsync(ct);

        var now = DateTime.UtcNow;
        var members = await context.PersonSecurityRoles
            .AsNoTracking()
            .Include(psr => psr.Person)
            .Where(psr => psr.SecurityRoleId == roleId &&
                          (psr.ExpiresDateTime == null || psr.ExpiresDateTime > now))
            .OrderBy(psr => psr.Person.LastName)
            .ThenBy(psr => psr.Person.FirstName)
            .Select(psr => new
            {
                PersonId = psr.Person.Id,
                psr.Person.FirstName,
                psr.Person.LastName,
                psr.Person.NickName,
                psr.Person.Email,
                psr.ExpiresDateTime
            })
            .ToListAsync(ct);

        var detail = new SecurityRoleDetailDto
        {
            IdKey = IdKeyHelper.Encode(role.Id),
            Name = role.Name,
            Description = role.Description,
            IsSystemRole = role.IsSystemRole,
            IsActive = role.IsActive,
            Claims = claims
                .Select(c => new RoleSecurityClaimDto
                {
                    ClaimIdKey = IdKeyHelper.Encode(c.ClaimId),
                    ClaimType = c.ClaimType,
                    ClaimValue = c.ClaimValue,
                    Description = c.Description,
                    AllowOrDeny = c.AllowOrDeny
                })
                .ToList(),
            Members = members
                .Select(m => new PersonSecurityRoleMemberDto
                {
                    PersonIdKey = IdKeyHelper.Encode(m.PersonId),
                    PersonName = BuildPersonName(m.FirstName, m.LastName, m.NickName),
                    Email = m.Email,
                    ExpiresDateTime = m.ExpiresDateTime
                })
                .ToList()
        };

        return Result<SecurityRoleDetailDto>.Success(detail);
    }

    /// <inheritdoc />
    public async Task<Result<List<SecurityClaimDto>>> GetAllClaimsAsync(CancellationToken ct = default)
    {
        var claims = await context.SecurityClaims
            .AsNoTracking()
            .OrderBy(c => c.ClaimType)
            .ThenBy(c => c.ClaimValue)
            .Select(c => new { c.Id, c.ClaimType, c.ClaimValue, c.Description })
            .ToListAsync(ct);

        var dtos = claims
            .Select(c => new SecurityClaimDto
            {
                IdKey = IdKeyHelper.Encode(c.Id),
                ClaimType = c.ClaimType,
                ClaimValue = c.ClaimValue,
                Description = c.Description
            })
            .ToList();

        return Result<List<SecurityClaimDto>>.Success(dtos);
    }

    /// <inheritdoc />
    public async Task<Result<SecurityRoleDto>> CreateAsync(CreateSecurityRoleRequest request, CancellationToken ct = default)
    {
        var validation = await createValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return Result<SecurityRoleDto>.Failure(Error.FromFluentValidation(validation));
        }

        var nameTaken = await context.SecurityRoles
            .AnyAsync(r => r.Name == request.Name, ct);

        if (nameTaken)
        {
            return Result<SecurityRoleDto>.Failure(
                Error.Conflict($"A security role named '{request.Name}' already exists."));
        }

        var entity = new SecurityRole
        {
            Name = request.Name,
            Description = request.Description,
            IsSystemRole = false,
            IsActive = request.IsActive,
            CreatedDateTime = DateTime.UtcNow
        };

        context.SecurityRoles.Add(entity);
        await context.SaveChangesAsync(ct);

        logger.LogInformation(
            "Security role created: IdKey={IdKey}, Name={Name}",
            entity.IdKey, entity.Name);

        return Result<SecurityRoleDto>.Success(new SecurityRoleDto
        {
            IdKey = entity.IdKey,
            Name = entity.Name,
            Description = entity.Description,
            IsSystemRole = entity.IsSystemRole,
            IsActive = entity.IsActive,
            ClaimCount = 0,
            MemberCount = 0
        });
    }

    /// <inheritdoc />
    public async Task<Result<SecurityRoleDto>> UpdateAsync(string idKey, UpdateSecurityRoleRequest request, CancellationToken ct = default)
    {
        if (!IdKeyHelper.TryDecode(idKey, out var roleId))
        {
            return Result<SecurityRoleDto>.Failure(Error.NotFound("SecurityRole", idKey));
        }

        var validation = await updateValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return Result<SecurityRoleDto>.Failure(Error.FromFluentValidation(validation));
        }

        // AsTracking required: global QueryTrackingBehavior is NoTracking (see PostgreSqlProvider),
        // so without it SaveChanges would not persist the mutations below. (fixes #685)
        var entity = await context.SecurityRoles
            .AsTracking()
            .FirstOrDefaultAsync(r => r.Id == roleId, ct);

        if (entity == null)
        {
            return Result<SecurityRoleDto>.Failure(Error.NotFound("SecurityRole", idKey));
        }

        if (entity.Name != request.Name)
        {
            var nameTaken = await context.SecurityRoles
                .AnyAsync(r => r.Id != roleId && r.Name == request.Name, ct);
            if (nameTaken)
            {
                return Result<SecurityRoleDto>.Failure(
                    Error.Conflict($"A security role named '{request.Name}' already exists."));
            }
        }

        entity.Name = request.Name;
        entity.Description = request.Description;
        entity.IsActive = request.IsActive;
        entity.ModifiedDateTime = DateTime.UtcNow;

        await context.SaveChangesAsync(ct);

        logger.LogInformation(
            "Security role updated: IdKey={IdKey}, Name={Name}",
            entity.IdKey, entity.Name);

        var now = DateTime.UtcNow;
        var claimCount = await context.RoleSecurityClaims.CountAsync(rsc => rsc.SecurityRoleId == roleId, ct);
        var memberCount = await context.PersonSecurityRoles
            .CountAsync(psr => psr.SecurityRoleId == roleId &&
                               (psr.ExpiresDateTime == null || psr.ExpiresDateTime > now), ct);

        return Result<SecurityRoleDto>.Success(new SecurityRoleDto
        {
            IdKey = entity.IdKey,
            Name = entity.Name,
            Description = entity.Description,
            IsSystemRole = entity.IsSystemRole,
            IsActive = entity.IsActive,
            ClaimCount = claimCount,
            MemberCount = memberCount
        });
    }

    /// <inheritdoc />
    public async Task<Result> DeleteAsync(string idKey, CancellationToken ct = default)
    {
        if (!IdKeyHelper.TryDecode(idKey, out var roleId))
        {
            return Result.Failure(Error.NotFound("SecurityRole", idKey));
        }

        var entity = await context.SecurityRoles
            .Include(r => r.RoleClaims)
            .Include(r => r.PersonRoles)
            .FirstOrDefaultAsync(r => r.Id == roleId, ct);

        if (entity == null)
        {
            return Result.Failure(Error.NotFound("SecurityRole", idKey));
        }

        if (entity.IsSystemRole)
        {
            return Result.Failure(
                Error.UnprocessableEntity($"System role '{entity.Name}' cannot be deleted."));
        }

        // Cascade: remove claim and member associations first
        context.RoleSecurityClaims.RemoveRange(entity.RoleClaims);
        context.PersonSecurityRoles.RemoveRange(entity.PersonRoles);
        context.SecurityRoles.Remove(entity);
        await context.SaveChangesAsync(ct);

        logger.LogInformation(
            "Security role deleted: IdKey={IdKey}, Name={Name}",
            idKey, entity.Name);

        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result> AssignClaimAsync(string roleIdKey, AssignClaimRequest request, CancellationToken ct = default)
    {
        var validation = await assignClaimValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return Result.Failure(Error.FromFluentValidation(validation));
        }

        if (!IdKeyHelper.TryDecode(roleIdKey, out var roleId))
        {
            return Result.Failure(Error.NotFound("SecurityRole", roleIdKey));
        }

        if (!IdKeyHelper.TryDecode(request.ClaimIdKey, out var claimId))
        {
            return Result.Failure(Error.NotFound("SecurityClaim", request.ClaimIdKey));
        }

        var role = await context.SecurityRoles
            .FirstOrDefaultAsync(r => r.Id == roleId, ct);
        if (role == null)
        {
            return Result.Failure(Error.NotFound("SecurityRole", roleIdKey));
        }

        var claim = await context.SecurityClaims
            .FirstOrDefaultAsync(c => c.Id == claimId, ct);
        if (claim == null)
        {
            return Result.Failure(Error.NotFound("SecurityClaim", request.ClaimIdKey));
        }

        var existing = await context.RoleSecurityClaims
            .FirstOrDefaultAsync(rsc => rsc.SecurityRoleId == roleId && rsc.SecurityClaimId == claimId, ct);

        if (existing != null)
        {
            existing.AllowOrDeny = request.AllowOrDeny;
            existing.ModifiedDateTime = DateTime.UtcNow;
        }
        else
        {
            context.RoleSecurityClaims.Add(new RoleSecurityClaim
            {
                SecurityRoleId = roleId,
                SecurityClaimId = claimId,
                AllowOrDeny = request.AllowOrDeny,
                CreatedDateTime = DateTime.UtcNow
            });
        }

        await context.SaveChangesAsync(ct);

        logger.LogInformation(
            "Claim assigned to role: RoleIdKey={RoleIdKey}, ClaimIdKey={ClaimIdKey}, AllowOrDeny={AllowOrDeny}",
            roleIdKey, request.ClaimIdKey, request.AllowOrDeny);

        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result> RemoveClaimAsync(string roleIdKey, string claimIdKey, CancellationToken ct = default)
    {
        if (!IdKeyHelper.TryDecode(roleIdKey, out var roleId))
        {
            return Result.Failure(Error.NotFound("SecurityRole", roleIdKey));
        }

        if (!IdKeyHelper.TryDecode(claimIdKey, out var claimId))
        {
            return Result.Failure(Error.NotFound("SecurityClaim", claimIdKey));
        }

        var link = await context.RoleSecurityClaims
            .FirstOrDefaultAsync(rsc => rsc.SecurityRoleId == roleId && rsc.SecurityClaimId == claimId, ct);

        if (link == null)
        {
            return Result.Failure(
                Error.NotFound($"Role-claim association for role '{roleIdKey}'", claimIdKey));
        }

        context.RoleSecurityClaims.Remove(link);
        await context.SaveChangesAsync(ct);

        logger.LogInformation(
            "Claim removed from role: RoleIdKey={RoleIdKey}, ClaimIdKey={ClaimIdKey}",
            roleIdKey, claimIdKey);

        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result> AddMemberAsync(string roleIdKey, AssignPersonRequest request, CancellationToken ct = default)
    {
        var validation = await assignPersonValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return Result.Failure(Error.FromFluentValidation(validation));
        }

        if (!IdKeyHelper.TryDecode(roleIdKey, out var roleId))
        {
            return Result.Failure(Error.NotFound("SecurityRole", roleIdKey));
        }

        if (!IdKeyHelper.TryDecode(request.PersonIdKey, out var personId))
        {
            return Result.Failure(Error.NotFound("Person", request.PersonIdKey));
        }

        var roleExists = await context.SecurityRoles.AnyAsync(r => r.Id == roleId, ct);
        if (!roleExists)
        {
            return Result.Failure(Error.NotFound("SecurityRole", roleIdKey));
        }

        var personExists = await context.People.AnyAsync(p => p.Id == personId, ct);
        if (!personExists)
        {
            return Result.Failure(Error.NotFound("Person", request.PersonIdKey));
        }

        var existing = await context.PersonSecurityRoles
            .FirstOrDefaultAsync(psr => psr.SecurityRoleId == roleId && psr.PersonId == personId, ct);

        if (existing != null)
        {
            existing.ExpiresDateTime = request.ExpiresDateTime;
            existing.ModifiedDateTime = DateTime.UtcNow;
        }
        else
        {
            context.PersonSecurityRoles.Add(new PersonSecurityRole
            {
                PersonId = personId,
                SecurityRoleId = roleId,
                ExpiresDateTime = request.ExpiresDateTime,
                CreatedDateTime = DateTime.UtcNow
            });
        }

        await context.SaveChangesAsync(ct);

        logger.LogInformation(
            "Member added to role: RoleIdKey={RoleIdKey}, PersonIdKey={PersonIdKey}, Expires={Expires}",
            roleIdKey, request.PersonIdKey, request.ExpiresDateTime);

        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result> RemoveMemberAsync(string roleIdKey, string personIdKey, CancellationToken ct = default)
    {
        if (!IdKeyHelper.TryDecode(roleIdKey, out var roleId))
        {
            return Result.Failure(Error.NotFound("SecurityRole", roleIdKey));
        }

        if (!IdKeyHelper.TryDecode(personIdKey, out var personId))
        {
            return Result.Failure(Error.NotFound("Person", personIdKey));
        }

        var link = await context.PersonSecurityRoles
            .FirstOrDefaultAsync(psr => psr.SecurityRoleId == roleId && psr.PersonId == personId, ct);

        if (link == null)
        {
            return Result.Failure(
                Error.NotFound($"Role-member association for role '{roleIdKey}'", personIdKey));
        }

        context.PersonSecurityRoles.Remove(link);
        await context.SaveChangesAsync(ct);

        logger.LogInformation(
            "Member removed from role: RoleIdKey={RoleIdKey}, PersonIdKey={PersonIdKey}",
            roleIdKey, personIdKey);

        return Result.Success();
    }

    private static string BuildPersonName(string firstName, string lastName, string? nickName)
    {
        var given = string.IsNullOrWhiteSpace(nickName) ? firstName : nickName;
        return $"{given} {lastName}".Trim();
    }
}
