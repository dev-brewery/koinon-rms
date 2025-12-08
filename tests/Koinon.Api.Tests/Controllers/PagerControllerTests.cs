using System.Security.Claims;
using FluentAssertions;
using Koinon.Api.Controllers;
using Koinon.Application.Common;
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

public class PagerControllerTests
{
    private readonly Mock<IParentPagingService> _parentPagingServiceMock;
    private readonly Mock<ILogger<PagerController>> _loggerMock;
    private readonly PagerController _controller;

    // Valid IdKeys and IDs for testing
    private readonly string _attendanceIdKey = IdKeyHelper.Encode(100);
    private readonly int _sentByPersonId = 42;
    private readonly int _pagerNumber = 127;

    public PagerControllerTests()
    {
        _parentPagingServiceMock = new Mock<IParentPagingService>();
        _loggerMock = new Mock<ILogger<PagerController>>();

        _controller = new PagerController(
            _parentPagingServiceMock.Object,
            _loggerMock.Object);

        // Setup HttpContext with claims for authenticated user
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, _sentByPersonId.ToString()),
            new Claim(ClaimTypes.Email, "supervisor@example.com"),
            new Claim(ClaimTypes.Name, "John Supervisor"),
            new Claim(ClaimTypes.Role, "Supervisor")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = claimsPrincipal
            }
        };
    }

    #region SendPage Tests

    [Fact]
    public async Task SendPage_WithValidRequest_ReturnsOkWithPagerMessage()
    {
        // Arrange
        var request = new SendPageRequest(
            PagerNumber: "127",
            MessageType: PagerMessageType.PickupNeeded,
            CustomMessage: null);

        var expectedMessage = new PagerMessageDto(
            IdKey: IdKeyHelper.Encode(1),
            MessageType: PagerMessageType.PickupNeeded,
            MessageText: "Please come to Room 101 to pick up Johnny.",
            Status: PagerMessageStatus.Sent,
            SentDateTime: DateTime.UtcNow,
            DeliveredDateTime: null,
            SentByPersonName: "John Supervisor");

        _parentPagingServiceMock
            .Setup(s => s.SendPageAsync(request, _sentByPersonId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PagerMessageDto>.Success(expectedMessage));

        // Act
        var result = await _controller.SendPage(request);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var message = okResult.Value.Should().BeAssignableTo<PagerMessageDto>().Subject;
        message.MessageType.Should().Be(PagerMessageType.PickupNeeded);
        message.Status.Should().Be(PagerMessageStatus.Sent);
        message.SentByPersonName.Should().Be("John Supervisor");
    }

    [Fact]
    public async Task SendPage_WithCustomMessage_ReturnsOkWithCustomMessage()
    {
        // Arrange
        var request = new SendPageRequest(
            PagerNumber: "127",
            MessageType: PagerMessageType.Custom,
            CustomMessage: "Your child needs a diaper change.");

        var expectedMessage = new PagerMessageDto(
            IdKey: IdKeyHelper.Encode(1),
            MessageType: PagerMessageType.Custom,
            MessageText: "Your child needs a diaper change.",
            Status: PagerMessageStatus.Sent,
            SentDateTime: DateTime.UtcNow,
            DeliveredDateTime: null,
            SentByPersonName: "John Supervisor");

        _parentPagingServiceMock
            .Setup(s => s.SendPageAsync(request, _sentByPersonId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PagerMessageDto>.Success(expectedMessage));

        // Act
        var result = await _controller.SendPage(request);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var message = okResult.Value.Should().BeAssignableTo<PagerMessageDto>().Subject;
        message.MessageType.Should().Be(PagerMessageType.Custom);
        message.MessageText.Should().Be("Your child needs a diaper change.");
    }

    [Fact]
    public async Task SendPage_WhenPagerNotFound_ReturnsNotFound()
    {
        // Arrange
        var request = new SendPageRequest(
            PagerNumber: "999",
            MessageType: PagerMessageType.PickupNeeded,
            CustomMessage: null);

        _parentPagingServiceMock
            .Setup(s => s.SendPageAsync(request, _sentByPersonId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PagerMessageDto>.Failure(
                new Error("NOT_FOUND", "Pager number 999 not found for today")));

        // Act
        var result = await _controller.SendPage(request);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var problemDetails = notFoundResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Title.Should().Be("Pager not found");
        problemDetails.Detail.Should().Contain("Pager number 999 not found");
    }

    [Fact]
    public async Task SendPage_WhenRateLimitExceeded_ReturnsBadRequest()
    {
        // Arrange
        var request = new SendPageRequest(
            PagerNumber: "127",
            MessageType: PagerMessageType.PickupNeeded,
            CustomMessage: null);

        _parentPagingServiceMock
            .Setup(s => s.SendPageAsync(request, _sentByPersonId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PagerMessageDto>.Failure(
                new Error("RATE_LIMIT_EXCEEDED", "Maximum 3 pages per hour exceeded for this pager")));

        // Act
        var result = await _controller.SendPage(request);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var problemDetails = badRequestResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Title.Should().Be("Rate limit exceeded");
        problemDetails.Detail.Should().Contain("Maximum 3 pages per hour");
    }

    [Fact]
    public async Task SendPage_WithValidationError_ReturnsBadRequest()
    {
        // Arrange
        var request = new SendPageRequest(
            PagerNumber: "127",
            MessageType: PagerMessageType.Custom,
            CustomMessage: null); // Missing required custom message

        var validationErrors = new Dictionary<string, string[]>
        {
            ["CustomMessage"] = new[] { "Custom message is required when MessageType is Custom" }
        };

        _parentPagingServiceMock
            .Setup(s => s.SendPageAsync(request, _sentByPersonId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PagerMessageDto>.Failure(
                new Error("VALIDATION_ERROR", "Validation failed", validationErrors)));

        // Act
        var result = await _controller.SendPage(request);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var problemDetails = badRequestResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Title.Should().Be("Validation failed");
        problemDetails.Extensions.Should().ContainKey("errors");
    }

    #endregion

    #region SearchPagers Tests

    [Fact]
    public async Task SearchPagers_WithSearchTerm_ReturnsOkWithMatchingPagers()
    {
        // Arrange
        var searchTerm = "127";
        var expectedAssignments = new List<PagerAssignmentDto>
        {
            new(
                IdKey: IdKeyHelper.Encode(1),
                PagerNumber: 127,
                AttendanceIdKey: _attendanceIdKey,
                ChildName: "Johnny Smith",
                GroupName: "Nursery",
                LocationName: "Room 101",
                ParentPhoneNumber: "+15555551234",
                CheckedInAt: DateTime.UtcNow.AddHours(-1),
                MessagesSentCount: 1)
        };

        _parentPagingServiceMock
            .Setup(s => s.SearchPagerAsync(
                It.Is<PageSearchRequest>(r => r.SearchTerm == searchTerm),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedAssignments);

        // Act
        var result = await _controller.SearchPagers(searchTerm, null);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var assignments = okResult.Value.Should().BeAssignableTo<List<PagerAssignmentDto>>().Subject;
        assignments.Should().HaveCount(1);
        assignments[0].PagerNumber.Should().Be(127);
        assignments[0].ChildName.Should().Be("Johnny Smith");
    }

    [Fact]
    public async Task SearchPagers_WithoutSearchTerm_ReturnsAllPagers()
    {
        // Arrange
        var expectedAssignments = new List<PagerAssignmentDto>
        {
            new(
                IdKey: IdKeyHelper.Encode(1),
                PagerNumber: 127,
                AttendanceIdKey: _attendanceIdKey,
                ChildName: "Johnny Smith",
                GroupName: "Nursery",
                LocationName: "Room 101",
                ParentPhoneNumber: "+15555551234",
                CheckedInAt: DateTime.UtcNow.AddHours(-1),
                MessagesSentCount: 1),
            new(
                IdKey: IdKeyHelper.Encode(2),
                PagerNumber: 128,
                AttendanceIdKey: IdKeyHelper.Encode(101),
                ChildName: "Sally Jones",
                GroupName: "Toddlers",
                LocationName: "Room 102",
                ParentPhoneNumber: "+15555555678",
                CheckedInAt: DateTime.UtcNow.AddHours(-1),
                MessagesSentCount: 0)
        };

        _parentPagingServiceMock
            .Setup(s => s.SearchPagerAsync(
                It.IsAny<PageSearchRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedAssignments);

        // Act
        var result = await _controller.SearchPagers(null, null);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var assignments = okResult.Value.Should().BeAssignableTo<List<PagerAssignmentDto>>().Subject;
        assignments.Should().HaveCount(2);
    }

    [Fact]
    public async Task SearchPagers_WithSpecificDate_ReturnsPagersForThatDate()
    {
        // Arrange
        var searchDate = new DateTime(2024, 1, 15);
        var expectedAssignments = new List<PagerAssignmentDto>
        {
            new(
                IdKey: IdKeyHelper.Encode(1),
                PagerNumber: 127,
                AttendanceIdKey: _attendanceIdKey,
                ChildName: "Johnny Smith",
                GroupName: "Nursery",
                LocationName: "Room 101",
                ParentPhoneNumber: "+15555551234",
                CheckedInAt: searchDate.AddHours(10),
                MessagesSentCount: 1)
        };

        _parentPagingServiceMock
            .Setup(s => s.SearchPagerAsync(
                It.Is<PageSearchRequest>(r => r.Date == searchDate),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedAssignments);

        // Act
        var result = await _controller.SearchPagers(null, searchDate);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var assignments = okResult.Value.Should().BeAssignableTo<List<PagerAssignmentDto>>().Subject;
        assignments.Should().HaveCount(1);
    }

    #endregion

    #region GetPageHistory Tests

    [Fact]
    public async Task GetPageHistory_WithValidPagerNumber_ReturnsOkWithHistory()
    {
        // Arrange
        var expectedHistory = new PageHistoryDto(
            IdKey: IdKeyHelper.Encode(1),
            PagerNumber: _pagerNumber,
            ChildName: "Johnny Smith",
            ParentPhoneNumber: "+15555551234",
            Messages: new List<PagerMessageDto>
            {
                new(
                    IdKey: IdKeyHelper.Encode(10),
                    MessageType: PagerMessageType.PickupNeeded,
                    MessageText: "Please come to Room 101 to pick up Johnny.",
                    Status: PagerMessageStatus.Delivered,
                    SentDateTime: DateTime.UtcNow.AddMinutes(-30),
                    DeliveredDateTime: DateTime.UtcNow.AddMinutes(-29),
                    SentByPersonName: "John Supervisor")
            });

        _parentPagingServiceMock
            .Setup(s => s.GetPageHistoryAsync(_pagerNumber, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedHistory);

        // Act
        var result = await _controller.GetPageHistory(_pagerNumber, null);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var history = okResult.Value.Should().BeAssignableTo<PageHistoryDto>().Subject;
        history.PagerNumber.Should().Be(_pagerNumber);
        history.ChildName.Should().Be("Johnny Smith");
        history.Messages.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetPageHistory_WhenPagerNotFound_ReturnsNotFound()
    {
        // Arrange
        var nonExistentPagerNumber = 999;

        _parentPagingServiceMock
            .Setup(s => s.GetPageHistoryAsync(nonExistentPagerNumber, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PageHistoryDto?)null);

        // Act
        var result = await _controller.GetPageHistory(nonExistentPagerNumber, null);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var problemDetails = notFoundResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Title.Should().Be("Page history not found");
        problemDetails.Detail.Should().Contain("999");
    }

    [Fact]
    public async Task GetPageHistory_WithSpecificDate_ReturnsHistoryForThatDate()
    {
        // Arrange
        var searchDate = new DateTime(2024, 1, 15);
        var expectedHistory = new PageHistoryDto(
            IdKey: IdKeyHelper.Encode(1),
            PagerNumber: _pagerNumber,
            ChildName: "Johnny Smith",
            ParentPhoneNumber: "+15555551234",
            Messages: new List<PagerMessageDto>());

        _parentPagingServiceMock
            .Setup(s => s.GetPageHistoryAsync(_pagerNumber, searchDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedHistory);

        // Act
        var result = await _controller.GetPageHistory(_pagerNumber, searchDate);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var history = okResult.Value.Should().BeAssignableTo<PageHistoryDto>().Subject;
        history.PagerNumber.Should().Be(_pagerNumber);
    }

    #endregion

    #region GetNextPagerNumber Tests

    [Fact]
    public async Task GetNextPagerNumber_WithoutCampusId_ReturnsNextNumber()
    {
        // Arrange
        var expectedNextNumber = 130;

        _parentPagingServiceMock
            .Setup(s => s.GetNextPagerNumberAsync(null, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedNextNumber);

        // Act
        var result = await _controller.GetNextPagerNumber(null);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var nextNumber = okResult.Value.Should().BeAssignableTo<int>().Subject;
        nextNumber.Should().Be(expectedNextNumber);
    }

    [Fact]
    public async Task GetNextPagerNumber_WithCampusId_ReturnsNextNumberForCampus()
    {
        // Arrange
        var campusIdKey = IdKeyHelper.Encode(123);
        var expectedNextNumber = 105;

        _parentPagingServiceMock
            .Setup(s => s.GetNextPagerNumberAsync(123, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedNextNumber);

        // Act
        var result = await _controller.GetNextPagerNumber(campusIdKey);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var nextNumber = okResult.Value.Should().BeAssignableTo<int>().Subject;
        nextNumber.Should().Be(expectedNextNumber);
    }

    [Fact]
    public async Task GetNextPagerNumber_WhenNoPagersAssigned_ReturnsStartingNumber()
    {
        // Arrange
        var expectedNextNumber = 100; // Starting number

        _parentPagingServiceMock
            .Setup(s => s.GetNextPagerNumberAsync(null, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedNextNumber);

        // Act
        var result = await _controller.GetNextPagerNumber(null);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var nextNumber = okResult.Value.Should().BeAssignableTo<int>().Subject;
        nextNumber.Should().Be(100);
    }

    #endregion
}
