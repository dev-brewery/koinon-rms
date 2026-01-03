using FluentAssertions;
using Koinon.Application.Services;
using Koinon.Domain.Entities;
using Koinon.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Koinon.Application.Tests.Services;

/// <summary>
/// Unit tests for SecurityClaimService.
/// Tests RBAC authorization with DENY-takes-precedence logic.
/// </summary>
public class SecurityClaimServiceTests : IDisposable
{
    private readonly Mock<ILogger<SecurityClaimService>> _loggerMock;
    private readonly KoinonDbContext _context;
    private readonly SecurityClaimService _service;

    public SecurityClaimServiceTests()
    {
        _loggerMock = new Mock<ILogger<SecurityClaimService>>();

        // Setup in-memory database
        var options = new DbContextOptionsBuilder<KoinonDbContext>()
            .UseInMemoryDatabase(databaseName: $"SecurityClaimTest_{Guid.NewGuid()}")
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new KoinonDbContext(options);
        _context.Database.EnsureCreated();

        _service = new SecurityClaimService(_context, _loggerMock.Object);

        // Seed test data
        SeedTestData();
    }

    private void SeedTestData()
    {
        // Create test person
        var person = new Person
        {
            Id = 1,
            FirstName = "Test",
            LastName = "User",
            Email = "test@example.com",
            Gender = Domain.Enums.Gender.Unknown,
            CreatedDateTime = DateTime.UtcNow
        };
        _context.People.Add(person);

        // Create security roles
        var adminRole = new SecurityRole
        {
            Id = 1,
            Name = "Administrator",
            Description = "Full system access",
            IsActive = true,
            CreatedDateTime = DateTime.UtcNow
        };

        var editorRole = new SecurityRole
        {
            Id = 2,
            Name = "Editor",
            Description = "Can edit content",
            IsActive = true,
            CreatedDateTime = DateTime.UtcNow
        };

        var viewerRole = new SecurityRole
        {
            Id = 3,
            Name = "Viewer",
            Description = "Read-only access",
            IsActive = true,
            CreatedDateTime = DateTime.UtcNow
        };

        _context.SecurityRoles.AddRange(adminRole, editorRole, viewerRole);

        // Create security claims
        var editPersonClaim = new SecurityClaim
        {
            Id = 1,
            ClaimType = "permission",
            ClaimValue = "person:edit",
            Description = "Can edit person records",
            CreatedDateTime = DateTime.UtcNow
        };

        var viewPersonClaim = new SecurityClaim
        {
            Id = 2,
            ClaimType = "permission",
            ClaimValue = "person:view",
            Description = "Can view person records",
            CreatedDateTime = DateTime.UtcNow
        };

        var deletePersonClaim = new SecurityClaim
        {
            Id = 3,
            ClaimType = "permission",
            ClaimValue = "person:delete",
            Description = "Can delete person records",
            CreatedDateTime = DateTime.UtcNow
        };

        _context.SecurityClaims.AddRange(editPersonClaim, viewPersonClaim, deletePersonClaim);

        // Assign roles to person (non-expired)
        var personAdminRole = new PersonSecurityRole
        {
            Id = 1,
            PersonId = 1,
            SecurityRoleId = 1,
            ExpiresDateTime = null, // Never expires
            CreatedDateTime = DateTime.UtcNow
        };

        var personEditorRole = new PersonSecurityRole
        {
            Id = 2,
            PersonId = 1,
            SecurityRoleId = 2,
            ExpiresDateTime = DateTime.UtcNow.AddDays(30), // Future expiration
            CreatedDateTime = DateTime.UtcNow
        };

        _context.PersonSecurityRoles.AddRange(personAdminRole, personEditorRole);

        // Assign claims to roles
        // Admin role: ALLOW person:edit
        var adminEditClaim = new RoleSecurityClaim
        {
            Id = 1,
            SecurityRoleId = 1,
            SecurityClaimId = 1,
            AllowOrDeny = 'A',
            CreatedDateTime = DateTime.UtcNow
        };

        // Admin role: ALLOW person:view
        var adminViewClaim = new RoleSecurityClaim
        {
            Id = 2,
            SecurityRoleId = 1,
            SecurityClaimId = 2,
            AllowOrDeny = 'A',
            CreatedDateTime = DateTime.UtcNow
        };

        // Editor role: ALLOW person:view
        var editorViewClaim = new RoleSecurityClaim
        {
            Id = 3,
            SecurityRoleId = 2,
            SecurityClaimId = 2,
            AllowOrDeny = 'A',
            CreatedDateTime = DateTime.UtcNow
        };

        // Editor role: DENY person:delete (explicit deny)
        var editorDenyDeleteClaim = new RoleSecurityClaim
        {
            Id = 4,
            SecurityRoleId = 2,
            SecurityClaimId = 3,
            AllowOrDeny = 'D',
            CreatedDateTime = DateTime.UtcNow
        };

        _context.RoleSecurityClaims.AddRange(
            adminEditClaim,
            adminViewClaim,
            editorViewClaim,
            editorDenyDeleteClaim
        );

        _context.SaveChanges();
    }

    [Fact]
    public async Task HasClaimAsync_WithAllowClaim_ReturnsTrue()
    {
        // Arrange
        var personId = 1;
        var claimType = "permission";
        var claimValue = "person:edit";

        // Act
        var result = await _service.HasClaimAsync(personId, claimType, claimValue);

        // Assert
        result.Should().BeTrue("person has ALLOW claim for person:edit through admin role");
    }

    [Fact]
    public async Task HasClaimAsync_WithNoClaim_ReturnsFalse()
    {
        // Arrange
        var personId = 1;
        var claimType = "permission";
        var claimValue = "finance:edit"; // Claim doesn't exist for this person

        // Act
        var result = await _service.HasClaimAsync(personId, claimType, claimValue);

        // Assert
        result.Should().BeFalse("person has no claim for finance:edit");
    }

    [Fact]
    public async Task HasClaimAsync_WithDenyClaim_ReturnsFalse()
    {
        // Arrange
        var personId = 1;
        var claimType = "permission";
        var claimValue = "person:delete";

        // Act
        var result = await _service.HasClaimAsync(personId, claimType, claimValue);

        // Assert
        result.Should().BeFalse("DENY takes precedence - editor role has explicit DENY for person:delete");
    }

    [Fact]
    public async Task HasClaimAsync_WithDenyTakesPrecedenceOverAllow_ReturnsFalse()
    {
        // Arrange
        // Create a scenario where one role allows and another denies the same claim
        var allowDenyRole = new SecurityRole
        {
            Id = 4,
            Name = "AllowDenyTest",
            IsActive = true,
            CreatedDateTime = DateTime.UtcNow
        };
        _context.SecurityRoles.Add(allowDenyRole);

        var personAllowDenyRole = new PersonSecurityRole
        {
            Id = 3,
            PersonId = 1,
            SecurityRoleId = 4,
            ExpiresDateTime = null,
            CreatedDateTime = DateTime.UtcNow
        };
        _context.PersonSecurityRoles.Add(personAllowDenyRole);

        // This role DENIES person:edit (while admin role ALLOWS it)
        var denyEditClaim = new RoleSecurityClaim
        {
            Id = 5,
            SecurityRoleId = 4,
            SecurityClaimId = 1, // person:edit
            AllowOrDeny = 'D',
            CreatedDateTime = DateTime.UtcNow
        };
        _context.RoleSecurityClaims.Add(denyEditClaim);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.HasClaimAsync(1, "permission", "person:edit");

        // Assert
        result.Should().BeFalse("DENY takes precedence even when another role allows the same claim");
    }

    [Fact]
    public async Task HasClaimAsync_WithExpiredRole_ReturnsFalse()
    {
        // Arrange
        // Create an expired role with a claim
        var expiredRole = new SecurityRole
        {
            Id = 5,
            Name = "ExpiredRole",
            IsActive = true,
            CreatedDateTime = DateTime.UtcNow
        };
        _context.SecurityRoles.Add(expiredRole);

        var expiredPersonRole = new PersonSecurityRole
        {
            Id = 4,
            PersonId = 1,
            SecurityRoleId = 5,
            ExpiresDateTime = DateTime.UtcNow.AddDays(-1), // Expired yesterday
            CreatedDateTime = DateTime.UtcNow.AddDays(-30)
        };
        _context.PersonSecurityRoles.Add(expiredPersonRole);

        var expiredClaim = new SecurityClaim
        {
            Id = 4,
            ClaimType = "permission",
            ClaimValue = "expired:claim",
            CreatedDateTime = DateTime.UtcNow
        };
        _context.SecurityClaims.Add(expiredClaim);

        var expiredRoleClaim = new RoleSecurityClaim
        {
            Id = 6,
            SecurityRoleId = 5,
            SecurityClaimId = 4,
            AllowOrDeny = 'A',
            CreatedDateTime = DateTime.UtcNow
        };
        _context.RoleSecurityClaims.Add(expiredRoleClaim);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.HasClaimAsync(1, "permission", "expired:claim");

        // Assert
        result.Should().BeFalse("expired roles should not grant claims");
    }

    [Fact]
    public async Task HasClaimAsync_WithNoRoles_ReturnsFalse()
    {
        // Arrange
        var personWithoutRoles = new Person
        {
            Id = 2,
            FirstName = "No",
            LastName = "Roles",
            Email = "noroles@example.com",
            Gender = Domain.Enums.Gender.Unknown,
            CreatedDateTime = DateTime.UtcNow
        };
        _context.People.Add(personWithoutRoles);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.HasClaimAsync(2, "permission", "person:view");

        // Assert
        result.Should().BeFalse("person with no roles should have no claims");
    }

    [Fact]
    public async Task GetPersonClaimsAsync_ReturnsAllAllowedClaims()
    {
        // Arrange
        var personId = 1;
        var claimType = "permission";

        // Act
        var result = await _service.GetPersonClaimsAsync(personId, claimType);

        // Assert
        var claims = result.ToList();
        claims.Should().Contain("person:edit", "admin role allows person:edit");
        claims.Should().Contain("person:view", "both admin and editor roles allow person:view");
        claims.Should().NotContain("person:delete", "editor role denies person:delete");
    }

    [Fact]
    public async Task GetPersonClaimsAsync_WithNoRoles_ReturnsEmpty()
    {
        // Arrange
        var personWithoutRoles = new Person
        {
            Id = 3,
            FirstName = "Empty",
            LastName = "Claims",
            Email = "empty@example.com",
            Gender = Domain.Enums.Gender.Unknown,
            CreatedDateTime = DateTime.UtcNow
        };
        _context.People.Add(personWithoutRoles);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetPersonClaimsAsync(3, "permission");

        // Assert
        result.Should().BeEmpty("person with no roles should have no claims");
    }

    [Fact]
    public async Task GetPersonClaimsAsync_ExcludesDeniedClaims()
    {
        // Arrange
        var personId = 1;
        var claimType = "permission";

        // Act
        var result = await _service.GetPersonClaimsAsync(personId, claimType);

        // Assert
        var claims = result.ToList();
        claims.Should().NotContain("person:delete", "DENY claims should be excluded from results");
    }

    [Fact]
    public async Task HasAnyClaimAsync_WithOneAllowedClaim_ReturnsTrue()
    {
        // Arrange
        var personId = 1;
        var claimType = "permission";
        var claimValues = new[] { "person:edit", "finance:edit" };

        // Act
        var result = await _service.HasAnyClaimAsync(personId, claimType, claimValues);

        // Assert
        result.Should().BeTrue("person has person:edit claim");
    }

    [Fact]
    public async Task HasAnyClaimAsync_WithNoAllowedClaims_ReturnsFalse()
    {
        // Arrange
        var personId = 1;
        var claimType = "permission";
        var claimValues = new[] { "finance:edit", "admin:configure" };

        // Act
        var result = await _service.HasAnyClaimAsync(personId, claimType, claimValues);

        // Assert
        result.Should().BeFalse("person has none of the requested claims");
    }

    [Fact]
    public async Task HasAnyClaimAsync_WithDeniedClaim_SkipsToNextClaim()
    {
        // Arrange
        var personId = 1;
        var claimType = "permission";
        // person:delete is denied, but person:view is allowed
        var claimValues = new[] { "person:delete", "person:view" };

        // Act
        var result = await _service.HasAnyClaimAsync(personId, claimType, claimValues);

        // Assert
        result.Should().BeTrue("person:view is allowed even though person:delete is denied");
    }

    [Fact]
    public async Task HasAnyClaimAsync_WithEmptyClaimValues_ReturnsFalse()
    {
        // Arrange
        var personId = 1;
        var claimType = "permission";
        var claimValues = Array.Empty<string>();

        // Act
        var result = await _service.HasAnyClaimAsync(personId, claimType, claimValues);

        // Assert
        result.Should().BeFalse("empty claim values should return false");
    }

    [Fact]
    public async Task HasAllClaimsAsync_WithAllAllowedClaims_ReturnsTrue()
    {
        // Arrange
        var personId = 1;
        var claimType = "permission";
        var claimValues = new[] { "person:edit", "person:view" };

        // Act
        var result = await _service.HasAllClaimsAsync(personId, claimType, claimValues);

        // Assert
        result.Should().BeTrue("person has both person:edit and person:view claims");
    }

    [Fact]
    public async Task HasAllClaimsAsync_WithOneDeniedClaim_ReturnsFalse()
    {
        // Arrange
        var personId = 1;
        var claimType = "permission";
        // person:delete is denied
        var claimValues = new[] { "person:view", "person:delete" };

        // Act
        var result = await _service.HasAllClaimsAsync(personId, claimType, claimValues);

        // Assert
        result.Should().BeFalse("person:delete is denied, so not all claims are allowed");
    }

    [Fact]
    public async Task HasAllClaimsAsync_WithOneMissingClaim_ReturnsFalse()
    {
        // Arrange
        var personId = 1;
        var claimType = "permission";
        var claimValues = new[] { "person:edit", "finance:edit" };

        // Act
        var result = await _service.HasAllClaimsAsync(personId, claimType, claimValues);

        // Assert
        result.Should().BeFalse("finance:edit claim doesn't exist for this person");
    }

    [Fact]
    public async Task HasAllClaimsAsync_WithEmptyClaimValues_ReturnsTrue()
    {
        // Arrange
        var personId = 1;
        var claimType = "permission";
        var claimValues = Array.Empty<string>();

        // Act
        var result = await _service.HasAllClaimsAsync(personId, claimType, claimValues);

        // Assert
        result.Should().BeTrue("vacuous truth: all of zero claims are satisfied");
    }

    [Fact]
    public async Task GetPersonRolesAsync_ReturnsActiveRoles()
    {
        // Arrange
        var personId = 1;

        // Act
        var result = await _service.GetPersonRolesAsync(personId);

        // Assert
        var roles = result.ToList();
        roles.Should().HaveCount(2, "person has 2 non-expired roles");
        roles.Should().Contain(r => r.Name == "Administrator");
        roles.Should().Contain(r => r.Name == "Editor");
    }

    [Fact]
    public async Task GetPersonRolesAsync_ExcludesExpiredRoles()
    {
        // Arrange
        // Add an expired role
        var expiredRole = new SecurityRole
        {
            Id = 6,
            Name = "ExpiredTestRole",
            IsActive = true,
            CreatedDateTime = DateTime.UtcNow
        };
        _context.SecurityRoles.Add(expiredRole);

        var expiredPersonRole = new PersonSecurityRole
        {
            Id = 5,
            PersonId = 1,
            SecurityRoleId = 6,
            ExpiresDateTime = DateTime.UtcNow.AddDays(-5), // Expired 5 days ago
            CreatedDateTime = DateTime.UtcNow.AddMonths(-1)
        };
        _context.PersonSecurityRoles.Add(expiredPersonRole);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetPersonRolesAsync(1);

        // Assert
        var roles = result.ToList();
        roles.Should().NotContain(r => r.Name == "ExpiredTestRole", "expired roles should be excluded");
    }

    [Fact]
    public async Task GetPersonRolesAsync_WithNoRoles_ReturnsEmpty()
    {
        // Arrange
        var personWithoutRoles = new Person
        {
            Id = 4,
            FirstName = "No",
            LastName = "Roles",
            Email = "noroles2@example.com",
            Gender = Domain.Enums.Gender.Unknown,
            CreatedDateTime = DateTime.UtcNow
        };
        _context.People.Add(personWithoutRoles);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetPersonRolesAsync(4);

        // Assert
        result.Should().BeEmpty("person with no roles should return empty list");
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
