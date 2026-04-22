using FluentAssertions;
using Koinon.Api.Controllers;
using Koinon.Application.Common;
using Koinon.Application.DTOs.CheckinOperations;
using Koinon.Application.Interfaces;
using Koinon.Domain.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Koinon.Api.Tests.Controllers;

public class CheckinOperationsControllerTests
{
    private readonly Mock<ICheckinOperationsService> _serviceMock;
    private readonly Mock<ILogger<CheckinOperationsController>> _loggerMock;
    private readonly CheckinOperationsController _controller;

    private readonly string _locationIdKey = IdKeyHelper.Encode(42);

    public CheckinOperationsControllerTests()
    {
        _serviceMock = new Mock<ICheckinOperationsService>();
        _loggerMock = new Mock<ILogger<CheckinOperationsController>>();
        _controller = new CheckinOperationsController(_serviceMock.Object, _loggerMock.Object);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    [Fact]
    public async Task GetDashboardAsync_ReturnsOkWithPayload()
    {
        // Arrange
        var payload = new CheckinOperationsDashboardDto(
            Rooms: new[]
            {
                new CheckinOperationsRoomDto(
                    LocationIdKey: _locationIdKey,
                    LocationName: "Nursery",
                    CheckedInCount: 3,
                    Capacity: 10,
                    PercentFull: 30,
                    CapacityPillColor: "green",
                    IsOpen: true)
            },
            Attendees: new[]
            {
                new CheckinOperationsAttendeeDto(
                    AttendanceIdKey: IdKeyHelper.Encode(1),
                    PersonIdKey: IdKeyHelper.Encode(11),
                    FullName: "Ella Smith",
                    LocationIdKey: _locationIdKey,
                    LocationName: "Nursery",
                    CheckInTime: DateTime.UtcNow,
                    CheckOutTime: null,
                    IsPresent: true)
            },
            Summary: new CheckinOperationsSummaryDto(
                TotalCheckedIn: 3,
                CurrentlyPresent: 3,
                CheckedOut: 0),
            GeneratedAt: DateTime.UtcNow);

        _serviceMock
            .Setup(s => s.GetDashboardAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<CheckinOperationsDashboardDto>.Success(payload));

        // Act
        var result = await _controller.GetDashboardAsync();

        // Assert
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var dataProperty = ok.Value!.GetType().GetProperty("data");
        var dto = dataProperty!.GetValue(ok.Value).Should().BeOfType<CheckinOperationsDashboardDto>().Subject;
        dto.Rooms.Should().HaveCount(1);
        dto.Summary.CurrentlyPresent.Should().Be(3);
    }

    [Fact]
    public async Task GetDashboardAsync_FailureReturnsUnprocessable()
    {
        // Arrange
        _serviceMock
            .Setup(s => s.GetDashboardAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<CheckinOperationsDashboardDto>.Failure(Error.Internal("boom")));

        // Act
        var result = await _controller.GetDashboardAsync();

        // Assert
        result.Should().BeOfType<UnprocessableEntityObjectResult>();
    }

    [Fact]
    public async Task ToggleRoomAsync_ValidIdKey_ReturnsOkWithToggledState()
    {
        // Arrange
        var response = new ToggleRoomResponseDto(_locationIdKey, IsOpen: false);
        _serviceMock
            .Setup(s => s.ToggleRoomAsync(_locationIdKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ToggleRoomResponseDto>.Success(response));

        // Act
        var result = await _controller.ToggleRoomAsync(_locationIdKey);

        // Assert
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var dataProperty = ok.Value!.GetType().GetProperty("data");
        var dto = dataProperty!.GetValue(ok.Value).Should().BeOfType<ToggleRoomResponseDto>().Subject;
        dto.IsOpen.Should().BeFalse();
        dto.LocationIdKey.Should().Be(_locationIdKey);
    }

    [Fact]
    public async Task ToggleRoomAsync_NotFound_ReturnsNotFoundProblemDetails()
    {
        // Arrange
        _serviceMock
            .Setup(s => s.ToggleRoomAsync(_locationIdKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ToggleRoomResponseDto>.Failure(Error.NotFound("Location", _locationIdKey)));

        // Act
        var result = await _controller.ToggleRoomAsync(_locationIdKey);

        // Assert
        var notFound = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var problem = notFound.Value.Should().BeOfType<ProblemDetails>().Subject;
        problem.Status.Should().Be(StatusCodes.Status404NotFound);
    }

    [Fact]
    public async Task ToggleRoomAsync_InvalidIdKey_ReturnsBadRequest()
    {
        // Arrange
        _serviceMock
            .Setup(s => s.ToggleRoomAsync("garbage", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ToggleRoomResponseDto>.Failure(Error.Validation("Invalid location IdKey 'garbage'")));

        // Act
        var result = await _controller.ToggleRoomAsync("garbage");

        // Assert
        var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var problem = badRequest.Value.Should().BeOfType<ProblemDetails>().Subject;
        problem.Status.Should().Be(StatusCodes.Status400BadRequest);
    }
}
