using FluentAssertions;
using Koinon.Api.Controllers;
using Koinon.Application.DTOs;
using Koinon.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Koinon.Api.Tests.Controllers;

public class DashboardControllerTests
{
    private readonly Mock<IDashboardService> _dashboardServiceMock;
    private readonly Mock<ILogger<DashboardController>> _loggerMock;
    private readonly DashboardController _controller;

    public DashboardControllerTests()
    {
        _dashboardServiceMock = new Mock<IDashboardService>();
        _loggerMock = new Mock<ILogger<DashboardController>>();
        _controller = new DashboardController(_dashboardServiceMock.Object, _loggerMock.Object);

        // Setup HttpContext for controller
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    [Fact]
    public async Task GetStats_ReturnsOk_WithDashboardStats()
    {
        // Arrange
        var expectedStats = new DashboardStatsDto
        {
            TotalPeople = 150,
            TotalFamilies = 45,
            ActiveGroups = 12,
            TodayCheckIns = 23,
            LastWeekCheckIns = 18,
            ActiveSchedules = 5,
            UpcomingSchedules = new List<UpcomingScheduleDto>
            {
                new()
                {
                    IdKey = "abc123",
                    Name = "Sunday Morning Service",
                    NextOccurrence = DateTime.UtcNow.AddDays(1),
                    MinutesUntilCheckIn = 60
                }
            }
        };

        _dashboardServiceMock
            .Setup(s => s.GetStatsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedStats);

        // Act
        var result = await _controller.GetStats(CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeAssignableTo<object>().Subject;

        // Extract data property using reflection
        var dataProperty = response.GetType().GetProperty("data");
        dataProperty.Should().NotBeNull("response should have a 'data' property");
        var stats = dataProperty!.GetValue(response).Should().BeOfType<DashboardStatsDto>().Subject;

        stats.TotalPeople.Should().Be(150);
        stats.TotalFamilies.Should().Be(45);
        stats.ActiveGroups.Should().Be(12);
        stats.TodayCheckIns.Should().Be(23);
        stats.LastWeekCheckIns.Should().Be(18);
        stats.ActiveSchedules.Should().Be(5);
        stats.UpcomingSchedules.Should().HaveCount(1);
        stats.UpcomingSchedules[0].Name.Should().Be("Sunday Morning Service");

        _dashboardServiceMock.Verify(s => s.GetStatsAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetStats_WhenServiceThrows_PropagatesException()
    {
        // Arrange
        _dashboardServiceMock
            .Setup(s => s.GetStatsAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database connection failed"));

        // Act & Assert
        var act = async () => await _controller.GetStats(CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Database connection failed");
    }

    [Fact]
    public async Task GetStats_WithCancellationToken_PassesToService()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        var expectedStats = new DashboardStatsDto
        {
            TotalPeople = 0,
            TotalFamilies = 0,
            ActiveGroups = 0,
            TodayCheckIns = 0,
            LastWeekCheckIns = 0,
            ActiveSchedules = 0,
            UpcomingSchedules = new List<UpcomingScheduleDto>()
        };

        _dashboardServiceMock
            .Setup(s => s.GetStatsAsync(cts.Token))
            .ReturnsAsync(expectedStats);

        // Act
        var result = await _controller.GetStats(cts.Token);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        _dashboardServiceMock.Verify(s => s.GetStatsAsync(cts.Token), Times.Once);
    }
}
