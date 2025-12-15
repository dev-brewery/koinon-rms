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
    private readonly Mock<IFirstTimeVisitorService> _firstTimeVisitorServiceMock;
    private readonly Mock<ILogger<AnalyticsController>> _loggerMock;
    private readonly AnalyticsController _controller;

    public AnalyticsControllerTests()
    {
        _analyticsServiceMock = new Mock<IAttendanceAnalyticsService>();
        _firstTimeVisitorServiceMock = new Mock<IFirstTimeVisitorService>();
        _loggerMock = new Mock<ILogger<AnalyticsController>>();
        _controller = new AnalyticsController(
            _analyticsServiceMock.Object,
            _firstTimeVisitorServiceMock.Object,
            _loggerMock.Object);

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
        var response = okResult.Value.Should().BeAssignableTo<object>().Subject;
        var dataProperty = response.GetType().GetProperty("data");
        dataProperty.Should().NotBeNull("response should have a 'data' property");
        var analytics = dataProperty!.GetValue(response).Should().BeOfType<AttendanceAnalyticsDto>().Subject;

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
        var response = okResult.Value.Should().BeAssignableTo<object>().Subject;
        var dataProperty = response.GetType().GetProperty("data");
        dataProperty.Should().NotBeNull("response should have a 'data' property");
        var trends = dataProperty!.GetValue(response).Should().BeAssignableTo<IReadOnlyList<AttendanceTrendDto>>().Subject;

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
        var response = okResult.Value.Should().BeAssignableTo<object>().Subject;
        var dataProperty = response.GetType().GetProperty("data");
        dataProperty.Should().NotBeNull("response should have a 'data' property");
        var byGroup = dataProperty!.GetValue(response).Should().BeAssignableTo<IReadOnlyList<AttendanceByGroupDto>>().Subject;

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

    [Fact]
    public async Task GetTodaysFirstTimeVisitors_ReturnsOk_WithVisitorData()
    {
        // Arrange
        var expectedVisitors = new List<FirstTimeVisitorDto>
        {
            new()
            {
                PersonIdKey = "person1",
                PersonName = "John Doe",
                Email = "john@example.com",
                PhoneNumber = "555-1234",
                CheckInDateTime = DateTime.Today,
                GroupName = "Children's Check-In",
                GroupTypeName = "Check-in Area",
                CampusName = "Main Campus",
                HasFollowUp = false
            },
            new()
            {
                PersonIdKey = "person2",
                PersonName = "Jane Smith",
                Email = "jane@example.com",
                PhoneNumber = "555-5678",
                CheckInDateTime = DateTime.Today,
                GroupName = "Youth Group",
                GroupTypeName = "Small Group",
                CampusName = "North Campus",
                HasFollowUp = true
            }
        };

        _firstTimeVisitorServiceMock
            .Setup(s => s.GetTodaysFirstTimersAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedVisitors);

        // Act
        var result = await _controller.GetTodaysFirstTimeVisitors(null, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeAssignableTo<object>().Subject;
        var dataProperty = response.GetType().GetProperty("data");
        dataProperty.Should().NotBeNull("response should have a 'data' property");
        var visitors = dataProperty!.GetValue(response).Should().BeAssignableTo<IReadOnlyList<FirstTimeVisitorDto>>().Subject;

        visitors.Should().HaveCount(2);
        visitors[0].PersonName.Should().Be("John Doe");
        visitors[0].Email.Should().Be("john@example.com");
        visitors[1].PersonName.Should().Be("Jane Smith");
        visitors[1].HasFollowUp.Should().BeTrue();

        _firstTimeVisitorServiceMock.Verify(
            s => s.GetTodaysFirstTimersAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetTodaysFirstTimeVisitors_WithCampusFilter_PassesCampusIdKey()
    {
        // Arrange
        var campusIdKey = "campus123";
        string? capturedCampusIdKey = null;

        _firstTimeVisitorServiceMock
            .Setup(s => s.GetTodaysFirstTimersAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Callback<string?, CancellationToken>((campus, _) => capturedCampusIdKey = campus)
            .ReturnsAsync(new List<FirstTimeVisitorDto>());

        // Act
        await _controller.GetTodaysFirstTimeVisitors(campusIdKey, CancellationToken.None);

        // Assert
        capturedCampusIdKey.Should().Be(campusIdKey);
    }

    [Fact]
    public async Task GetFirstTimeVisitorsByDateRange_ReturnsOk_WithVisitorData()
    {
        // Arrange
        var startDate = new DateOnly(2024, 1, 1);
        var endDate = new DateOnly(2024, 1, 31);
        var expectedVisitors = new List<FirstTimeVisitorDto>
        {
            new()
            {
                PersonIdKey = "person1",
                PersonName = "John Doe",
                Email = "john@example.com",
                PhoneNumber = "555-1234",
                CheckInDateTime = new DateTime(2024, 1, 5),
                GroupName = "Children's Check-In",
                GroupTypeName = "Check-in Area",
                CampusName = "Main Campus",
                HasFollowUp = false
            },
            new()
            {
                PersonIdKey = "person2",
                PersonName = "Jane Smith",
                Email = "jane@example.com",
                PhoneNumber = "555-5678",
                CheckInDateTime = new DateTime(2024, 1, 15),
                GroupName = "Youth Group",
                GroupTypeName = "Small Group",
                CampusName = "North Campus",
                HasFollowUp = true
            }
        };

        _firstTimeVisitorServiceMock
            .Setup(s => s.GetFirstTimersByDateRangeAsync(
                It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedVisitors);

        // Act
        var result = await _controller.GetFirstTimeVisitorsByDateRange(
            startDate, endDate, null, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeAssignableTo<object>().Subject;
        var dataProperty = response.GetType().GetProperty("data");
        dataProperty.Should().NotBeNull("response should have a 'data' property");
        var visitors = dataProperty!.GetValue(response).Should().BeAssignableTo<IReadOnlyList<FirstTimeVisitorDto>>().Subject;

        visitors.Should().HaveCount(2);
        visitors[0].CheckInDateTime.Should().Be(new DateTime(2024, 1, 5));
        visitors[1].CheckInDateTime.Should().Be(new DateTime(2024, 1, 15));

        _firstTimeVisitorServiceMock.Verify(
            s => s.GetFirstTimersByDateRangeAsync(startDate, endDate, null, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetFirstTimeVisitorsByDateRange_WithCampusFilter_PassesAllParameters()
    {
        // Arrange
        var startDate = new DateOnly(2024, 1, 1);
        var endDate = new DateOnly(2024, 1, 31);
        var campusIdKey = "campus123";

        DateOnly? capturedStartDate = null;
        DateOnly? capturedEndDate = null;
        string? capturedCampusIdKey = null;

        _firstTimeVisitorServiceMock
            .Setup(s => s.GetFirstTimersByDateRangeAsync(
                It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Callback<DateOnly, DateOnly, string?, CancellationToken>(
                (start, end, campus, _) =>
                {
                    capturedStartDate = start;
                    capturedEndDate = end;
                    capturedCampusIdKey = campus;
                })
            .ReturnsAsync(new List<FirstTimeVisitorDto>());

        // Act
        await _controller.GetFirstTimeVisitorsByDateRange(
            startDate, endDate, campusIdKey, CancellationToken.None);

        // Assert
        capturedStartDate.Should().Be(startDate);
        capturedEndDate.Should().Be(endDate);
        capturedCampusIdKey.Should().Be(campusIdKey);
    }

    [Fact]
    public async Task GetFirstTimeVisitorsByDateRange_WithMissingStartDate_ReturnsBadRequest()
    {
        // Arrange
        var endDate = new DateOnly(2024, 1, 31);

        // Act
        var result = await _controller.GetFirstTimeVisitorsByDateRange(
            null, endDate, null, CancellationToken.None);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var problemDetails = badRequestResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Detail.Should().Be("Both startDate and endDate are required.");

        _firstTimeVisitorServiceMock.Verify(
            s => s.GetFirstTimersByDateRangeAsync(
                It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetFirstTimeVisitorsByDateRange_WithMissingEndDate_ReturnsBadRequest()
    {
        // Arrange
        var startDate = new DateOnly(2024, 1, 1);

        // Act
        var result = await _controller.GetFirstTimeVisitorsByDateRange(
            startDate, null, null, CancellationToken.None);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var problemDetails = badRequestResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Detail.Should().Be("Both startDate and endDate are required.");

        _firstTimeVisitorServiceMock.Verify(
            s => s.GetFirstTimersByDateRangeAsync(
                It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetFirstTimeVisitorsByDateRange_WithStartDateAfterEndDate_ReturnsBadRequest()
    {
        // Arrange
        var startDate = new DateOnly(2024, 1, 31);
        var endDate = new DateOnly(2024, 1, 1);

        // Act
        var result = await _controller.GetFirstTimeVisitorsByDateRange(
            startDate, endDate, null, CancellationToken.None);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var problemDetails = badRequestResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Detail.Should().Be("startDate must be less than or equal to endDate.");

        _firstTimeVisitorServiceMock.Verify(
            s => s.GetFirstTimersByDateRangeAsync(
                It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetTodaysFirstTimeVisitors_WhenServiceThrows_PropagatesException()
    {
        // Arrange
        _firstTimeVisitorServiceMock
            .Setup(s => s.GetTodaysFirstTimersAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database connection failed"));

        // Act & Assert
        var act = async () => await _controller.GetTodaysFirstTimeVisitors(null, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Database connection failed");
    }

    [Fact]
    public async Task GetFirstTimeVisitorsByDateRange_WhenServiceThrows_PropagatesException()
    {
        // Arrange
        var startDate = new DateOnly(2024, 1, 1);
        var endDate = new DateOnly(2024, 1, 31);

        _firstTimeVisitorServiceMock
            .Setup(s => s.GetFirstTimersByDateRangeAsync(
                It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database connection failed"));

        // Act & Assert
        var act = async () => await _controller.GetFirstTimeVisitorsByDateRange(
            startDate, endDate, null, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Database connection failed");
    }
}
