using System.Security.Claims;
using FluentAssertions;
using Koinon.Api.Controllers;
using Koinon.Application.DTOs;
using Koinon.Application.Interfaces;
using Koinon.Domain.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Koinon.Api.Tests.Controllers;

public class AuthorizedPickupControllerTests
{
    private readonly Mock<IAuthorizedPickupService> _authorizedPickupServiceMock;
    private readonly Mock<ILogger<AuthorizedPickupController>> _loggerMock;
    private readonly AuthorizedPickupController _supervisorController;

    // Valid IdKeys for testing
    private readonly string _attendanceIdKey = IdKeyHelper.Encode(100);
    private readonly string _childIdKey = IdKeyHelper.Encode(50);
    private readonly int _supervisorUserId = 99;

    public AuthorizedPickupControllerTests()
    {
        _authorizedPickupServiceMock = new Mock<IAuthorizedPickupService>();
        _loggerMock = new Mock<ILogger<AuthorizedPickupController>>();

        // Setup controller with supervisor user
        _supervisorController = new AuthorizedPickupController(
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
                ChildIdKey: _childIdKey,
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
                ChildIdKey: _childIdKey,
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
        var history = okResult.Value.Should().BeAssignableTo<List<PickupLogDto>>().Subject;
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
                ChildIdKey: _childIdKey,
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
        var history = okResult.Value.Should().BeAssignableTo<List<PickupLogDto>>().Subject;
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
        var history = okResult.Value.Should().BeAssignableTo<List<PickupLogDto>>().Subject;
        history.Should().BeEmpty();
    }

    #endregion
}
