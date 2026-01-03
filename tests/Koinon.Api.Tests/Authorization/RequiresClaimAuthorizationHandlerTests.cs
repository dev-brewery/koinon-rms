using System.Security.Claims;
using FluentAssertions;
using Koinon.Api.Authorization;
using Koinon.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Koinon.Api.Tests.Authorization;

/// <summary>
/// Unit tests for RequiresClaimAuthorizationHandler.
/// Tests claim-based authorization handler logic.
/// </summary>
public class RequiresClaimAuthorizationHandlerTests
{
    private readonly Mock<ISecurityClaimService> _securityClaimServiceMock;
    private readonly Mock<ILogger<RequiresClaimAuthorizationHandler>> _loggerMock;
    private readonly RequiresClaimAuthorizationHandler _handler;

    public RequiresClaimAuthorizationHandlerTests()
    {
        _securityClaimServiceMock = new Mock<ISecurityClaimService>();
        _loggerMock = new Mock<ILogger<RequiresClaimAuthorizationHandler>>();
        _handler = new RequiresClaimAuthorizationHandler(
            _securityClaimServiceMock.Object,
            _loggerMock.Object
        );
    }

    [Fact]
    public async Task HandleRequirementAsync_WithValidClaim_Succeeds()
    {
        // Arrange
        var personId = 123;
        var claimType = "permission";
        var claimValue = "person:edit";

        var user = CreateClaimsPrincipal(personId);
        var requirement = new RequiresClaimRequirement(claimType, claimValue);
        var context = new AuthorizationHandlerContext(
            new[] { requirement },
            user,
            null
        );

        _securityClaimServiceMock
            .Setup(s => s.HasClaimAsync(personId, claimType, claimValue))
            .ReturnsAsync(true);

        // Act
        await _handler.HandleAsync(context);

        // Assert
        context.HasSucceeded.Should().BeTrue("user has the required claim");
        context.HasFailed.Should().BeFalse();

        _securityClaimServiceMock.Verify(
            s => s.HasClaimAsync(personId, claimType, claimValue),
            Times.Once
        );
    }

    [Fact]
    public async Task HandleRequirementAsync_WithoutClaim_Fails()
    {
        // Arrange
        var personId = 123;
        var claimType = "permission";
        var claimValue = "person:delete";

        var user = CreateClaimsPrincipal(personId);
        var requirement = new RequiresClaimRequirement(claimType, claimValue);
        var context = new AuthorizationHandlerContext(
            new[] { requirement },
            user,
            null
        );

        _securityClaimServiceMock
            .Setup(s => s.HasClaimAsync(personId, claimType, claimValue))
            .ReturnsAsync(false);

        // Act
        await _handler.HandleAsync(context);

        // Assert
        context.HasSucceeded.Should().BeFalse("user does not have the required claim");
        context.HasFailed.Should().BeTrue();

        _securityClaimServiceMock.Verify(
            s => s.HasClaimAsync(personId, claimType, claimValue),
            Times.Once
        );
    }

    [Fact]
    public async Task HandleRequirementAsync_WithAnonymousUser_Fails()
    {
        // Arrange
        var claimType = "permission";
        var claimValue = "person:view";

        // Create anonymous user (no claims)
        var user = new ClaimsPrincipal(new ClaimsIdentity());
        var requirement = new RequiresClaimRequirement(claimType, claimValue);
        var context = new AuthorizationHandlerContext(
            new[] { requirement },
            user,
            null
        );

        // Act
        await _handler.HandleAsync(context);

        // Assert
        context.HasSucceeded.Should().BeFalse("anonymous users should be denied");
        context.HasFailed.Should().BeTrue();

        // Should not call SecurityClaimService for anonymous users
        _securityClaimServiceMock.Verify(
            s => s.HasClaimAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never
        );
    }

    [Fact]
    public async Task HandleRequirementAsync_WithInvalidPersonId_Fails()
    {
        // Arrange
        var claimType = "permission";
        var claimValue = "person:edit";

        // Create user with invalid person ID claim
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "not-a-number")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var user = new ClaimsPrincipal(identity);

        var requirement = new RequiresClaimRequirement(claimType, claimValue);
        var context = new AuthorizationHandlerContext(
            new[] { requirement },
            user,
            null
        );

        // Act
        await _handler.HandleAsync(context);

        // Assert
        context.HasSucceeded.Should().BeFalse("invalid person ID should fail authorization");
        context.HasFailed.Should().BeTrue();

        // Should not call SecurityClaimService with invalid person ID
        _securityClaimServiceMock.Verify(
            s => s.HasClaimAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never
        );
    }

    [Fact]
    public async Task HandleRequirementAsync_WithSubClaim_Succeeds()
    {
        // Arrange
        var personId = 456;
        var claimType = "permission";
        var claimValue = "admin:configure";

        // Use "sub" claim instead of NameIdentifier (common in JWT tokens)
        var claims = new[]
        {
            new Claim("sub", personId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var user = new ClaimsPrincipal(identity);

        var requirement = new RequiresClaimRequirement(claimType, claimValue);
        var context = new AuthorizationHandlerContext(
            new[] { requirement },
            user,
            null
        );

        _securityClaimServiceMock
            .Setup(s => s.HasClaimAsync(personId, claimType, claimValue))
            .ReturnsAsync(true);

        // Act
        await _handler.HandleAsync(context);

        // Assert
        context.HasSucceeded.Should().BeTrue("handler should recognize 'sub' claim");
        context.HasFailed.Should().BeFalse();

        _securityClaimServiceMock.Verify(
            s => s.HasClaimAsync(personId, claimType, claimValue),
            Times.Once
        );
    }

    [Fact]
    public async Task HandleRequirementAsync_WithPersonIdClaim_Succeeds()
    {
        // Arrange
        var personId = 789;
        var claimType = "permission";
        var claimValue = "finance:view";

        // Use "personId" claim (custom claim type)
        var claims = new[]
        {
            new Claim("personId", personId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var user = new ClaimsPrincipal(identity);

        var requirement = new RequiresClaimRequirement(claimType, claimValue);
        var context = new AuthorizationHandlerContext(
            new[] { requirement },
            user,
            null
        );

        _securityClaimServiceMock
            .Setup(s => s.HasClaimAsync(personId, claimType, claimValue))
            .ReturnsAsync(true);

        // Act
        await _handler.HandleAsync(context);

        // Assert
        context.HasSucceeded.Should().BeTrue("handler should recognize 'personId' claim");
        context.HasFailed.Should().BeFalse();

        _securityClaimServiceMock.Verify(
            s => s.HasClaimAsync(personId, claimType, claimValue),
            Times.Once
        );
    }

    [Fact]
    public async Task HandleRequirementAsync_WithMultipleRequirements_OnlyHandlesRequiresClaim()
    {
        // Arrange
        var personId = 123;
        var claimType = "permission";
        var claimValue = "person:edit";

        var user = CreateClaimsPrincipal(personId);
        var claimRequirement = new RequiresClaimRequirement(claimType, claimValue);
        var otherRequirement = new Mock<IAuthorizationRequirement>().Object;

        var context = new AuthorizationHandlerContext(
            new[] { claimRequirement, otherRequirement },
            user,
            null
        );

        _securityClaimServiceMock
            .Setup(s => s.HasClaimAsync(personId, claimType, claimValue))
            .ReturnsAsync(true);

        // Act
        await _handler.HandleAsync(context);

        // Assert
        // Only the RequiresClaimRequirement should be marked as succeeded
        context.PendingRequirements.Should().Contain(otherRequirement, "other requirements should not be handled");
        context.PendingRequirements.Should().NotContain(claimRequirement, "claim requirement should be handled");
    }

    [Fact]
    public async Task HandleRequirementAsync_WithDeniedClaim_Fails()
    {
        // Arrange
        var personId = 123;
        var claimType = "permission";
        var claimValue = "person:delete";

        var user = CreateClaimsPrincipal(personId);
        var requirement = new RequiresClaimRequirement(claimType, claimValue);
        var context = new AuthorizationHandlerContext(
            new[] { requirement },
            user,
            null
        );

        // Service returns false (claim is denied or doesn't exist)
        _securityClaimServiceMock
            .Setup(s => s.HasClaimAsync(personId, claimType, claimValue))
            .ReturnsAsync(false);

        // Act
        await _handler.HandleAsync(context);

        // Assert
        context.HasSucceeded.Should().BeFalse("denied claims should fail authorization");
        context.HasFailed.Should().BeTrue();
    }

    [Fact]
    public async Task HandleRequirementAsync_UsesFirstAvailablePersonIdClaim()
    {
        // Arrange
        var personId = 999;
        var claimType = "permission";
        var claimValue = "test:claim";

        // User has multiple potential person ID claims
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, personId.ToString()),
            new Claim("sub", "different-id"),
            new Claim("personId", "another-id")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var user = new ClaimsPrincipal(identity);

        var requirement = new RequiresClaimRequirement(claimType, claimValue);
        var context = new AuthorizationHandlerContext(
            new[] { requirement },
            user,
            null
        );

        _securityClaimServiceMock
            .Setup(s => s.HasClaimAsync(personId, claimType, claimValue))
            .ReturnsAsync(true);

        // Act
        await _handler.HandleAsync(context);

        // Assert
        context.HasSucceeded.Should().BeTrue();

        // Should use the first claim (NameIdentifier)
        _securityClaimServiceMock.Verify(
            s => s.HasClaimAsync(personId, claimType, claimValue),
            Times.Once
        );
    }

    [Fact]
    public async Task HandleRequirementAsync_WithServiceException_Fails()
    {
        // Arrange
        var personId = 123;
        var claimType = "permission";
        var claimValue = "person:edit";

        var user = CreateClaimsPrincipal(personId);
        var requirement = new RequiresClaimRequirement(claimType, claimValue);
        var context = new AuthorizationHandlerContext(
            new[] { requirement },
            user,
            null
        );

        _securityClaimServiceMock
            .Setup(s => s.HasClaimAsync(personId, claimType, claimValue))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _handler.HandleAsync(context)
        );

        // Context should not have succeeded
        context.HasSucceeded.Should().BeFalse();
    }

    /// <summary>
    /// Helper method to create a ClaimsPrincipal with a person ID claim.
    /// </summary>
    private static ClaimsPrincipal CreateClaimsPrincipal(int personId)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, personId.ToString()),
            new Claim(ClaimTypes.Name, "Test User"),
            new Claim(ClaimTypes.Email, "test@example.com")
        };

        var identity = new ClaimsIdentity(claims, "TestAuth");
        return new ClaimsPrincipal(identity);
    }
}
