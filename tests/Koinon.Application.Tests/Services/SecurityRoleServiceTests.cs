using FluentAssertions;
using Koinon.Application.DTOs.Security;
using Koinon.Application.Services;
using Koinon.Application.Validators.Security;
using Koinon.Domain.Data;
using Koinon.Domain.Entities;
using Koinon.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Koinon.Application.Tests.Services;

/// <summary>
/// Unit tests for <see cref="SecurityRoleService"/>.
/// Covers admin CRUD operations for roles, claims, and members.
/// </summary>
public class SecurityRoleServiceTests : IDisposable
{
    private readonly KoinonDbContext _context;
    private readonly SecurityRoleService _service;

    public SecurityRoleServiceTests()
    {
        var options = new DbContextOptionsBuilder<KoinonDbContext>()
            .UseInMemoryDatabase(databaseName: $"SecurityRoleTest_{Guid.NewGuid()}")
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new KoinonDbContext(options);
        _context.Database.EnsureCreated();

        var loggerMock = new Mock<ILogger<SecurityRoleService>>();

        _service = new SecurityRoleService(
            _context,
            new CreateSecurityRoleRequestValidator(),
            new UpdateSecurityRoleRequestValidator(),
            new AssignClaimRequestValidator(),
            new AssignPersonRequestValidator(),
            loggerMock.Object);

        SeedData();
    }

    private void SeedData()
    {
        _context.People.Add(new Person
        {
            Id = 100,
            FirstName = "Alice",
            LastName = "Admin",
            NickName = "Alice",
            Email = "alice@example.com",
            Gender = Domain.Enums.Gender.Female,
            CreatedDateTime = DateTime.UtcNow
        });

        _context.People.Add(new Person
        {
            Id = 101,
            FirstName = "Bob",
            LastName = "User",
            Email = "bob@example.com",
            Gender = Domain.Enums.Gender.Male,
            CreatedDateTime = DateTime.UtcNow
        });

        _context.SecurityRoles.Add(new SecurityRole
        {
            Id = 200,
            Name = "System Admin",
            Description = "Built-in admin",
            IsSystemRole = true,
            IsActive = true,
            CreatedDateTime = DateTime.UtcNow
        });

        _context.SecurityRoles.Add(new SecurityRole
        {
            Id = 201,
            Name = "Volunteer Coordinator",
            Description = "Manages volunteers",
            IsSystemRole = false,
            IsActive = true,
            CreatedDateTime = DateTime.UtcNow
        });

        _context.SecurityClaims.Add(new SecurityClaim
        {
            Id = 300,
            ClaimType = "people",
            ClaimValue = "view",
            Description = "View people",
        });

        _context.SecurityClaims.Add(new SecurityClaim
        {
            Id = 301,
            ClaimType = "people",
            ClaimValue = "edit",
            Description = "Edit people",
        });

        _context.SaveChanges();
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllRolesWithCounts()
    {
        // Arrange — add a claim and member to Volunteer Coordinator
        _context.RoleSecurityClaims.Add(new RoleSecurityClaim
        {
            SecurityRoleId = 201,
            SecurityClaimId = 300,
            AllowOrDeny = 'A',
            CreatedDateTime = DateTime.UtcNow
        });
        _context.PersonSecurityRoles.Add(new PersonSecurityRole
        {
            PersonId = 100,
            SecurityRoleId = 201,
            CreatedDateTime = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetAllAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        var roles = result.Value!;
        roles.Should().HaveCount(2);

        var volunteerCoordinator = roles.First(r => r.Name == "Volunteer Coordinator");
        volunteerCoordinator.ClaimCount.Should().Be(1);
        volunteerCoordinator.MemberCount.Should().Be(1);
        volunteerCoordinator.IsSystemRole.Should().BeFalse();

        var systemAdmin = roles.First(r => r.Name == "System Admin");
        systemAdmin.IsSystemRole.Should().BeTrue();
        systemAdmin.ClaimCount.Should().Be(0);
        systemAdmin.MemberCount.Should().Be(0);
    }

    [Fact]
    public async Task CreateAsync_WithValidRequest_CreatesRole()
    {
        var request = new CreateSecurityRoleRequest
        {
            Name = "Test Role",
            Description = "A test role",
            IsActive = true
        };

        var result = await _service.CreateAsync(request);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Test Role");
        result.Value.IsSystemRole.Should().BeFalse();
        result.Value.IsActive.Should().BeTrue();
        result.Value.ClaimCount.Should().Be(0);
        result.Value.MemberCount.Should().Be(0);

        var entity = await _context.SecurityRoles.FirstOrDefaultAsync(r => r.Name == "Test Role");
        entity.Should().NotBeNull();
        entity!.IsSystemRole.Should().BeFalse();
    }

    [Fact]
    public async Task CreateAsync_WithMissingName_ReturnsValidationError()
    {
        var request = new CreateSecurityRoleRequest { Name = "", Description = null };

        var result = await _service.CreateAsync(request);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("VALIDATION_ERROR");
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateName_ReturnsConflict()
    {
        var request = new CreateSecurityRoleRequest
        {
            Name = "Volunteer Coordinator", // already seeded
            Description = null,
            IsActive = true
        };

        var result = await _service.CreateAsync(request);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("CONFLICT");
    }

    [Fact]
    public async Task UpdateAsync_WithValidRequest_UpdatesRole()
    {
        var idKey = IdKeyHelper.Encode(201);
        var request = new UpdateSecurityRoleRequest
        {
            Name = "Volunteer Lead",
            Description = "Updated description",
            IsActive = true
        };

        var result = await _service.UpdateAsync(idKey, request);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Volunteer Lead");
        result.Value.Description.Should().Be("Updated description");
    }

    [Fact]
    public async Task UpdateAsync_WithInvalidIdKey_ReturnsNotFound()
    {
        var request = new UpdateSecurityRoleRequest
        {
            Name = "Whatever",
            IsActive = true
        };

        var result = await _service.UpdateAsync("not-a-real-idkey!!", request);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("NOT_FOUND");
    }

    [Fact]
    public async Task DeleteAsync_RefusesSystemRole()
    {
        var idKey = IdKeyHelper.Encode(200); // System Admin

        var result = await _service.DeleteAsync(idKey);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("UNPROCESSABLE_ENTITY");

        var stillExists = await _context.SecurityRoles.AnyAsync(r => r.Id == 200);
        stillExists.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteAsync_NonSystemRole_DeletesWithCascade()
    {
        _context.RoleSecurityClaims.Add(new RoleSecurityClaim
        {
            SecurityRoleId = 201,
            SecurityClaimId = 300,
            AllowOrDeny = 'A',
            CreatedDateTime = DateTime.UtcNow
        });
        _context.PersonSecurityRoles.Add(new PersonSecurityRole
        {
            PersonId = 100,
            SecurityRoleId = 201,
            CreatedDateTime = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        var idKey = IdKeyHelper.Encode(201);

        var result = await _service.DeleteAsync(idKey);

        result.IsSuccess.Should().BeTrue();
        (await _context.SecurityRoles.AnyAsync(r => r.Id == 201)).Should().BeFalse();
        (await _context.RoleSecurityClaims.AnyAsync(rsc => rsc.SecurityRoleId == 201)).Should().BeFalse();
        (await _context.PersonSecurityRoles.AnyAsync(psr => psr.SecurityRoleId == 201)).Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_InvalidIdKey_ReturnsNotFound()
    {
        var result = await _service.DeleteAsync("!!!bad!!!");
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("NOT_FOUND");
    }

    [Fact]
    public async Task AssignClaimAsync_HappyPath_AddsAssociation()
    {
        var roleIdKey = IdKeyHelper.Encode(201);
        var claimIdKey = IdKeyHelper.Encode(300);
        var request = new AssignClaimRequest { ClaimIdKey = claimIdKey, AllowOrDeny = 'A' };

        var result = await _service.AssignClaimAsync(roleIdKey, request);

        result.IsSuccess.Should().BeTrue();
        var association = await _context.RoleSecurityClaims
            .FirstOrDefaultAsync(rsc => rsc.SecurityRoleId == 201 && rsc.SecurityClaimId == 300);
        association.Should().NotBeNull();
        association!.AllowOrDeny.Should().Be('A');
    }

    [Fact]
    public async Task AssignClaimAsync_ExistingAssociation_UpdatesDecision()
    {
        _context.RoleSecurityClaims.Add(new RoleSecurityClaim
        {
            SecurityRoleId = 201,
            SecurityClaimId = 300,
            AllowOrDeny = 'A',
            CreatedDateTime = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        var roleIdKey = IdKeyHelper.Encode(201);
        var claimIdKey = IdKeyHelper.Encode(300);
        var request = new AssignClaimRequest { ClaimIdKey = claimIdKey, AllowOrDeny = 'D' };

        var result = await _service.AssignClaimAsync(roleIdKey, request);

        result.IsSuccess.Should().BeTrue();
        var association = await _context.RoleSecurityClaims
            .FirstOrDefaultAsync(rsc => rsc.SecurityRoleId == 201 && rsc.SecurityClaimId == 300);
        association!.AllowOrDeny.Should().Be('D');
    }

    [Fact]
    public async Task AssignClaimAsync_InvalidAllowOrDeny_ReturnsValidationError()
    {
        var roleIdKey = IdKeyHelper.Encode(201);
        var claimIdKey = IdKeyHelper.Encode(300);
        var request = new AssignClaimRequest { ClaimIdKey = claimIdKey, AllowOrDeny = 'X' };

        var result = await _service.AssignClaimAsync(roleIdKey, request);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("VALIDATION_ERROR");
    }

    [Fact]
    public async Task AssignClaimAsync_InvalidRoleIdKey_ReturnsNotFound()
    {
        var request = new AssignClaimRequest
        {
            ClaimIdKey = IdKeyHelper.Encode(300),
            AllowOrDeny = 'A'
        };

        var result = await _service.AssignClaimAsync("!!!bad!!!", request);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("NOT_FOUND");
    }

    [Fact]
    public async Task RemoveClaimAsync_HappyPath_RemovesAssociation()
    {
        _context.RoleSecurityClaims.Add(new RoleSecurityClaim
        {
            SecurityRoleId = 201,
            SecurityClaimId = 300,
            AllowOrDeny = 'A',
            CreatedDateTime = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        var result = await _service.RemoveClaimAsync(
            IdKeyHelper.Encode(201),
            IdKeyHelper.Encode(300));

        result.IsSuccess.Should().BeTrue();
        (await _context.RoleSecurityClaims.AnyAsync(
            rsc => rsc.SecurityRoleId == 201 && rsc.SecurityClaimId == 300))
            .Should().BeFalse();
    }

    [Fact]
    public async Task RemoveClaimAsync_NotAssociated_ReturnsNotFound()
    {
        var result = await _service.RemoveClaimAsync(
            IdKeyHelper.Encode(201),
            IdKeyHelper.Encode(300));

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("NOT_FOUND");
    }

    [Fact]
    public async Task AddMemberAsync_HappyPath_AddsMembership()
    {
        var roleIdKey = IdKeyHelper.Encode(201);
        var personIdKey = IdKeyHelper.Encode(100);
        var request = new AssignPersonRequest { PersonIdKey = personIdKey };

        var result = await _service.AddMemberAsync(roleIdKey, request);

        result.IsSuccess.Should().BeTrue();
        var membership = await _context.PersonSecurityRoles
            .FirstOrDefaultAsync(psr => psr.SecurityRoleId == 201 && psr.PersonId == 100);
        membership.Should().NotBeNull();
        membership!.ExpiresDateTime.Should().BeNull();
    }

    [Fact]
    public async Task AddMemberAsync_WithExpiration_SetsExpiration()
    {
        var roleIdKey = IdKeyHelper.Encode(201);
        var personIdKey = IdKeyHelper.Encode(100);
        var expiresAt = DateTime.UtcNow.AddDays(30);
        var request = new AssignPersonRequest { PersonIdKey = personIdKey, ExpiresDateTime = expiresAt };

        var result = await _service.AddMemberAsync(roleIdKey, request);

        result.IsSuccess.Should().BeTrue();
        var membership = await _context.PersonSecurityRoles
            .FirstOrDefaultAsync(psr => psr.SecurityRoleId == 201 && psr.PersonId == 100);
        membership!.ExpiresDateTime.Should().BeCloseTo(expiresAt, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task AddMemberAsync_AlreadyMember_UpdatesExpiration()
    {
        _context.PersonSecurityRoles.Add(new PersonSecurityRole
        {
            PersonId = 100,
            SecurityRoleId = 201,
            CreatedDateTime = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        var newExpires = DateTime.UtcNow.AddDays(7);
        var request = new AssignPersonRequest
        {
            PersonIdKey = IdKeyHelper.Encode(100),
            ExpiresDateTime = newExpires
        };

        var result = await _service.AddMemberAsync(IdKeyHelper.Encode(201), request);

        result.IsSuccess.Should().BeTrue();
        var memberships = await _context.PersonSecurityRoles
            .Where(psr => psr.SecurityRoleId == 201 && psr.PersonId == 100)
            .ToListAsync();
        memberships.Should().HaveCount(1);
        memberships[0].ExpiresDateTime.Should().BeCloseTo(newExpires, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task AddMemberAsync_InvalidPersonIdKey_ReturnsNotFound()
    {
        var request = new AssignPersonRequest { PersonIdKey = "!!!bad!!!" };

        var result = await _service.AddMemberAsync(IdKeyHelper.Encode(201), request);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("NOT_FOUND");
    }

    [Fact]
    public async Task RemoveMemberAsync_HappyPath_RemovesMembership()
    {
        _context.PersonSecurityRoles.Add(new PersonSecurityRole
        {
            PersonId = 100,
            SecurityRoleId = 201,
            CreatedDateTime = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        var result = await _service.RemoveMemberAsync(
            IdKeyHelper.Encode(201),
            IdKeyHelper.Encode(100));

        result.IsSuccess.Should().BeTrue();
        (await _context.PersonSecurityRoles.AnyAsync(
            psr => psr.SecurityRoleId == 201 && psr.PersonId == 100))
            .Should().BeFalse();
    }

    [Fact]
    public async Task RemoveMemberAsync_NotMember_ReturnsNotFound()
    {
        var result = await _service.RemoveMemberAsync(
            IdKeyHelper.Encode(201),
            IdKeyHelper.Encode(100));

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("NOT_FOUND");
    }

    [Fact]
    public async Task GetByIdKeyAsync_ReturnsRoleWithClaimsAndMembers()
    {
        _context.RoleSecurityClaims.Add(new RoleSecurityClaim
        {
            SecurityRoleId = 201,
            SecurityClaimId = 300,
            AllowOrDeny = 'A',
            CreatedDateTime = DateTime.UtcNow
        });
        _context.PersonSecurityRoles.Add(new PersonSecurityRole
        {
            PersonId = 100,
            SecurityRoleId = 201,
            CreatedDateTime = DateTime.UtcNow
        });
        _context.PersonSecurityRoles.Add(new PersonSecurityRole
        {
            PersonId = 101,
            SecurityRoleId = 201,
            ExpiresDateTime = DateTime.UtcNow.AddDays(-1), // expired — should not count
            CreatedDateTime = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        var result = await _service.GetByIdKeyAsync(IdKeyHelper.Encode(201));

        result.IsSuccess.Should().BeTrue();
        var detail = result.Value!;
        detail.Name.Should().Be("Volunteer Coordinator");
        detail.Claims.Should().HaveCount(1);
        detail.Claims[0].AllowOrDeny.Should().Be('A');
        detail.Members.Should().HaveCount(1, "expired members are excluded");
        detail.Members[0].PersonName.Should().Contain("Alice");
    }

    [Fact]
    public async Task GetByIdKeyAsync_InvalidIdKey_ReturnsNotFound()
    {
        var result = await _service.GetByIdKeyAsync("!!!bad!!!");

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("NOT_FOUND");
    }

    [Fact]
    public async Task GetAllClaimsAsync_ReturnsAllClaims()
    {
        var result = await _service.GetAllClaimsAsync();

        result.IsSuccess.Should().BeTrue();
        result.Value!.Should().HaveCount(2);
        result.Value.Should().Contain(c => c.ClaimType == "people" && c.ClaimValue == "view");
        result.Value.Should().Contain(c => c.ClaimType == "people" && c.ClaimValue == "edit");
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        GC.SuppressFinalize(this);
    }
}
