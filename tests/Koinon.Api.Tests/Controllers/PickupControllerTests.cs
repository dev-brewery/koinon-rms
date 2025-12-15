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
        _loggerMock = new Mock<ILogger<PickupController>>();

        // Setup controller with regular user (CheckInVolunteer role)
        _controller = new PickupController(
            _authorizedPickupServiceMock.Object,
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
        var response = okResult.Value!;
        var dataProperty = response.GetType().GetProperty("data");
        dataProperty.Should().NotBeNull("response should have a 'data' property");
        var verificationResult = dataProperty!.GetValue(response).Should().BeOfType<PickupVerificationResultDto>().Subject;
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
        var response = okResult.Value!;
        var dataProperty = response.GetType().GetProperty("data");
        dataProperty.Should().NotBeNull("response should have a 'data' property");
        var verificationResult = dataProperty!.GetValue(response).Should().BeOfType<PickupVerificationResultDto>().Subject;
        verificationResult.IsAuthorized.Should().BeFalse();
        verificationResult.RequiresSupervisorOverride.Should().BeTrue();
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
        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        var pickupLog = createdResult.Value.Should().BeOfType<PickupLogDto>().Subject;
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
        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        var pickupLog = createdResult.Value.Should().BeOfType<PickupLogDto>().Subject;
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

    #region GetPickupHistory Tests

    [Fact]
    public async Task GetPickupHistory_WithValidChildIdKey_ReturnsOkWithHistory()
    {
        // Arrange
        var expectedHistory = new List<PickupLogDto>
        {
            new(
                IdKey: IdKeyHelper.Encode(300),
                AttendanceIdKey: _attendanceIdKey,
                ChildName: "Johnny Smith",
                PickupPersonName: "Sarah Smith",
                WasAuthorized: true,
                SupervisorOverride: false,
                SupervisorName: null,
                CheckoutDateTime: DateTime.UtcNow.AddDays(-1),
                Notes: null),
            new(
                IdKey: IdKeyHelper.Encode(301),
                AttendanceIdKey: IdKeyHelper.Encode(101),
                ChildName: "Johnny Smith",
                PickupPersonName: "Mike Smith",
                WasAuthorized: true,
                SupervisorOverride: false,
                SupervisorName: null,
                CheckoutDateTime: DateTime.UtcNow.AddDays(-7),
                Notes: null)
        };

        _authorizedPickupServiceMock
            .Setup(s => s.GetPickupHistoryAsync(
                _childIdKey,
                null,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedHistory);

        // Act
        var result = await _supervisorController.GetPickupHistory(_childIdKey, null, null);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value!;
        var dataProperty = response.GetType().GetProperty("data");
        dataProperty.Should().NotBeNull("response should have a 'data' property");
        var history = dataProperty!.GetValue(response).Should().BeAssignableTo<IEnumerable<PickupLogDto>>().Subject.ToList();
        history.Should().HaveCount(2);
        history[0].ChildName.Should().Be("Johnny Smith");
        history[1].PickupPersonName.Should().Be("Mike Smith");
    }

    [Fact]
    public async Task GetPickupHistory_WithInvalidDateRange_ReturnsBadRequest()
    {
        // Arrange
        var fromDate = new DateTime(2024, 1, 15);
        var toDate = new DateTime(2024, 1, 10); // toDate before fromDate

        // Act
        var result = await _supervisorController.GetPickupHistory(_childIdKey, fromDate, toDate);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var problemDetails = badRequestResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Title.Should().Be("Invalid date range");
        problemDetails.Detail.Should().Contain("toDate' must be greater than or equal to 'fromDate");
    }

    [Fact]
    public async Task GetPickupHistory_WithDateRange_ReturnsFilteredHistory()
    {
        // Arrange
        var fromDate = new DateTime(2024, 1, 1);
        var toDate = new DateTime(2024, 1, 31);

        var expectedHistory = new List<PickupLogDto>
        {
            new(
                IdKey: IdKeyHelper.Encode(300),
                AttendanceIdKey: _attendanceIdKey,
                ChildName: "Johnny Smith",
                PickupPersonName: "Sarah Smith",
                WasAuthorized: true,
                SupervisorOverride: false,
                SupervisorName: null,
                CheckoutDateTime: new DateTime(2024, 1, 15, 12, 0, 0),
                Notes: null)
        };

        _authorizedPickupServiceMock
            .Setup(s => s.GetPickupHistoryAsync(
                _childIdKey,
                fromDate,
                toDate,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedHistory);

        // Act
        var result = await _supervisorController.GetPickupHistory(_childIdKey, fromDate, toDate);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value!;
        var dataProperty = response.GetType().GetProperty("data");
        dataProperty.Should().NotBeNull("response should have a 'data' property");
        var history = dataProperty!.GetValue(response).Should().BeAssignableTo<IEnumerable<PickupLogDto>>().Subject.ToList();
        history.Should().HaveCount(1);
        history[0].CheckoutDateTime.Should().BeAfter(fromDate).And.BeBefore(toDate);
    }

    [Fact]
    public async Task GetPickupHistory_WithNoHistory_ReturnsEmptyList()
    {
        // Arrange
        var expectedHistory = new List<PickupLogDto>();

        _authorizedPickupServiceMock
            .Setup(s => s.GetPickupHistoryAsync(
                _childIdKey,
                null,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedHistory);

        // Act
        var result = await _supervisorController.GetPickupHistory(_childIdKey, null, null);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value!;
        var dataProperty = response.GetType().GetProperty("data");
        dataProperty.Should().NotBeNull("response should have a 'data' property");
        var history = dataProperty!.GetValue(response).Should().BeAssignableTo<IEnumerable<PickupLogDto>>().Subject.ToList();
        history.Should().BeEmpty();
    }

    #endregion
}
