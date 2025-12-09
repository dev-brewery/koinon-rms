using System.Security.Claims;
using FluentAssertions;
using Koinon.Api.Controllers;
using Koinon.Application.DTOs;
using Koinon.Application.DTOs.Requests;
using Koinon.Application.Interfaces;
using Koinon.Domain.Data;
using Koinon.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Koinon.Api.Tests.Controllers;

public class PickupControllerTests
{
    private readonly Mock<IAuthorizedPickupService> _authorizedPickupServiceMock;
    private readonly Mock<IPickupRateLimitService> _rateLimitServiceMock;
    private readonly Mock<ILogger<PickupController>> _loggerMock;
    private readonly PickupController _controller;
    private readonly PickupController _supervisorController;

    // Valid IdKeys and IDs for testing
    private readonly string _attendanceIdKey = IdKeyHelper.Encode(100);
    private readonly string _childIdKey = IdKeyHelper.Encode(50);
    private readonly string _authorizedPickupIdKey = IdKeyHelper.Encode(200);
    private readonly int _regularUserId = 42;
    private readonly int _supervisorUserId = 99;

    public PickupControllerTests()
    {
        _authorizedPickupServiceMock = new Mock<IAuthorizedPickupService>();
        _rateLimitServiceMock = new Mock<IPickupRateLimitService>();
        _loggerMock = new Mock<ILogger<PickupController>>();

        // Setup controller with regular user (CheckInVolunteer role)
        _controller = new PickupController(
            _authorizedPickupServiceMock.Object,
            _rateLimitServiceMock.Object,
            _loggerMock.Object);

        var regularClaims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, _regularUserId.ToString()),
            new Claim(ClaimTypes.Email, "volunteer@example.com"),
            new Claim(ClaimTypes.Name, "Jane Volunteer"),
            new Claim(ClaimTypes.Role, "CheckInVolunteer")
        };
        var regularIdentity = new ClaimsIdentity(regularClaims, "TestAuth");
        var regularPrincipal = new ClaimsPrincipal(regularIdentity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = regularPrincipal
            }
        };

        // Setup controller with supervisor user
        _supervisorController = new PickupController(
            _authorizedPickupServiceMock.Object,
            _rateLimitServiceMock.Object,
            _loggerMock.Object);

        var supervisorClaims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, _supervisorUserId.ToString()),
            new Claim(ClaimTypes.Email, "supervisor@example.com"),
            new Claim(ClaimTypes.Name, "John Supervisor"),
            new Claim(ClaimTypes.Role, "Supervisor")
        };
        var supervisorIdentity = new ClaimsIdentity(supervisorClaims, "TestAuth");
        var supervisorPrincipal = new ClaimsPrincipal(supervisorIdentity);

        _supervisorController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = supervisorPrincipal
            }
        };
    }

    #region VerifyPickup Tests

    [Fact]
    public async Task VerifyPickup_WithValidRequest_ReturnsOkWithResult()
    {
        // Arrange
        var request = new VerifyPickupRequest(
            AttendanceIdKey: _attendanceIdKey,
            PickupPersonIdKey: IdKeyHelper.Encode(75),
            PickupPersonName: "Sarah Smith",
            SecurityCode: "1234");

        var expectedResult = new PickupVerificationResultDto(
            IsAuthorized: true,
            AuthorizationLevel: AuthorizationLevel.Always,
            AuthorizedPickupIdKey: _authorizedPickupIdKey,
            Message: "Authorized to pick up child",
            RequiresSupervisorOverride: false);

        _authorizedPickupServiceMock
            .Setup(s => s.VerifyPickupAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.VerifyPickup(request);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var verificationResult = okResult.Value.Should().BeAssignableTo<PickupVerificationResultDto>().Subject;
        verificationResult.IsAuthorized.Should().BeTrue();
        verificationResult.AuthorizationLevel.Should().Be(AuthorizationLevel.Always);
        verificationResult.RequiresSupervisorOverride.Should().BeFalse();
        verificationResult.Message.Should().Be("Authorized to pick up child");
    }

    [Fact]
    public async Task VerifyPickup_WithMissingAttendanceIdKey_ReturnsBadRequest()
    {
        // Arrange
        var request = new VerifyPickupRequest(
            AttendanceIdKey: "",
            PickupPersonIdKey: IdKeyHelper.Encode(75),
            PickupPersonName: "Sarah Smith",
            SecurityCode: "1234");

        // Act
        var result = await _controller.VerifyPickup(request);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var problemDetails = badRequestResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Title.Should().Be("Invalid request");
        problemDetails.Detail.Should().Contain("AttendanceIdKey is required");
    }

    [Fact]
    public async Task VerifyPickup_WithMissingSecurityCode_ReturnsBadRequest()
    {
        // Arrange
        var request = new VerifyPickupRequest(
            AttendanceIdKey: _attendanceIdKey,
            PickupPersonIdKey: IdKeyHelper.Encode(75),
            PickupPersonName: "Sarah Smith",
            SecurityCode: "");

        // Act
        var result = await _controller.VerifyPickup(request);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var problemDetails = badRequestResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Title.Should().Be("Invalid request");
        problemDetails.Detail.Should().Contain("SecurityCode is required");
    }

    [Fact]
    public async Task VerifyPickup_WithUnauthorizedPerson_ReturnsOkWithUnauthorizedResult()
    {
        // Arrange
        var request = new VerifyPickupRequest(
            AttendanceIdKey: _attendanceIdKey,
            PickupPersonIdKey: IdKeyHelper.Encode(75),
            PickupPersonName: "Unknown Person",
            SecurityCode: "9999");

        var expectedResult = new PickupVerificationResultDto(
            IsAuthorized: false,
            AuthorizationLevel: null,
            AuthorizedPickupIdKey: null,
            Message: "Person is not authorized to pick up this child",
            RequiresSupervisorOverride: true);

        _authorizedPickupServiceMock
            .Setup(s => s.VerifyPickupAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.VerifyPickup(request);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var verificationResult = okResult.Value.Should().BeAssignableTo<PickupVerificationResultDto>().Subject;
        verificationResult.IsAuthorized.Should().BeFalse();
        verificationResult.RequiresSupervisorOverride.Should().BeTrue();
    }

    [Fact]
    public async Task VerifyPickup_WhenRateLimited_Returns429()
    {
        // Arrange
        var request = new VerifyPickupRequest(
            AttendanceIdKey: _attendanceIdKey,
            PickupPersonIdKey: IdKeyHelper.Encode(75),
            PickupPersonName: "Sarah Smith",
            SecurityCode: "1234");

        _rateLimitServiceMock
            .Setup(s => s.IsRateLimited(_attendanceIdKey, It.IsAny<string>()))
            .Returns(true);

        _rateLimitServiceMock
            .Setup(s => s.GetRetryAfter(_attendanceIdKey, It.IsAny<string>()))
            .Returns(TimeSpan.FromMinutes(10));

        // Act
        var result = await _controller.VerifyPickup(request);

        // Assert
        var statusCodeResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusCodeResult.StatusCode.Should().Be(StatusCodes.Status429TooManyRequests);
        var problemDetails = statusCodeResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Title.Should().Be("Too Many Requests");
        problemDetails.Detail.Should().Contain("Too many failed pickup verification attempts");

        // Verify Retry-After header was set
        _controller.Response.Headers.Should().ContainKey("Retry-After");
    }

    [Fact]
    public async Task VerifyPickup_OnFailedVerification_RecordsFailedAttempt()
    {
        // Arrange
        var request = new VerifyPickupRequest(
            AttendanceIdKey: _attendanceIdKey,
            PickupPersonIdKey: IdKeyHelper.Encode(75),
            PickupPersonName: "Sarah Smith",
            SecurityCode: "1234");

        var expectedResult = new PickupVerificationResultDto(
            IsAuthorized: false,
            AuthorizationLevel: AuthorizationLevel.Never,
            AuthorizedPickupIdKey: _authorizedPickupIdKey,
            Message: "Invalid security code",
            RequiresSupervisorOverride: false);

        _authorizedPickupServiceMock
            .Setup(s => s.VerifyPickupAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        _rateLimitServiceMock
            .Setup(s => s.IsRateLimited(_attendanceIdKey, It.IsAny<string>()))
            .Returns(false);

        // Act
        var result = await _controller.VerifyPickup(request);

        // Assert
        result.Should().BeOfType<OkObjectResult>();

        // Verify failed attempt was recorded
        _rateLimitServiceMock.Verify(
            s => s.RecordFailedAttempt(_attendanceIdKey, It.IsAny<string>()),
            Times.Once);

        // Verify reset was NOT called
        _rateLimitServiceMock.Verify(
            s => s.ResetAttempts(It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task VerifyPickup_OnSuccessfulVerification_ResetsAttempts()
    {
        // Arrange
        var request = new VerifyPickupRequest(
            AttendanceIdKey: _attendanceIdKey,
            PickupPersonIdKey: IdKeyHelper.Encode(75),
            PickupPersonName: "Sarah Smith",
            SecurityCode: "1234");

        var expectedResult = new PickupVerificationResultDto(
            IsAuthorized: true,
            AuthorizationLevel: AuthorizationLevel.Always,
            AuthorizedPickupIdKey: _authorizedPickupIdKey,
            Message: "Authorized to pick up child",
            RequiresSupervisorOverride: false);

        _authorizedPickupServiceMock
            .Setup(s => s.VerifyPickupAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        _rateLimitServiceMock
            .Setup(s => s.IsRateLimited(_attendanceIdKey, It.IsAny<string>()))
            .Returns(false);

        // Act
        var result = await _controller.VerifyPickup(request);

        // Assert
        result.Should().BeOfType<OkObjectResult>();

        // Verify reset was called
        _rateLimitServiceMock.Verify(
            s => s.ResetAttempts(_attendanceIdKey, It.IsAny<string>()),
            Times.Once);

        // Verify failed attempt was NOT recorded
        _rateLimitServiceMock.Verify(
            s => s.RecordFailedAttempt(It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task VerifyPickup_WithSupervisorOverrideRequired_DoesNotRecordFailedAttempt()
    {
        // Arrange
        var request = new VerifyPickupRequest(
            AttendanceIdKey: _attendanceIdKey,
            PickupPersonIdKey: IdKeyHelper.Encode(75),
            PickupPersonName: "Unknown Person",
            SecurityCode: "1234");

        var expectedResult = new PickupVerificationResultDto(
            IsAuthorized: false,
            AuthorizationLevel: AuthorizationLevel.EmergencyOnly,
            AuthorizedPickupIdKey: _authorizedPickupIdKey,
            Message: "Emergency-only authorization. Supervisor approval required.",
            RequiresSupervisorOverride: true);

        _authorizedPickupServiceMock
            .Setup(s => s.VerifyPickupAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        _rateLimitServiceMock
            .Setup(s => s.IsRateLimited(_attendanceIdKey, It.IsAny<string>()))
            .Returns(false);

        // Act
        var result = await _controller.VerifyPickup(request);

        // Assert
        result.Should().BeOfType<OkObjectResult>();

        // Verify neither failed attempt nor reset was called
        // (supervisor override scenarios are legitimate, not brute-force attempts)
        _rateLimitServiceMock.Verify(
            s => s.RecordFailedAttempt(It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);

        _rateLimitServiceMock.Verify(
            s => s.ResetAttempts(It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task VerifyPickup_UsesRemoteIpAddress_NotXForwardedForHeader()
    {
        // Arrange
        var request = new VerifyPickupRequest(
            AttendanceIdKey: _attendanceIdKey,
            PickupPersonIdKey: IdKeyHelper.Encode(75),
            PickupPersonName: "Sarah Smith",
            SecurityCode: "1234");

        var expectedResult = new PickupVerificationResultDto(
            IsAuthorized: false,
            AuthorizationLevel: AuthorizationLevel.Never,
            AuthorizedPickupIdKey: _authorizedPickupIdKey,
            Message: "Invalid security code",
            RequiresSupervisorOverride: false);

        _authorizedPickupServiceMock
            .Setup(s => s.VerifyPickupAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        _rateLimitServiceMock
            .Setup(s => s.IsRateLimited(_attendanceIdKey, It.IsAny<string>()))
            .Returns(false);

        // Setup HttpContext with a spoofed X-Forwarded-For header and a known RemoteIpAddress
        var httpContext = new DefaultHttpContext();
        httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("192.168.1.100");
        httpContext.Request.Headers["X-Forwarded-For"] = "10.0.0.1"; // Spoofed IP
        httpContext.User = _controller.ControllerContext.HttpContext.User;

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        // Act
        var result = await _controller.VerifyPickup(request);

        // Assert
        result.Should().BeOfType<OkObjectResult>();

        // Verify that the rate limiter was called with the RemoteIpAddress, NOT the X-Forwarded-For value
        _rateLimitServiceMock.Verify(
            s => s.RecordFailedAttempt(_attendanceIdKey, "192.168.1.100"),
            Times.Once,
            "Controller should use HttpContext.Connection.RemoteIpAddress (which is set by ForwardedHeaders middleware), not the raw X-Forwarded-For header");

        // Verify it was NOT called with the spoofed IP
        _rateLimitServiceMock.Verify(
            s => s.RecordFailedAttempt(_attendanceIdKey, "10.0.0.1"),
            Times.Never,
            "Controller should never use the raw X-Forwarded-For header value");
    }

    #endregion

    #region RecordPickup Tests

    [Fact]
    public async Task RecordPickup_WithValidRequest_ReturnsCreated()
    {
        // Arrange
        var request = new RecordPickupRequest(
            AttendanceIdKey: _attendanceIdKey,
            PickupPersonIdKey: IdKeyHelper.Encode(75),
            PickupPersonName: "Sarah Smith",
            WasAuthorized: true,
            AuthorizedPickupIdKey: _authorizedPickupIdKey,
            SupervisorOverride: false,
            SupervisorPersonIdKey: null,
            Notes: null);

        var expectedLog = new PickupLogDto(
            IdKey: IdKeyHelper.Encode(300),
            AttendanceIdKey: _attendanceIdKey,
            ChildIdKey: _childIdKey,
            ChildName: "Johnny Smith",
            PickupPersonName: "Sarah Smith",
            WasAuthorized: true,
            SupervisorOverride: false,
            SupervisorName: null,
            CheckoutDateTime: DateTime.UtcNow,
            Notes: null);

        _authorizedPickupServiceMock
            .Setup(s => s.RecordPickupAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedLog);

        // Act
        var result = await _controller.RecordPickup(request);

        // Assert
        var createdResult = result.Should().BeOfType<CreatedResult>().Subject;
        createdResult.StatusCode.Should().Be(StatusCodes.Status201Created);
        createdResult.Location.Should().Be($"/api/v1/people/{_childIdKey}/pickup-history");
        var pickupLog = createdResult.Value.Should().BeAssignableTo<PickupLogDto>().Subject;
        pickupLog.WasAuthorized.Should().BeTrue();
        pickupLog.SupervisorOverride.Should().BeFalse();
        pickupLog.PickupPersonName.Should().Be("Sarah Smith");
    }

    [Fact]
    public async Task RecordPickup_WithSupervisorOverride_WhenNotSupervisor_Returns403()
    {
        // Arrange
        var request = new RecordPickupRequest(
            AttendanceIdKey: _attendanceIdKey,
            PickupPersonIdKey: IdKeyHelper.Encode(75),
            PickupPersonName: "Unknown Person",
            WasAuthorized: false,
            AuthorizedPickupIdKey: null,
            SupervisorOverride: true,
            SupervisorPersonIdKey: IdKeyHelper.Encode(_regularUserId),
            Notes: "Emergency situation");

        // Act
        var result = await _controller.RecordPickup(request);

        // Assert
        var problemResult = result.Should().BeOfType<ObjectResult>().Subject;
        problemResult.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        var problemDetails = problemResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Title.Should().Be("Forbidden");
        problemDetails.Detail.Should().Contain("Supervisor override requires a user with Supervisor role");
    }

    [Fact]
    public async Task RecordPickup_WithSupervisorOverride_WhenSupervisor_Succeeds()
    {
        // Arrange
        var request = new RecordPickupRequest(
            AttendanceIdKey: _attendanceIdKey,
            PickupPersonIdKey: IdKeyHelper.Encode(75),
            PickupPersonName: "Unknown Person",
            WasAuthorized: false,
            AuthorizedPickupIdKey: null,
            SupervisorOverride: true,
            SupervisorPersonIdKey: IdKeyHelper.Encode(_supervisorUserId),
            Notes: "Emergency situation approved by supervisor");

        var expectedLog = new PickupLogDto(
            IdKey: IdKeyHelper.Encode(300),
            AttendanceIdKey: _attendanceIdKey,
            ChildIdKey: _childIdKey,
            ChildName: "Johnny Smith",
            PickupPersonName: "Unknown Person",
            WasAuthorized: false,
            SupervisorOverride: true,
            SupervisorName: "John Supervisor",
            CheckoutDateTime: DateTime.UtcNow,
            Notes: "Emergency situation approved by supervisor");

        _authorizedPickupServiceMock
            .Setup(s => s.RecordPickupAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedLog);

        // Act
        var result = await _supervisorController.RecordPickup(request);

        // Assert
        var createdResult = result.Should().BeOfType<CreatedResult>().Subject;
        createdResult.StatusCode.Should().Be(StatusCodes.Status201Created);
        createdResult.Location.Should().Be($"/api/v1/people/{_childIdKey}/pickup-history");
        var pickupLog = createdResult.Value.Should().BeAssignableTo<PickupLogDto>().Subject;
        pickupLog.SupervisorOverride.Should().BeTrue();
        pickupLog.SupervisorName.Should().Be("John Supervisor");
        pickupLog.WasAuthorized.Should().BeFalse();
    }

    [Fact]
    public async Task RecordPickup_WithMissingAttendanceIdKey_ReturnsBadRequest()
    {
        // Arrange
        var request = new RecordPickupRequest(
            AttendanceIdKey: "",
            PickupPersonIdKey: IdKeyHelper.Encode(75),
            PickupPersonName: "Sarah Smith",
            WasAuthorized: true,
            AuthorizedPickupIdKey: _authorizedPickupIdKey,
            SupervisorOverride: false,
            SupervisorPersonIdKey: null,
            Notes: null);

        // Act
        var result = await _controller.RecordPickup(request);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var problemDetails = badRequestResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Title.Should().Be("Invalid request");
        problemDetails.Detail.Should().Contain("AttendanceIdKey is required");
    }

    #endregion
}
