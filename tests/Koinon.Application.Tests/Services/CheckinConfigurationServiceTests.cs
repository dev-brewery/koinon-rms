using FluentAssertions;
using Koinon.Application.DTOs;
using Koinon.Application.Interfaces;
using Koinon.Application.Services;
using Koinon.Application.Tests.Fakes;
using Koinon.Domain.Entities;
using Koinon.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Koinon.Application.Tests.Services;

/// <summary>
/// Tests for CheckinConfigurationService.
/// </summary>
public class CheckinConfigurationServiceTests : IDisposable
{
    private readonly KoinonDbContext _context;
    private readonly FakeUserContext _userContext;
    private readonly Mock<IGradeCalculationService> _mockGradeService;
    private readonly Mock<ILogger<CheckinConfigurationService>> _mockLogger;
    private readonly CheckinConfigurationService _service;
    private readonly Campus _testCampus;
    private readonly GroupType _checkinGroupType;
    private readonly Schedule _sundayMorningSchedule;

    public CheckinConfigurationServiceTests()
    {
        // Setup in-memory database
        var options = new DbContextOptionsBuilder<KoinonDbContext>()
            .UseInMemoryDatabase(databaseName: $"KoinonTestDb_{Guid.NewGuid()}")
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new KoinonDbContext(options);
        _context.Database.EnsureCreated();

        // Setup user context (authenticated by default)
        _userContext = new FakeUserContext();

        // Setup grade calculation service mock
        _mockGradeService = new Mock<IGradeCalculationService>();

        // Setup default mock behaviors to avoid null returns
        _mockGradeService.Setup(s => s.CalculateAgeInMonths(It.IsAny<DateOnly?>(), It.IsAny<DateOnly?>()))
            .Returns((DateOnly? birthDate, DateOnly? currentDate) => birthDate.HasValue ? 60 : null);
        _mockGradeService.Setup(s => s.CalculateGrade(It.IsAny<int?>(), It.IsAny<DateOnly?>()))
            .Returns((int? gradYear, DateOnly? currentDate) => gradYear.HasValue ? 5 : null);

        // Setup logger mock
        _mockLogger = new Mock<ILogger<CheckinConfigurationService>>();

        // Create service
        _service = new CheckinConfigurationService(_context, _userContext, _mockGradeService.Object, _mockLogger.Object);

        // Seed test data
        _testCampus = SeedCampus();
        _checkinGroupType = SeedGroupType();
        _sundayMorningSchedule = SeedSchedule();
        SeedCheckinAreas();
    }

    private Campus SeedCampus()
    {
        var campus = new Campus
        {
            Id = 1,
            Name = "Main Campus",
            ShortCode = "MAIN",
            IsActive = true,
            CreatedDateTime = DateTime.UtcNow
        };
        _context.Campuses.Add(campus);
        _context.SaveChanges();
        return campus;
    }

    private GroupType SeedGroupType()
    {
        var groupType = new GroupType
        {
            Id = 1,
            Name = "Check-in Area",
            IsFamilyGroupType = false,
            AllowMultipleLocations = true,
            TakesAttendance = true,
            IsSystem = false,
            CreatedDateTime = DateTime.UtcNow
        };

        _context.GroupTypes.Add(groupType);
        _context.SaveChanges();

        var role = new GroupTypeRole
        {
            Id = 1,
            GroupTypeId = groupType.Id,
            Name = "Child",
            Order = 0,
            IsLeader = false,
            CreatedDateTime = DateTime.UtcNow
        };

        _context.GroupTypeRoles.Add(role);
        _context.SaveChanges();

        return groupType;
    }

    private Schedule SeedSchedule()
    {
        var schedule = new Schedule
        {
            Id = 1,
            Name = "Sunday Morning 9:00 AM",
            Description = "Sunday morning worship service",
            WeeklyDayOfWeek = DayOfWeek.Sunday,
            WeeklyTimeOfDay = new TimeSpan(9, 0, 0),
            CheckInStartOffsetMinutes = 60, // Opens 1 hour before
            CheckInEndOffsetMinutes = 30,   // Closes 30 min after
            IsActive = true,
            Order = 0,
            CreatedDateTime = DateTime.UtcNow
        };
        _context.Schedules.Add(schedule);
        _context.SaveChanges();
        return schedule;
    }

    private void SeedCheckinAreas()
    {
        // Children's Ministry area
        var childrenArea = new Group
        {
            Id = 1,
            Name = "Children's Ministry",
            Description = "Check-in for children ages 0-12",
            GroupTypeId = _checkinGroupType.Id,
            CampusId = _testCampus.Id,
            ScheduleId = _sundayMorningSchedule.Id,
            IsActive = true,
            IsArchived = false,
            CreatedDateTime = DateTime.UtcNow
        };

        // Infant Room (location within children's area)
        var infantRoom = new Group
        {
            Id = 2,
            Name = "Infant Room",
            Description = "Ages 0-1",
            GroupTypeId = _checkinGroupType.Id,
            CampusId = _testCampus.Id,
            ParentGroupId = childrenArea.Id,
            GroupCapacity = 10, // Soft capacity
            IsActive = true,
            IsArchived = false,
            CreatedDateTime = DateTime.UtcNow
        };

        // Toddler Room (location within children's area)
        var toddlerRoom = new Group
        {
            Id = 3,
            Name = "Toddler Room",
            Description = "Ages 2-3",
            GroupTypeId = _checkinGroupType.Id,
            CampusId = _testCampus.Id,
            ParentGroupId = childrenArea.Id,
            GroupCapacity = 15, // Soft capacity
            IsActive = true,
            IsArchived = false,
            CreatedDateTime = DateTime.UtcNow
        };

        // Volunteers area
        var volunteersArea = new Group
        {
            Id = 4,
            Name = "Volunteers",
            Description = "Volunteer check-in",
            GroupTypeId = _checkinGroupType.Id,
            CampusId = _testCampus.Id,
            ScheduleId = _sundayMorningSchedule.Id,
            IsActive = true,
            IsArchived = false,
            CreatedDateTime = DateTime.UtcNow
        };

        _context.Groups.AddRange(childrenArea, infantRoom, toddlerRoom, volunteersArea);
        _context.SaveChanges();
    }

    private void SeedAttendance(int locationGroupId, DateOnly date, int count)
    {
        var occurrence = new AttendanceOccurrence
        {
            GroupId = locationGroupId,
            OccurrenceDate = date,
            SundayDate = date,
            CreatedDateTime = DateTime.UtcNow
        };
        _context.AttendanceOccurrences.Add(occurrence);
        _context.SaveChanges();

        for (int i = 0; i < count; i++)
        {
            var attendance = new Attendance
            {
                OccurrenceId = occurrence.Id,
                StartDateTime = DateTime.UtcNow,
                EndDateTime = null, // Still checked in
                CreatedDateTime = DateTime.UtcNow
            };
            _context.Attendances.Add(attendance);
        }
        _context.SaveChanges();
    }

    [Fact]
    public async Task GetConfigurationByCampusAsync_ValidCampus_ReturnsConfiguration()
    {
        // Arrange
        var campusIdKey = _testCampus.IdKey;

        // Act
        var result = await _service.GetConfigurationByCampusAsync(campusIdKey);

        // Assert
        result.Should().NotBeNull();
        result!.Campus.Should().NotBeNull();
        result.Campus.Name.Should().Be("Main Campus");
        result.Areas.Should().NotBeEmpty();
        result.ServerTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task GetConfigurationByCampusAsync_InvalidCampus_ReturnsNull()
    {
        // Arrange
        var invalidIdKey = "INVALID";

        // Act
        var result = await _service.GetConfigurationByCampusAsync(invalidIdKey);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetActiveAreasAsync_ValidCampus_ReturnsActiveAreas()
    {
        // Arrange
        var campusIdKey = _testCampus.IdKey;

        // Act
        var result = await _service.GetActiveAreasAsync(campusIdKey);

        // Assert
        result.Should().NotBeEmpty();
        result.Should().HaveCount(2); // Children's Ministry and Volunteers
        result.Should().Contain(a => a.Name == "Children's Ministry");
        result.Should().Contain(a => a.Name == "Volunteers");
    }

    [Fact]
    public async Task GetActiveAreasAsync_IncludesLocations_ReturnsAreasWithLocations()
    {
        // Arrange
        var campusIdKey = _testCampus.IdKey;

        // Act
        var result = await _service.GetActiveAreasAsync(campusIdKey);

        // Assert
        var childrenArea = result.FirstOrDefault(a => a.Name == "Children's Ministry");
        childrenArea.Should().NotBeNull();
        childrenArea!.Locations.Should().NotBeEmpty();
        childrenArea.Locations.Should().HaveCount(2); // Infant Room and Toddler Room
        childrenArea.Locations.Should().Contain(l => l.Name == "Infant Room");
        childrenArea.Locations.Should().Contain(l => l.Name == "Toddler Room");
    }

    [Fact]
    public async Task GetAreaByIdKeyAsync_ValidArea_ReturnsAreaDetails()
    {
        // Arrange
        var area = await _context.Groups.FirstAsync(g => g.Name == "Children's Ministry");
        var areaIdKey = area.IdKey;

        // Act
        var result = await _service.GetAreaByIdKeyAsync(areaIdKey);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Children's Ministry");
        result.Description.Should().Be("Check-in for children ages 0-12");
        result.Locations.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAreaByIdKeyAsync_InvalidArea_ReturnsNull()
    {
        // Arrange
        var invalidIdKey = "INVALID";

        // Act
        var result = await _service.GetAreaByIdKeyAsync(invalidIdKey);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetLocationCapacityAsync_NoAttendance_ReturnsZeroCount()
    {
        // Arrange
        var infantRoom = await _context.Groups.FirstAsync(g => g.Name == "Infant Room");
        var locationIdKey = infantRoom.IdKey;

        // Act
        var result = await _service.GetLocationCapacityAsync(locationIdKey);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Infant Room");
        result.CurrentCount.Should().Be(0);
        result.CapacityStatus.Should().Be(CapacityStatus.Available);
    }

    [Fact]
    public async Task GetLocationCapacityAsync_WithAttendance_ReturnsCorrectCount()
    {
        // Arrange
        var infantRoom = await _context.Groups.FirstAsync(g => g.Name == "Infant Room");
        var locationIdKey = infantRoom.IdKey;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Seed attendance
        SeedAttendance(infantRoom.Id, today, 5);

        // Act
        var result = await _service.GetLocationCapacityAsync(locationIdKey, today);

        // Assert
        result.Should().NotBeNull();
        result!.CurrentCount.Should().Be(5);
        result.CapacityStatus.Should().Be(CapacityStatus.Available);
    }

    [Fact]
    public async Task GetLocationCapacityAsync_AtSoftCapacity_ReturnsWarningStatus()
    {
        // Arrange
        var infantRoom = await _context.Groups.FirstAsync(g => g.Name == "Infant Room");
        var locationIdKey = infantRoom.IdKey;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Seed attendance at capacity (10 = soft capacity)
        SeedAttendance(infantRoom.Id, today, 10);

        // Act
        var result = await _service.GetLocationCapacityAsync(locationIdKey, today);

        // Assert
        result.Should().NotBeNull();
        result!.CurrentCount.Should().Be(10);
        result.SoftCapacity.Should().Be(10);
        result.CapacityStatus.Should().Be(CapacityStatus.Warning);
    }

    [Fact]
    public async Task GetActiveSchedulesAsync_ValidCampus_ReturnsSchedules()
    {
        // Arrange
        var campusIdKey = _testCampus.IdKey;

        // For this test to work, we need the current time to be within the check-in window
        // Sunday 8:30 AM (30 minutes before 9:00 AM service)
        var testTime = GetNextSunday(DateTime.UtcNow).AddHours(8).AddMinutes(30);

        // Act
        var result = await _service.GetActiveSchedulesAsync(campusIdKey, testTime);

        // Assert
        result.Should().NotBeEmpty();
        result.Should().Contain(s => s.Name == "Sunday Morning 9:00 AM");
    }

    [Fact]
    public async Task GetActiveSchedulesAsync_OutsideCheckinWindow_ReturnsEmpty()
    {
        // Arrange
        var campusIdKey = _testCampus.IdKey;

        // Monday morning (not Sunday, so no active schedules)
        var testTime = GetNextMonday(DateTime.UtcNow).AddHours(9);

        // Act
        var result = await _service.GetActiveSchedulesAsync(campusIdKey, testTime);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task IsCheckinOpenAsync_WithinCheckinWindow_ReturnsTrue()
    {
        // Arrange
        var scheduleIdKey = _sundayMorningSchedule.IdKey;

        // Sunday 8:30 AM (within check-in window: 8:00 AM - 9:30 AM)
        var testTime = GetNextSunday(DateTime.UtcNow).AddHours(8).AddMinutes(30);

        // Act
        var result = await _service.IsCheckinOpenAsync(scheduleIdKey, testTime);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsCheckinOpenAsync_BeforeCheckinWindow_ReturnsFalse()
    {
        // Arrange
        var scheduleIdKey = _sundayMorningSchedule.IdKey;

        // Sunday 7:00 AM (before 8:00 AM check-in start)
        var testTime = GetNextSunday(DateTime.UtcNow).AddHours(7);

        // Act
        var result = await _service.IsCheckinOpenAsync(scheduleIdKey, testTime);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsCheckinOpenAsync_AfterCheckinWindow_ReturnsFalse()
    {
        // Arrange
        var scheduleIdKey = _sundayMorningSchedule.IdKey;

        // Sunday 10:00 AM (after 9:30 AM check-in end)
        var testTime = GetNextSunday(DateTime.UtcNow).AddHours(10);

        // Act
        var result = await _service.IsCheckinOpenAsync(scheduleIdKey, testTime);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsCheckinOpenAsync_InvalidSchedule_ReturnsFalse()
    {
        // Arrange
        var invalidIdKey = "INVALID";

        // Act
        var result = await _service.IsCheckinOpenAsync(invalidIdKey);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetActiveAreasAsync_WithCapacity_CalculatesCorrectStatus()
    {
        // Arrange
        var campusIdKey = _testCampus.IdKey;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var infantRoom = await _context.Groups.FirstAsync(g => g.Name == "Infant Room");
        var toddlerRoom = await _context.Groups.FirstAsync(g => g.Name == "Toddler Room");

        // Seed attendance
        SeedAttendance(infantRoom.Id, today, 10); // At soft capacity
        SeedAttendance(toddlerRoom.Id, today, 5);  // Below capacity

        // Act
        var result = await _service.GetActiveAreasAsync(campusIdKey);

        // Assert
        var childrenArea = result.FirstOrDefault(a => a.Name == "Children's Ministry");
        childrenArea.Should().NotBeNull();

        var infantLoc = childrenArea!.Locations.First(l => l.Name == "Infant Room");
        infantLoc.CurrentCount.Should().Be(10);
        infantLoc.CapacityStatus.Should().Be(CapacityStatus.Warning);

        var toddlerLoc = childrenArea.Locations.First(l => l.Name == "Toddler Room");
        toddlerLoc.CurrentCount.Should().Be(5);
        toddlerLoc.CapacityStatus.Should().Be(CapacityStatus.Available);

        // Area should be at warning level since one location is at capacity
        childrenArea.CapacityStatus.Should().Be(CapacityStatus.Warning);
    }

    [Fact]
    public async Task GetConfigurationByCampusAsync_Unauthenticated_ThrowsUnauthorized()
    {
        // Arrange
        _userContext.IsAuthenticated = false;
        var campusIdKey = _testCampus.IdKey;

        // Act
        var act = async () => await _service.GetConfigurationByCampusAsync(campusIdKey);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task GetActiveAreasAsync_Unauthenticated_ThrowsUnauthorized()
    {
        // Arrange
        _userContext.IsAuthenticated = false;
        var campusIdKey = _testCampus.IdKey;

        // Act
        var act = async () => await _service.GetActiveAreasAsync(campusIdKey);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task GetAreaByIdKeyAsync_Unauthenticated_ThrowsUnauthorized()
    {
        // Arrange
        _userContext.IsAuthenticated = false;
        var area = await _context.Groups.FirstAsync(g => g.Name == "Children's Ministry");
        var areaIdKey = area.IdKey;

        // Act
        var act = async () => await _service.GetAreaByIdKeyAsync(areaIdKey);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task GetLocationCapacityAsync_Unauthenticated_ThrowsUnauthorized()
    {
        // Arrange
        _userContext.IsAuthenticated = false;
        var infantRoom = await _context.Groups.FirstAsync(g => g.Name == "Infant Room");
        var locationIdKey = infantRoom.IdKey;

        // Act
        var act = async () => await _service.GetLocationCapacityAsync(locationIdKey);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task GetActiveSchedulesAsync_Unauthenticated_ThrowsUnauthorized()
    {
        // Arrange
        _userContext.IsAuthenticated = false;
        var campusIdKey = _testCampus.IdKey;

        // Act
        var act = async () => await _service.GetActiveSchedulesAsync(campusIdKey);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    // Helper methods

    private static DateTime GetNextSunday(DateTime from)
    {
        var daysUntilSunday = ((int)DayOfWeek.Sunday - (int)from.DayOfWeek + 7) % 7;
        if (daysUntilSunday == 0)
        {
            return from.Date; // Today is Sunday
        }
        return from.Date.AddDays(daysUntilSunday);
    }

    private static DateTime GetNextMonday(DateTime from)
    {
        var daysUntilMonday = ((int)DayOfWeek.Monday - (int)from.DayOfWeek + 7) % 7;
        if (daysUntilMonday == 0)
        {
            return from.Date.AddDays(7); // Next Monday
        }
        return from.Date.AddDays(daysUntilMonday);
    }


    #region FilterAreasByPersonEligibility Tests

    [Fact]
    public void FilterAreasByPersonEligibility_WithNoAreas_ReturnsEmptyList()
    {
        // Arrange
        var areas = new List<CheckinAreaDto>();
        var birthDate = new DateOnly(2015, 1, 1);
        var graduationYear = 2033;

        // Act
        var result = _service.FilterAreasByPersonEligibility(areas, birthDate, graduationYear);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void FilterAreasByPersonEligibility_PersonMatchesAgeRange_IncludesArea()
    {
        // Arrange
        var currentDate = new DateOnly(2025, 9, 1);
        var birthDate = new DateOnly(2020, 1, 1); // ~5 years old = 68 months

        _mockGradeService.Setup(s => s.CalculateAgeInMonths(birthDate, currentDate))
            .Returns(68);
        _mockGradeService.Setup(s => s.CalculateGrade(It.IsAny<int?>(), currentDate))
            .Returns((int?)null);

        var area = CreateTestAreaDto("Preschool", minAgeMonths: 36, maxAgeMonths: 72);
        var areas = new List<CheckinAreaDto> { area };

        // Act
        var result = _service.FilterAreasByPersonEligibility(areas, birthDate, null, currentDate);

        // Assert
        result.Should().ContainSingle();
        result.First().Name.Should().Be("Preschool");
    }

    [Fact]
    public void FilterAreasByPersonEligibility_PersonBelowMinAge_ExcludesArea()
    {
        // Arrange
        var currentDate = new DateOnly(2025, 9, 1);
        var birthDate = new DateOnly(2023, 1, 1); // ~2 years old = 32 months

        _mockGradeService.Setup(s => s.CalculateAgeInMonths(birthDate, currentDate))
            .Returns(32);

        var area = CreateTestAreaDto("Preschool", minAgeMonths: 36, maxAgeMonths: 72);
        var areas = new List<CheckinAreaDto> { area };

        // Act
        var result = _service.FilterAreasByPersonEligibility(areas, birthDate, null, currentDate);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void FilterAreasByPersonEligibility_PersonAboveMaxAge_ExcludesArea()
    {
        // Arrange
        var currentDate = new DateOnly(2025, 9, 1);
        var birthDate = new DateOnly(2018, 1, 1); // ~7 years old = 92 months

        _mockGradeService.Setup(s => s.CalculateAgeInMonths(birthDate, currentDate))
            .Returns(92);

        var area = CreateTestAreaDto("Preschool", minAgeMonths: 36, maxAgeMonths: 72);
        var areas = new List<CheckinAreaDto> { area };

        // Act
        var result = _service.FilterAreasByPersonEligibility(areas, birthDate, null, currentDate);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void FilterAreasByPersonEligibility_PersonMatchesGradeRange_IncludesArea()
    {
        // Arrange
        var currentDate = new DateOnly(2025, 9, 1);
        var graduationYear = 2033; // 5th grade (grade = 5)

        _mockGradeService.Setup(s => s.CalculateAgeInMonths(It.IsAny<DateOnly?>(), currentDate))
            .Returns((int?)null);
        _mockGradeService.Setup(s => s.CalculateGrade(graduationYear, currentDate))
            .Returns(5);

        var area = CreateTestAreaDto("Elementary", minGrade: 1, maxGrade: 6);
        var areas = new List<CheckinAreaDto> { area };

        // Act
        var result = _service.FilterAreasByPersonEligibility(areas, null, graduationYear, currentDate);

        // Assert
        result.Should().ContainSingle();
        result.First().Name.Should().Be("Elementary");
    }

    [Fact]
    public void FilterAreasByPersonEligibility_PersonBelowMinGrade_ExcludesArea()
    {
        // Arrange
        var currentDate = new DateOnly(2025, 9, 1);
        var graduationYear = 2038; // Kindergarten (grade = 0)

        _mockGradeService.Setup(s => s.CalculateGrade(graduationYear, currentDate))
            .Returns(0);

        var area = CreateTestAreaDto("Elementary", minGrade: 1, maxGrade: 6);
        var areas = new List<CheckinAreaDto> { area };

        // Act
        var result = _service.FilterAreasByPersonEligibility(areas, null, graduationYear, currentDate);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void FilterAreasByPersonEligibility_PersonAboveMaxGrade_ExcludesArea()
    {
        // Arrange
        var currentDate = new DateOnly(2025, 9, 1);
        var graduationYear = 2029; // 9th grade (grade = 9)

        _mockGradeService.Setup(s => s.CalculateGrade(graduationYear, currentDate))
            .Returns(9);

        var area = CreateTestAreaDto("Elementary", minGrade: 1, maxGrade: 6);
        var areas = new List<CheckinAreaDto> { area };

        // Act
        var result = _service.FilterAreasByPersonEligibility(areas, null, graduationYear, currentDate);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void FilterAreasByPersonEligibility_AreaWithNoRestrictions_AlwaysIncludesArea()
    {
        // Arrange
        var currentDate = new DateOnly(2025, 9, 1);
        var birthDate = new DateOnly(2020, 1, 1);
        var graduationYear = 2033;

        _mockGradeService.Setup(s => s.CalculateAgeInMonths(birthDate, currentDate))
            .Returns(68);
        _mockGradeService.Setup(s => s.CalculateGrade(graduationYear, currentDate))
            .Returns(5);

        var area = CreateTestAreaDto("All Ages", minAgeMonths: null, maxAgeMonths: null, minGrade: null, maxGrade: null);
        var areas = new List<CheckinAreaDto> { area };

        // Act
        var result = _service.FilterAreasByPersonEligibility(areas, birthDate, graduationYear, currentDate);

        // Assert
        result.Should().ContainSingle();
    }

    [Fact]
    public void FilterAreasByPersonEligibility_PersonWithNoBirthDate_PassesAgeFilters()
    {
        // Arrange
        var currentDate = new DateOnly(2025, 9, 1);

        _mockGradeService.Setup(s => s.CalculateAgeInMonths(null, currentDate))
            .Returns((int?)null);

        var area = CreateTestAreaDto("Preschool", minAgeMonths: 36, maxAgeMonths: 72);
        var areas = new List<CheckinAreaDto> { area };

        // Act - Person with no birth date should pass age filters
        var result = _service.FilterAreasByPersonEligibility(areas, null, null, currentDate);

        // Assert
        result.Should().ContainSingle();
    }

    [Fact]
    public void FilterAreasByPersonEligibility_PersonWithNoGraduationYear_PassesGradeFilters()
    {
        // Arrange
        var currentDate = new DateOnly(2025, 9, 1);

        _mockGradeService.Setup(s => s.CalculateGrade(null, currentDate))
            .Returns((int?)null);

        var area = CreateTestAreaDto("Elementary", minGrade: 1, maxGrade: 6);
        var areas = new List<CheckinAreaDto> { area };

        // Act - Person with no graduation year should pass grade filters
        var result = _service.FilterAreasByPersonEligibility(areas, null, null, currentDate);

        // Assert
        result.Should().ContainSingle();
    }

    [Fact]
    public void FilterAreasByPersonEligibility_PreKGrade_HandlesNegativeGrade()
    {
        // Arrange
        var currentDate = new DateOnly(2025, 9, 1);
        var graduationYear = 2039; // Pre-K (grade = -1)

        _mockGradeService.Setup(s => s.CalculateGrade(graduationYear, currentDate))
            .Returns(-1);

        var area = CreateTestAreaDto("Preschool", minGrade: -1, maxGrade: 0);
        var areas = new List<CheckinAreaDto> { area };

        // Act
        var result = _service.FilterAreasByPersonEligibility(areas, null, graduationYear, currentDate);

        // Assert
        result.Should().ContainSingle();
        result.First().Name.Should().Be("Preschool");
    }

    [Fact]
    public void FilterAreasByPersonEligibility_MultipleAreas_FiltersCorrectly()
    {
        // Arrange
        var currentDate = new DateOnly(2025, 9, 1);
        var birthDate = new DateOnly(2020, 1, 1); // ~5 years old
        var graduationYear = 2033; // 5th grade

        _mockGradeService.Setup(s => s.CalculateAgeInMonths(birthDate, currentDate))
            .Returns(68);
        _mockGradeService.Setup(s => s.CalculateGrade(graduationYear, currentDate))
            .Returns(5);

        var areas = new List<CheckinAreaDto>
        {
            CreateTestAreaDto("Nursery", minAgeMonths: 0, maxAgeMonths: 24), // Too young
            CreateTestAreaDto("Preschool", minAgeMonths: 36, maxAgeMonths: 72), // Age matches, no grade filter
            CreateTestAreaDto("Elementary", minGrade: 1, maxGrade: 6), // Grade matches, no age filter
            CreateTestAreaDto("Youth", minGrade: 7, maxGrade: 12), // Grade too high
            CreateTestAreaDto("All Ages") // No restrictions
        };

        // Act
        var result = _service.FilterAreasByPersonEligibility(areas, birthDate, graduationYear, currentDate);

        // Assert
        result.Should().HaveCount(3);
        result.Select(a => a.Name).Should().Contain(new[] { "Preschool", "Elementary", "All Ages" });
    }

    private CheckinAreaDto CreateTestAreaDto(
        string name,
        int? minAgeMonths = null,
        int? maxAgeMonths = null,
        int? minGrade = null,
        int? maxGrade = null)
    {
        return new CheckinAreaDto
        {
            IdKey = "test",
            Guid = Guid.NewGuid(),
            Name = name,
            GroupType = new GroupTypeSummaryDto
            {
                IdKey = "test",
                Guid = Guid.NewGuid(),
                Name = "Test Type",
                IsFamilyGroupType = false,
                AllowMultipleLocations = true,
                Roles = new List<GroupTypeRoleDto>()
            },
            Locations = new List<CheckinLocationDto>(),
            IsActive = true,
            CapacityStatus = CapacityStatus.Available,
            MinAgeMonths = minAgeMonths,
            MaxAgeMonths = maxAgeMonths,
            MinGrade = minGrade,
            MaxGrade = maxGrade
        };
    }

    #endregion

    #region Validation Tests

    [Fact]
    public void FilterAreasByPersonEligibility_InvalidAgeRange_ThrowsArgumentException()
    {
        // Arrange
        var area = CreateTestAreaDto("Test Area", minAgeMonths: 100, maxAgeMonths: 50);
        var areas = new List<CheckinAreaDto> { area };

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            _service.FilterAreasByPersonEligibility(areas, DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-5)), null));

        exception.Message.Should().Contain("MinAgeMonths (100) cannot be greater than MaxAgeMonths (50)");
    }

    [Fact]
    public void FilterAreasByPersonEligibility_NegativeMinAge_ThrowsArgumentException()
    {
        // Arrange
        var area = CreateTestAreaDto("Test Area", minAgeMonths: -1);
        var areas = new List<CheckinAreaDto> { area };

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            _service.FilterAreasByPersonEligibility(areas, DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-5)), null));

        exception.Message.Should().Contain("MinAgeMonths cannot be negative");
    }

    [Fact]
    public void FilterAreasByPersonEligibility_NegativeMaxAge_ThrowsArgumentException()
    {
        // Arrange
        var area = CreateTestAreaDto("Test Area", maxAgeMonths: -10);
        var areas = new List<CheckinAreaDto> { area };

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            _service.FilterAreasByPersonEligibility(areas, DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-5)), null));

        exception.Message.Should().Contain("MaxAgeMonths cannot be negative");
    }

    [Fact]
    public void FilterAreasByPersonEligibility_ExcessiveMaxAge_ThrowsArgumentException()
    {
        // Arrange
        var area = CreateTestAreaDto("Test Area", maxAgeMonths: 1500); // Over 100 years
        var areas = new List<CheckinAreaDto> { area };

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            _service.FilterAreasByPersonEligibility(areas, DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-5)), null));

        exception.Message.Should().Contain("exceeds reasonable limit (1200 months)");
    }

    [Fact]
    public void FilterAreasByPersonEligibility_InvalidGradeRange_ThrowsArgumentException()
    {
        // Arrange
        var area = CreateTestAreaDto("Test Area", minGrade: 8, maxGrade: 3);
        var areas = new List<CheckinAreaDto> { area };

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            _service.FilterAreasByPersonEligibility(areas, null, 2025));

        exception.Message.Should().Contain("MinGrade (8) cannot be greater than MaxGrade (3)");
    }

    [Fact]
    public void FilterAreasByPersonEligibility_GradeBelowReasonableLimit_ThrowsArgumentException()
    {
        // Arrange
        var area = CreateTestAreaDto("Test Area", minGrade: -5); // Below kindergarten
        var areas = new List<CheckinAreaDto> { area };

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            _service.FilterAreasByPersonEligibility(areas, null, 2025));

        exception.Message.Should().Contain("is below reasonable limit (-1)");
    }

    [Fact]
    public void FilterAreasByPersonEligibility_GradeAboveReasonableLimit_ThrowsArgumentException()
    {
        // Arrange
        var area = CreateTestAreaDto("Test Area", maxGrade: 15); // Above 12th grade
        var areas = new List<CheckinAreaDto> { area };

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            _service.FilterAreasByPersonEligibility(areas, null, 2025));

        exception.Message.Should().Contain("exceeds reasonable limit (12)");
    }

    [Fact]
    public void FilterAreasByPersonEligibility_ValidAgeRange_DoesNotThrow()
    {
        // Arrange
        _mockGradeService.Setup(s => s.CalculateAgeInMonths(It.IsAny<DateOnly?>(), It.IsAny<DateOnly?>()))
            .Returns(60); // 5 years old

        var area = CreateTestAreaDto("Test Area", minAgeMonths: 36, maxAgeMonths: 72);
        var areas = new List<CheckinAreaDto> { area };

        // Act
        var result = _service.FilterAreasByPersonEligibility(
            areas,
            DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-5)),
            null);

        // Assert
        result.Should().HaveCount(1);
    }

    [Fact]
    public void FilterAreasByPersonEligibility_ValidGradeRange_DoesNotThrow()
    {
        // Arrange
        _mockGradeService.Setup(s => s.CalculateGrade(It.IsAny<int?>(), It.IsAny<DateOnly?>()))
            .Returns(5); // 5th grade

        var area = CreateTestAreaDto("Test Area", minGrade: 3, maxGrade: 8);
        var areas = new List<CheckinAreaDto> { area };

        // Act
        var result = _service.FilterAreasByPersonEligibility(
            areas,
            null,
            2028);

        // Assert
        result.Should().HaveCount(1);
    }

    [Fact]
    public void FilterAreasByPersonEligibility_NoRangesSpecified_DoesNotThrow()
    {
        // Arrange
        var area = CreateTestAreaDto("Test Area"); // No age or grade restrictions
        var areas = new List<CheckinAreaDto> { area };

        // Act
        var result = _service.FilterAreasByPersonEligibility(areas, null, null);

        // Assert
        result.Should().HaveCount(1);
    }

    #endregion

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
