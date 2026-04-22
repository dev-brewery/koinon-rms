using System.Security.Claims;
using FluentAssertions;
using Koinon.Api.Services;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;

namespace Koinon.Api.Tests.Services;

/// <summary>
/// Unit tests for <see cref="HttpContextUserContext"/>.
/// Focuses on the #668 regression: CurrentPersonId must resolve whether the JWT
/// handler surfaces the person identifier as ClaimTypes.NameIdentifier (the
/// standard, always-mapped claim) or the custom "person_id" claim.
/// </summary>
public class HttpContextUserContextTests
{
    private static HttpContextUserContext CreateSut(params Claim[] claims)
    {
        var identity = new ClaimsIdentity(claims, authenticationType: "TestJwt");
        var principal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext { User = principal };
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.Setup(a => a.HttpContext).Returns(httpContext);
        return new HttpContextUserContext(accessor.Object);
    }

    [Fact]
    public void CurrentPersonId_WithNameIdentifierClaim_ReturnsPersonId()
    {
        // Arrange: only the standard NameIdentifier claim is present (the case
        // when the JWT handler's inbound claim map rewrites "sub"/"nameid" and
        // the custom "person_id" claim is missing or dropped).
        var sut = CreateSut(new Claim(ClaimTypes.NameIdentifier, "42"));

        // Act
        var result = sut.CurrentPersonId;

        // Assert
        result.Should().Be(42);
    }

    [Fact]
    public void CurrentPersonId_WithPersonIdClaimOnly_ReturnsPersonId()
    {
        // Arrange: only the custom "person_id" claim is present (the case when
        // MapInboundClaims is disabled and NameIdentifier is not produced).
        var sut = CreateSut(new Claim("person_id", "99"));

        // Act
        var result = sut.CurrentPersonId;

        // Assert
        result.Should().Be(99);
    }

    [Fact]
    public void CurrentPersonId_WithBothClaims_PrefersNameIdentifier()
    {
        // Arrange: AuthService emits both claims with the same value. Either
        // would be correct, but NameIdentifier is the canonical source.
        var sut = CreateSut(
            new Claim(ClaimTypes.NameIdentifier, "7"),
            new Claim("person_id", "7"));

        // Act
        var result = sut.CurrentPersonId;

        // Assert
        result.Should().Be(7);
    }

    [Fact]
    public void CurrentPersonId_WithNoRelevantClaims_ReturnsNull()
    {
        // Arrange
        var sut = CreateSut(new Claim(ClaimTypes.Email, "a@b.c"));

        // Act
        var result = sut.CurrentPersonId;

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void CurrentPersonId_WithNonNumericNameIdentifier_ReturnsNull()
    {
        // Arrange: defensive — if the claim contains a non-integer value the
        // accessor must return null rather than throw.
        var sut = CreateSut(new Claim(ClaimTypes.NameIdentifier, "not-a-number"));

        // Act
        var result = sut.CurrentPersonId;

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void CurrentPersonId_WithNullHttpContext_ReturnsNull()
    {
        // Arrange
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.Setup(a => a.HttpContext).Returns((HttpContext?)null);
        var sut = new HttpContextUserContext(accessor.Object);

        // Act
        var result = sut.CurrentPersonId;

        // Assert
        result.Should().BeNull();
    }
}
