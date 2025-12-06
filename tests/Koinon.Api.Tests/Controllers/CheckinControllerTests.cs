using FluentAssertions;
using Koinon.Api.Controllers;
using Koinon.Application.DTOs;
using Koinon.Application.Interfaces;
using Koinon.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Koinon.Api.Tests.Controllers;

public class CheckinControllerTests
{
    private readonly Mock<ICheckinConfigurationService> _configServiceMock;
    private readonly Mock<ICheckinSearchService> _searchServiceMock;
    private readonly Mock<ICheckinAttendanceService> _attendanceServiceMock;
    private readonly Mock<ILabelGenerationService> _labelServiceMock;
    private readonly Mock<ILogger<CheckinController>> _loggerMock;
    private readonly CheckinController _controller;

    public CheckinControllerTests()
    {
        _configServiceMock = new Mock<ICheckinConfigurationService>();
        _searchServiceMock = new Mock<ICheckinSearchService>();
        _attendanceServiceMock = new Mock<ICheckinAttendanceService>();
        _labelServiceMock = new Mock<ILabelGenerationService>();
        _loggerMock = new Mock<ILogger<CheckinController>>();

        _controller = new CheckinController(
            _configServiceMock.Object,
            _searchServiceMock.Object,
            _attendanceServiceMock.Object,
            _labelServiceMock.Object,
            _loggerMock.Object);

        // Setup HttpContext for controller
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    #region GetActiveAreas Tests

    [Fact]
    public async Task GetActiveAreas_WithValidCampusId_ReturnsOkWithAreas()
    {
        // Arrange
        var campusId = "campus123";
        var expectedAreas = new List<CheckinAreaDto>
        {
            new()
            {
                IdKey = "area1",
                Guid = Guid.NewGuid(),
                Name = "Children's Ministry",
                Description = "Ages 0-12",
                GroupType = new GroupTypeDto
                {
                    IdKey = "gt1",
                    Guid = Guid.NewGuid(),
                    Name = "Check-in Area",
                    IsFamilyGroupType = false,
                    AllowMultipleLocations = true,
                    Roles = new List<GroupTypeRoleDto>()
                },
                Locations = new List<CheckinLocationDto>(),
                IsActive = true,
                CapacityStatus = CapacityStatus.Available
            }
        };

        _configServiceMock
            .Setup(s => s.GetActiveAreasAsync(campusId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedAreas);

        // Act
        var result = await _controller.GetActiveAreas(campusId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var areas = okResult.Value.Should().BeAssignableTo<IReadOnlyList<CheckinAreaDto>>().Subject;
        areas.Should().HaveCount(1);
        areas[0].Name.Should().Be("Children's Ministry");
    }

    [Fact]
    public async Task GetActiveAreas_WithEmptyCampusId_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.GetActiveAreas(string.Empty);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var problemDetails = badRequestResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Title.Should().Be("Invalid request");
        problemDetails.Detail.Should().Contain("Campus IdKey is required");
    }

    [Fact]
    public async Task GetActiveAreas_WithValidCampusId_ReturnsEmptyList()
    {
        // Arrange
        var campusId = "campus123";
        _configServiceMock
            .Setup(s => s.GetActiveAreasAsync(campusId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CheckinAreaDto>());

        // Act
        var result = await _controller.GetActiveAreas(campusId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var areas = okResult.Value.Should().BeAssignableTo<IReadOnlyList<CheckinAreaDto>>().Subject;
        areas.Should().BeEmpty();
    }

    #endregion

    #region GetConfiguration Tests

    [Fact]
    public async Task GetConfiguration_WithValidCampusId_ReturnsOkWithConfig()
    {
        // Arrange
        var campusId = "campus123";
        var expectedConfig = new CheckinConfigurationDto
        {
            Campus = new CampusSummaryDto { IdKey = campusId, Name = "Main Campus" },
            Areas = new List<CheckinAreaDto>(),
            ActiveSchedules = new List<ScheduleDto>(),
            ServerTime = DateTime.UtcNow
        };

        _configServiceMock
            .Setup(s => s.GetConfigurationByCampusAsync(campusId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedConfig);

        // Act
        var result = await _controller.GetConfiguration(campusId, null);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var config = okResult.Value.Should().BeOfType<CheckinConfigurationDto>().Subject;
        config.Campus.Name.Should().Be("Main Campus");
    }

    [Fact]
    public async Task GetConfiguration_WithValidKioskId_ReturnsOkWithConfig()
    {
        // Arrange
        var kioskId = "kiosk456";
        var expectedConfig = new CheckinConfigurationDto
        {
            Campus = new CampusSummaryDto { IdKey = "campus123", Name = "Main Campus" },
            Areas = new List<CheckinAreaDto>(),
            ActiveSchedules = new List<ScheduleDto>(),
            ServerTime = DateTime.UtcNow
        };

        _configServiceMock
            .Setup(s => s.GetConfigurationByKioskAsync(kioskId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedConfig);

        // Act
        var result = await _controller.GetConfiguration(null, kioskId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var config = okResult.Value.Should().BeOfType<CheckinConfigurationDto>().Subject;
        config.Campus.Name.Should().Be("Main Campus");
    }

    [Fact]
    public async Task GetConfiguration_WithNeitherCampusNorKiosk_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.GetConfiguration(null, null);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var problemDetails = badRequestResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Detail.Should().Contain("Either campusId or kioskId must be provided");
    }

    [Fact]
    public async Task GetConfiguration_WithNonExistentCampus_ReturnsNotFound()
    {
        // Arrange
        var campusId = "nonexistent";
        _configServiceMock
            .Setup(s => s.GetConfigurationByCampusAsync(campusId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CheckinConfigurationDto?)null);

        // Act
        var result = await _controller.GetConfiguration(campusId, null);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var problemDetails = notFoundResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Title.Should().Be("Configuration not found");
    }

    #endregion

    #region SearchFamilies Tests

    [Fact]
    public async Task SearchFamilies_WithValidQuery_ReturnsOkWithFamilies()
    {
        // Arrange
        var query = "5551234";
        var expectedFamilies = new List<CheckinFamilySearchResultDto>
        {
            new()
            {
                FamilyIdKey = "family1",
                FamilyName = "Doe Family",
                Members = new List<CheckinFamilyMemberDto>
                {
                    new()
                    {
                        PersonIdKey = "person1",
                        FullName = "John Doe",
                        FirstName = "John",
                        LastName = "Doe",
                        Gender = "Male",
                        RoleName = "Adult",
                        IsChild = false,
                        HasRecentCheckIn = true
                    }
                },
                RecentCheckInCount = 5
            }
        };

        _searchServiceMock
            .Setup(s => s.SearchAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedFamilies);

        // Act
        var result = await _controller.SearchFamilies(query);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var families = okResult.Value.Should().BeAssignableTo<List<CheckinFamilySearchResultDto>>().Subject;
        families.Should().HaveCount(1);
        families[0].FamilyName.Should().Be("Doe Family");
    }

    [Fact]
    public async Task SearchFamilies_WithEmptyQuery_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.SearchFamilies(string.Empty);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var problemDetails = badRequestResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Detail.Should().Contain("Search query is required");
    }

    [Fact]
    public async Task SearchFamilies_WithNoResults_ReturnsOkWithEmptyList()
    {
        // Arrange
        var query = "nonexistent";
        _searchServiceMock
            .Setup(s => s.SearchAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CheckinFamilySearchResultDto>());

        // Act
        var result = await _controller.SearchFamilies(query);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var families = okResult.Value.Should().BeAssignableTo<List<CheckinFamilySearchResultDto>>().Subject;
        families.Should().BeEmpty();
    }

    #endregion

    #region RecordAttendance Tests

    [Fact]
    public async Task RecordAttendance_WithValidRequest_ReturnsCreated()
    {
        // Arrange
        var request = new BatchCheckinRequestDto(
            new List<CheckinRequestDto>
            {
                new()
                {
                    PersonIdKey = "person1",
                    LocationIdKey = "location1",
                    ScheduleIdKey = "schedule1",
                    GenerateSecurityCode = true
                }
            });

        var expectedResult = new BatchCheckinResultDto(
            new List<CheckinResultDto>
            {
                new(
                    Success: true,
                    AttendanceIdKey: "attendance1",
                    SecurityCode: "ABC123",
                    CheckInTime: DateTime.UtcNow,
                    Person: new CheckinPersonSummaryDto("person1", "John Doe", "John", "Doe"),
                    Location: new CheckinLocationSummaryDto("location1", "Room 101", "Building A > Room 101"))
            },
            SuccessCount: 1,
            FailureCount: 0,
            AllSucceeded: true);

        _attendanceServiceMock
            .Setup(s => s.BatchCheckInAsync(It.IsAny<BatchCheckinRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.RecordAttendance(request);

        // Assert
        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        var batchResult = createdResult.Value.Should().BeOfType<BatchCheckinResultDto>().Subject;
        batchResult.SuccessCount.Should().Be(1);
        batchResult.AllSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task RecordAttendance_WithEmptyRequest_ReturnsBadRequest()
    {
        // Arrange
        var request = new BatchCheckinRequestDto(new List<CheckinRequestDto>());

        // Act
        var result = await _controller.RecordAttendance(request);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var problemDetails = badRequestResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Detail.Should().Contain("At least one check-in is required");
    }

    [Fact]
    public async Task RecordAttendance_WithAllFailures_ReturnsUnprocessableEntity()
    {
        // Arrange
        var request = new BatchCheckinRequestDto(
            new List<CheckinRequestDto>
            {
                new()
                {
                    PersonIdKey = "person1",
                    LocationIdKey = "location1",
                    GenerateSecurityCode = false
                }
            });

        var expectedResult = new BatchCheckinResultDto(
            new List<CheckinResultDto>
            {
                new(Success: false, ErrorMessage: "Location at capacity")
            },
            SuccessCount: 0,
            FailureCount: 1,
            AllSucceeded: false);

        _attendanceServiceMock
            .Setup(s => s.BatchCheckInAsync(It.IsAny<BatchCheckinRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.RecordAttendance(request);

        // Assert
        var unprocessableResult = result.Should().BeOfType<UnprocessableEntityObjectResult>().Subject;
        var problemDetails = unprocessableResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Title.Should().Be("Check-in failed");
    }

    [Fact]
    public async Task RecordAttendance_WithPartialSuccess_ReturnsCreated()
    {
        // Arrange
        var request = new BatchCheckinRequestDto(
            new List<CheckinRequestDto>
            {
                new()
                {
                    PersonIdKey = "person1",
                    LocationIdKey = "location1",
                    GenerateSecurityCode = true
                },
                new()
                {
                    PersonIdKey = "person2",
                    LocationIdKey = "location1",
                    GenerateSecurityCode = true
                }
            });

        var expectedResult = new BatchCheckinResultDto(
            new List<CheckinResultDto>
            {
                new(
                    Success: true,
                    AttendanceIdKey: "attendance1",
                    SecurityCode: "ABC123",
                    CheckInTime: DateTime.UtcNow,
                    Person: new CheckinPersonSummaryDto("person1", "John Doe", "John", "Doe"),
                    Location: new CheckinLocationSummaryDto("location1", "Room 101", "Building A > Room 101")),
                new(Success: false, ErrorMessage: "Already checked in")
            },
            SuccessCount: 1,
            FailureCount: 1,
            AllSucceeded: false);

        _attendanceServiceMock
            .Setup(s => s.BatchCheckInAsync(It.IsAny<BatchCheckinRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.RecordAttendance(request);

        // Assert
        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        var batchResult = createdResult.Value.Should().BeOfType<BatchCheckinResultDto>().Subject;
        batchResult.SuccessCount.Should().Be(1);
        batchResult.FailureCount.Should().Be(1);
        batchResult.AllSucceeded.Should().BeFalse();
    }

    #endregion

    #region CheckOut Tests

    [Fact]
    public async Task CheckOut_WithValidAttendanceId_ReturnsNoContent()
    {
        // Arrange
        var attendanceIdKey = "attendance123";
        _attendanceServiceMock
            .Setup(s => s.CheckOutAsync(attendanceIdKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.CheckOut(attendanceIdKey);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task CheckOut_WithNonExistentAttendance_ReturnsNotFound()
    {
        // Arrange
        var attendanceIdKey = "nonexistent";
        _attendanceServiceMock
            .Setup(s => s.CheckOutAsync(attendanceIdKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.CheckOut(attendanceIdKey);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var problemDetails = notFoundResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Title.Should().Be("Attendance not found");
    }

    #endregion

    #region GetAttendanceLabels Tests

    [Fact]
    public async Task GetAttendanceLabels_WithValidAttendanceId_ReturnsOkWithLabels()
    {
        // Arrange
        var attendanceIdKey = "attendance123";
        var expectedLabels = new LabelSetDto(
            attendanceIdKey,
            "person1",
            new List<LabelDto>
            {
                new(LabelType.ChildName, "ZPL content here", "ZPL", new Dictionary<string, string>
                {
                    { "name", "John Doe" },
                    { "securityCode", "ABC123" }
                })
            });

        _labelServiceMock
            .Setup(s => s.GenerateLabelsAsync(It.IsAny<LabelRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedLabels);

        // Act
        var result = await _controller.GetAttendanceLabels(attendanceIdKey);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var labels = okResult.Value.Should().BeOfType<LabelSetDto>().Subject;
        labels.Labels.Should().HaveCount(1);
        labels.AttendanceIdKey.Should().Be(attendanceIdKey);
    }

    [Fact]
    public async Task GetAttendanceLabels_WithNonExistentAttendance_ReturnsNotFound()
    {
        // Arrange
        var attendanceIdKey = "nonexistent";
        _labelServiceMock
            .Setup(s => s.GenerateLabelsAsync(It.IsAny<LabelRequestDto>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Attendance not found"));

        // Act
        var result = await _controller.GetAttendanceLabels(attendanceIdKey);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var problemDetails = notFoundResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Title.Should().Be("Attendance not found");
    }

    #endregion

    #region GetLocationAttendance Tests

    [Fact]
    public async Task GetLocationAttendance_WithValidLocationId_ReturnsOkWithAttendance()
    {
        // Arrange
        var locationIdKey = "location123";
        var expectedAttendance = new List<AttendanceSummaryDto>
        {
            new(
                "attendance1",
                new CheckinPersonSummaryDto("person1", "John Doe", "John", "Doe"),
                new CheckinLocationSummaryDto(locationIdKey, "Room 101", "Building A > Room 101"),
                DateTime.UtcNow.AddHours(-1),
                SecurityCode: "ABC123")
        };

        _attendanceServiceMock
            .Setup(s => s.GetCurrentAttendanceAsync(locationIdKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedAttendance);

        // Act
        var result = await _controller.GetLocationAttendance(locationIdKey);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var attendance = okResult.Value.Should().BeAssignableTo<IReadOnlyList<AttendanceSummaryDto>>().Subject;
        attendance.Should().HaveCount(1);
        attendance[0].Location.IdKey.Should().Be(locationIdKey);
    }

    [Fact]
    public async Task GetLocationAttendance_WithNoAttendance_ReturnsOkWithEmptyList()
    {
        // Arrange
        var locationIdKey = "location123";
        _attendanceServiceMock
            .Setup(s => s.GetCurrentAttendanceAsync(locationIdKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AttendanceSummaryDto>());

        // Act
        var result = await _controller.GetLocationAttendance(locationIdKey);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var attendance = okResult.Value.Should().BeAssignableTo<IReadOnlyList<AttendanceSummaryDto>>().Subject;
        attendance.Should().BeEmpty();
    }

    #endregion
}
