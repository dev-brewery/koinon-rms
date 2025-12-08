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

public class AnalyticsControllerTests
{
    private readonly Mock<IAttendanceAnalyticsService> _analyticsServiceMock;
    private readonly Mock<ILogger<AnalyticsController>> _loggerMock;
    private readonly AnalyticsController _controller;

    public AnalyticsControllerTests()
    {
        _analyticsServiceMock = new Mock<IAttendanceAnalyticsService>();
        _loggerMock = new Mock<ILogger<AnalyticsController>>();
        _controller = new AnalyticsController(_analyticsServiceMock.Object, _loggerMock.Object);

        // Setup HttpContext for controller
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    [Fact]
    public async Task GetAttendanceSummary_ReturnsOk_WithAnalyticsData()
    {
        // Arrange
        var startDate = new DateOnly(2024, 1, 1);
        var endDate = new DateOnly(2024, 1, 31);
        var expectedAnalytics = new AttendanceAnalyticsDto(
            TotalAttendance: 450,
            UniqueAttendees: 125,
            FirstTimeVisitors: 15,
            ReturningVisitors: 110,
            AverageAttendance: 50.5m,
            StartDate: startDate,
            EndDate: endDate
        );

        _analyticsServiceMock
            .Setup(s => s.GetSummaryAsync(It.IsAny<AttendanceQueryOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedAnalytics);

        // Act
        var result = await _controller.GetAttendanceSummary(
            startDate, endDate, null, null, null, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var analytics = okResult.Value.Should().BeOfType<AttendanceAnalyticsDto>().Subject;

        analytics.TotalAttendance.Should().Be(450);
        analytics.UniqueAttendees.Should().Be(125);
        analytics.FirstTimeVisitors.Should().Be(15);
        analytics.ReturningVisitors.Should().Be(110);
        analytics.AverageAttendance.Should().Be(50.5m);
        analytics.StartDate.Should().Be(startDate);
        analytics.EndDate.Should().Be(endDate);

        _analyticsServiceMock.Verify(
            s => s.GetSummaryAsync(It.IsAny<AttendanceQueryOptions>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetAttendanceSummary_WithFilters_PassesCorrectOptions()
    {
        // Arrange
        var startDate = new DateOnly(2024, 1, 1);
        var endDate = new DateOnly(2024, 1, 31);
        var campusIdKey = "campus123";
        var groupTypeIdKey = "grouptype456";
        var groupIdKey = "group789";

        var expectedAnalytics = new AttendanceAnalyticsDto(
            TotalAttendance: 100,
            UniqueAttendees: 50,
            FirstTimeVisitors: 5,
            ReturningVisitors: 45,
            AverageAttendance: 25m,
            StartDate: startDate,
            EndDate: endDate
        );

        AttendanceQueryOptions? capturedOptions = null;
        _analyticsServiceMock
            .Setup(s => s.GetSummaryAsync(It.IsAny<AttendanceQueryOptions>(), It.IsAny<CancellationToken>()))
            .Callback<AttendanceQueryOptions, CancellationToken>((opts, _) => capturedOptions = opts)
            .ReturnsAsync(expectedAnalytics);

        // Act
        await _controller.GetAttendanceSummary(
            startDate, endDate, campusIdKey, groupTypeIdKey, groupIdKey, CancellationToken.None);

        // Assert
        capturedOptions.Should().NotBeNull();
        capturedOptions!.StartDate.Should().Be(startDate);
        capturedOptions.EndDate.Should().Be(endDate);
        capturedOptions.CampusIdKey.Should().Be(campusIdKey);
        capturedOptions.GroupTypeIdKey.Should().Be(groupTypeIdKey);
        capturedOptions.GroupIdKey.Should().Be(groupIdKey);
    }

    [Fact]
    public async Task GetAttendanceTrends_ReturnsOk_WithTrendData()
    {
        // Arrange
        var startDate = new DateOnly(2024, 1, 1);
        var endDate = new DateOnly(2024, 1, 7);
        var expectedTrends = new List<AttendanceTrendDto>
        {
            new(new DateOnly(2024, 1, 1), 50, 5, 45),
            new(new DateOnly(2024, 1, 2), 55, 3, 52),
            new(new DateOnly(2024, 1, 3), 48, 2, 46)
        };

        _analyticsServiceMock
            .Setup(s => s.GetTrendsAsync(It.IsAny<AttendanceQueryOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedTrends);

        // Act
        var result = await _controller.GetAttendanceTrends(
            startDate, endDate, null, null, null, GroupBy.Day, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var trends = okResult.Value.Should().BeAssignableTo<IReadOnlyList<AttendanceTrendDto>>().Subject;

        trends.Should().HaveCount(3);
        trends[0].Date.Should().Be(new DateOnly(2024, 1, 1));
        trends[0].Count.Should().Be(50);
        trends[0].FirstTime.Should().Be(5);
        trends[0].Returning.Should().Be(45);

        _analyticsServiceMock.Verify(
            s => s.GetTrendsAsync(It.IsAny<AttendanceQueryOptions>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetAttendanceTrends_WithGroupByWeek_PassesCorrectOptions()
    {
        // Arrange
        var startDate = new DateOnly(2024, 1, 1);
        var endDate = new DateOnly(2024, 1, 31);

        AttendanceQueryOptions? capturedOptions = null;
        _analyticsServiceMock
            .Setup(s => s.GetTrendsAsync(It.IsAny<AttendanceQueryOptions>(), It.IsAny<CancellationToken>()))
            .Callback<AttendanceQueryOptions, CancellationToken>((opts, _) => capturedOptions = opts)
            .ReturnsAsync(new List<AttendanceTrendDto>());

        // Act
        await _controller.GetAttendanceTrends(
            startDate, endDate, null, null, null, GroupBy.Week, CancellationToken.None);

        // Assert
        capturedOptions.Should().NotBeNull();
        capturedOptions!.GroupBy.Should().Be(GroupBy.Week);
    }

    [Fact]
    public async Task GetAttendanceByGroup_ReturnsOk_WithGroupData()
    {
        // Arrange
        var startDate = new DateOnly(2024, 1, 1);
        var endDate = new DateOnly(2024, 1, 31);
        var expectedByGroup = new List<AttendanceByGroupDto>
        {
            new("group1", "Youth Group", "Small Group", 150, 35),
            new("group2", "Children's Ministry", "Check-in Area", 200, 45),
            new("group3", "Adult Bible Study", "Small Group", 100, 25)
        };

        _analyticsServiceMock
            .Setup(s => s.GetByGroupAsync(It.IsAny<AttendanceQueryOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedByGroup);

        // Act
        var result = await _controller.GetAttendanceByGroup(
            startDate, endDate, null, null, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var byGroup = okResult.Value.Should().BeAssignableTo<IReadOnlyList<AttendanceByGroupDto>>().Subject;

        byGroup.Should().HaveCount(3);
        byGroup[0].GroupName.Should().Be("Youth Group");
        byGroup[0].TotalAttendance.Should().Be(150);
        byGroup[0].UniqueAttendees.Should().Be(35);

        _analyticsServiceMock.Verify(
            s => s.GetByGroupAsync(It.IsAny<AttendanceQueryOptions>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetAttendanceByGroup_WithFilters_PassesCorrectOptions()
    {
        // Arrange
        var startDate = new DateOnly(2024, 1, 1);
        var endDate = new DateOnly(2024, 1, 31);
        var campusIdKey = "campus123";
        var groupTypeIdKey = "grouptype456";

        AttendanceQueryOptions? capturedOptions = null;
        _analyticsServiceMock
            .Setup(s => s.GetByGroupAsync(It.IsAny<AttendanceQueryOptions>(), It.IsAny<CancellationToken>()))
            .Callback<AttendanceQueryOptions, CancellationToken>((opts, _) => capturedOptions = opts)
            .ReturnsAsync(new List<AttendanceByGroupDto>());

        // Act
        await _controller.GetAttendanceByGroup(
            startDate, endDate, campusIdKey, groupTypeIdKey, CancellationToken.None);

        // Assert
        capturedOptions.Should().NotBeNull();
        capturedOptions!.StartDate.Should().Be(startDate);
        capturedOptions.EndDate.Should().Be(endDate);
        capturedOptions.CampusIdKey.Should().Be(campusIdKey);
        capturedOptions.GroupTypeIdKey.Should().Be(groupTypeIdKey);
    }

    [Fact]
    public async Task GetAttendanceSummary_WhenServiceThrows_PropagatesException()
    {
        // Arrange
        _analyticsServiceMock
            .Setup(s => s.GetSummaryAsync(It.IsAny<AttendanceQueryOptions>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database connection failed"));

        // Act & Assert
        var act = async () => await _controller.GetAttendanceSummary(
            null, null, null, null, null, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Database connection failed");
    }

    [Fact]
    public async Task GetAttendanceTrends_WhenServiceThrows_PropagatesException()
    {
        // Arrange
        _analyticsServiceMock
            .Setup(s => s.GetTrendsAsync(It.IsAny<AttendanceQueryOptions>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database connection failed"));

        // Act & Assert
        var act = async () => await _controller.GetAttendanceTrends(
            null, null, null, null, null, GroupBy.Day, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Database connection failed");
    }

    [Fact]
    public async Task GetAttendanceByGroup_WhenServiceThrows_PropagatesException()
    {
        // Arrange
        _analyticsServiceMock
            .Setup(s => s.GetByGroupAsync(It.IsAny<AttendanceQueryOptions>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database connection failed"));

        // Act & Assert
        var act = async () => await _controller.GetAttendanceByGroup(
            null, null, null, null, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Database connection failed");
    }
}
