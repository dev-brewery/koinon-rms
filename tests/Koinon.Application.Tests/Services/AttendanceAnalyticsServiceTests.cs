using FluentAssertions;
using Koinon.Application.DTOs;
using Koinon.Application.Services;
using Koinon.Domain.Data;
using Koinon.Domain.Entities;
using Koinon.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Koinon.Application.Tests.Services;

/// <summary>
/// Tests for AttendanceAnalyticsService.
/// </summary>
public class AttendanceAnalyticsServiceTests : IDisposable
{
    private readonly KoinonDbContext _context;
    private readonly Mock<ILogger<AttendanceAnalyticsService>> _mockLogger;
    private readonly AttendanceAnalyticsService _service;

    public AttendanceAnalyticsServiceTests()
    {
        // Setup in-memory database
        var options = new DbContextOptionsBuilder<KoinonDbContext>()
            .UseInMemoryDatabase(databaseName: $"KoinonTestDb_{Guid.NewGuid()}")
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new KoinonDbContext(options);

        // Manually initialize the context
        _context.Database.EnsureCreated();

        // Setup logger mock
        _mockLogger = new Mock<ILogger<AttendanceAnalyticsService>>();

        // Create service
        _service = new AttendanceAnalyticsService(_context, _mockLogger.Object);

        // Seed test data
        SeedTestData();
    }

    private void SeedTestData()
    {
        var now = DateTime.UtcNow;
        var today = DateOnly.FromDateTime(now);

        // Add group types
        var worshipGroupType = new GroupType
        {
            Id = 1,
            Name = "Worship Service",
            IsFamilyGroupType = false,
            AllowMultipleLocations = false,
            IsSystem = false,
            CreatedDateTime = now
        };

        var smallGroupType = new GroupType
        {
            Id = 2,
            Name = "Small Group",
            IsFamilyGroupType = false,
            AllowMultipleLocations = false,
            IsSystem = false,
            CreatedDateTime = now
        };

        _context.GroupTypes.AddRange(worshipGroupType, smallGroupType);

        // Add groups
        var sundayService = new Group
        {
            Id = 1,
            GroupTypeId = 1,
            Name = "Sunday Service",
            IsActive = true,
            IsArchived = false,
            CreatedDateTime = now
        };

        var eveningService = new Group
        {
            Id = 2,
            GroupTypeId = 1,
            Name = "Evening Service",
            IsActive = true,
            IsArchived = false,
            CreatedDateTime = now
        };

        var smallGroup1 = new Group
        {
            Id = 3,
            GroupTypeId = 2,
            Name = "Small Group 1",
            IsActive = true,
            IsArchived = false,
            CreatedDateTime = now
        };

        _context.Groups.AddRange(sundayService, eveningService, smallGroup1);

        // Add attendance occurrences
        var occurrence1 = new AttendanceOccurrence
        {
            Id = 1,
            GroupId = 1,
            OccurrenceDate = today.AddDays(-14),
            SundayDate = today.AddDays(-14),
            CreatedDateTime = now
        };

        var occurrence2 = new AttendanceOccurrence
        {
            Id = 2,
            GroupId = 1,
            OccurrenceDate = today.AddDays(-7),
            SundayDate = today.AddDays(-7),
            CreatedDateTime = now
        };

        var occurrence3 = new AttendanceOccurrence
        {
            Id = 3,
            GroupId = 1,
            OccurrenceDate = today,
            SundayDate = today,
            CreatedDateTime = now
        };

        var occurrence4 = new AttendanceOccurrence
        {
            Id = 4,
            GroupId = 2,
            OccurrenceDate = today.AddDays(-7),
            SundayDate = today.AddDays(-7),
            CreatedDateTime = now
        };

        var occurrence5 = new AttendanceOccurrence
        {
            Id = 5,
            GroupId = 3,
            OccurrenceDate = today.AddDays(-3),
            SundayDate = today.AddDays(-7),
            CreatedDateTime = now
        };

        _context.AttendanceOccurrences.AddRange(occurrence1, occurrence2, occurrence3, occurrence4, occurrence5);

        // Add person aliases
        var personAlias1 = new PersonAlias
        {
            Id = 1,
            PersonId = 1,
            AliasPersonId = 1,
            CreatedDateTime = now
        };

        var personAlias2 = new PersonAlias
        {
            Id = 2,
            PersonId = 2,
            AliasPersonId = 2,
            CreatedDateTime = now
        };

        var personAlias3 = new PersonAlias
        {
            Id = 3,
            PersonId = 3,
            AliasPersonId = 3,
            CreatedDateTime = now
        };

        _context.PersonAliases.AddRange(personAlias1, personAlias2, personAlias3);

        // Add attendances
        var attendances = new List<Attendance>
        {
            // Occurrence 1 (14 days ago) - Sunday Service
            new() { Id = 1, PersonAliasId = 1, OccurrenceId = 1, StartDateTime = now.AddDays(-14), IsFirstTime = true, CreatedDateTime = now },
            new() { Id = 2, PersonAliasId = 2, OccurrenceId = 1, StartDateTime = now.AddDays(-14), IsFirstTime = false, CreatedDateTime = now },
            new() { Id = 3, PersonAliasId = 3, OccurrenceId = 1, StartDateTime = now.AddDays(-14), IsFirstTime = false, CreatedDateTime = now },

            // Occurrence 2 (7 days ago) - Sunday Service
            new() { Id = 4, PersonAliasId = 1, OccurrenceId = 2, StartDateTime = now.AddDays(-7), IsFirstTime = false, CreatedDateTime = now },
            new() { Id = 5, PersonAliasId = 2, OccurrenceId = 2, StartDateTime = now.AddDays(-7), IsFirstTime = false, CreatedDateTime = now },

            // Occurrence 3 (today) - Sunday Service
            new() { Id = 6, PersonAliasId = 1, OccurrenceId = 3, StartDateTime = now, IsFirstTime = false, CreatedDateTime = now },
            new() { Id = 7, PersonAliasId = 2, OccurrenceId = 3, StartDateTime = now, IsFirstTime = false, CreatedDateTime = now },
            new() { Id = 8, PersonAliasId = 3, OccurrenceId = 3, StartDateTime = now, IsFirstTime = false, CreatedDateTime = now },

            // Occurrence 4 (7 days ago) - Evening Service
            new() { Id = 9, PersonAliasId = 1, OccurrenceId = 4, StartDateTime = now.AddDays(-7), IsFirstTime = false, CreatedDateTime = now },
            new() { Id = 10, PersonAliasId = 3, OccurrenceId = 4, StartDateTime = now.AddDays(-7), IsFirstTime = true, CreatedDateTime = now },

            // Occurrence 5 (3 days ago) - Small Group 1
            new() { Id = 11, PersonAliasId = 2, OccurrenceId = 5, StartDateTime = now.AddDays(-3), IsFirstTime = false, CreatedDateTime = now },
            new() { Id = 12, PersonAliasId = 3, OccurrenceId = 5, StartDateTime = now.AddDays(-3), IsFirstTime = false, CreatedDateTime = now },
        };

        _context.Attendances.AddRange(attendances);

        _context.SaveChanges();
    }

    [Fact]
    public async Task GetSummaryAsync_ReturnsCorrectTotalAttendance()
    {
        // Arrange
        var options = new AttendanceQueryOptions(
            StartDate: DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-30),
            EndDate: DateOnly.FromDateTime(DateTime.UtcNow));

        // Act
        var summary = await _service.GetSummaryAsync(options);

        // Assert
        summary.TotalAttendance.Should().Be(12); // All attendance records within range
    }

    [Fact]
    public async Task GetSummaryAsync_ReturnsCorrectUniqueAttendees()
    {
        // Arrange
        var options = new AttendanceQueryOptions(
            StartDate: DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-30),
            EndDate: DateOnly.FromDateTime(DateTime.UtcNow));

        // Act
        var summary = await _service.GetSummaryAsync(options);

        // Assert
        summary.UniqueAttendees.Should().Be(3); // 3 unique PersonAliasIds
    }

    [Fact]
    public async Task GetSummaryAsync_ReturnsCorrectFirstTimeVisitors()
    {
        // Arrange
        var options = new AttendanceQueryOptions(
            StartDate: DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-30),
            EndDate: DateOnly.FromDateTime(DateTime.UtcNow));

        // Act
        var summary = await _service.GetSummaryAsync(options);

        // Assert
        summary.FirstTimeVisitors.Should().Be(2); // 2 records with IsFirstTime = true
    }

    [Fact]
    public async Task GetSummaryAsync_ReturnsCorrectReturningVisitors()
    {
        // Arrange
        var options = new AttendanceQueryOptions(
            StartDate: DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-30),
            EndDate: DateOnly.FromDateTime(DateTime.UtcNow));

        // Act
        var summary = await _service.GetSummaryAsync(options);

        // Assert
        summary.ReturningVisitors.Should().Be(10); // Total - FirstTime = 12 - 2 = 10
    }

    [Fact]
    public async Task GetSummaryAsync_CalculatesAverageAttendancePerOccurrence()
    {
        // Arrange
        var options = new AttendanceQueryOptions(
            StartDate: DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-30),
            EndDate: DateOnly.FromDateTime(DateTime.UtcNow));

        // Act
        var summary = await _service.GetSummaryAsync(options);

        // Assert
        // 12 total attendance / 5 occurrences = 2.4
        summary.AverageAttendance.Should().Be(2.4m);
    }

    [Fact]
    public async Task GetSummaryAsync_WithGroupFilter_FiltersCorrectly()
    {
        // Arrange
        var groupIdKey = IdKeyHelper.Encode(1); // Sunday Service
        var options = new AttendanceQueryOptions(
            StartDate: DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-30),
            EndDate: DateOnly.FromDateTime(DateTime.UtcNow),
            GroupIdKey: groupIdKey);

        // Act
        var summary = await _service.GetSummaryAsync(options);

        // Assert
        summary.TotalAttendance.Should().Be(8); // Only attendance for Sunday Service (occurrences 1, 2, 3)
    }

    [Fact]
    public async Task GetTrendsAsync_GroupsByDay_ReturnsCorrectData()
    {
        // Arrange
        var options = new AttendanceQueryOptions(
            StartDate: DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-30),
            EndDate: DateOnly.FromDateTime(DateTime.UtcNow),
            GroupBy: GroupBy.Day);

        // Act
        var trends = await _service.GetTrendsAsync(options);

        // Assert
        trends.Should().NotBeEmpty();
        trends.Should().HaveCount(4); // 4 distinct dates (14 days ago, 7 days ago, 3 days ago, today)

        // Verify trends are sorted by date
        trends.Should().BeInAscendingOrder(t => t.Date);
    }

    [Fact]
    public async Task GetTrendsAsync_GroupsByWeek_ReturnsCorrectData()
    {
        // Arrange
        var options = new AttendanceQueryOptions(
            StartDate: DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-30),
            EndDate: DateOnly.FromDateTime(DateTime.UtcNow),
            GroupBy: GroupBy.Week);

        // Act
        var trends = await _service.GetTrendsAsync(options);

        // Assert
        trends.Should().NotBeEmpty();
        // All occurrences should be grouped into weeks
        foreach (var trend in trends)
        {
            trend.Count.Should().BeGreaterThan(0);
            trend.Date.DayOfWeek.Should().Be(DayOfWeek.Sunday); // Week should start on Sunday
        }
    }

    [Fact]
    public async Task GetByGroupAsync_ReturnsCorrectGroupBreakdown()
    {
        // Arrange
        var options = new AttendanceQueryOptions(
            StartDate: DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-30),
            EndDate: DateOnly.FromDateTime(DateTime.UtcNow));

        // Act
        var groupData = await _service.GetByGroupAsync(options);

        // Assert
        groupData.Should().NotBeEmpty();
        groupData.Should().HaveCount(3); // 3 groups with attendance

        // Verify sorted by total attendance (descending)
        groupData.Should().BeInDescendingOrder(g => g.TotalAttendance);

        // Check the highest attendance group (Sunday Service)
        var sundayService = groupData.First();
        sundayService.GroupName.Should().Be("Sunday Service");
        sundayService.TotalAttendance.Should().Be(8);
        sundayService.UniqueAttendees.Should().Be(3);
        sundayService.GroupIdKey.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetByGroupAsync_WithGroupTypeFilter_FiltersCorrectly()
    {
        // Arrange
        var groupTypeIdKey = IdKeyHelper.Encode(2); // Small Group
        var options = new AttendanceQueryOptions(
            StartDate: DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-30),
            EndDate: DateOnly.FromDateTime(DateTime.UtcNow),
            GroupTypeIdKey: groupTypeIdKey);

        // Act
        var groupData = await _service.GetByGroupAsync(options);

        // Assert
        groupData.Should().HaveCount(1); // Only Small Group 1
        groupData.First().GroupName.Should().Be("Small Group 1");
        groupData.First().GroupTypeName.Should().Be("Small Group");
    }

    [Fact]
    public async Task GetSummaryAsync_WithNoData_ReturnsZeroCounts()
    {
        // Arrange - Clear all data
        _context.Attendances.RemoveRange(_context.Attendances);
        await _context.SaveChangesAsync();

        var options = new AttendanceQueryOptions(
            StartDate: DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-30),
            EndDate: DateOnly.FromDateTime(DateTime.UtcNow));

        // Act
        var summary = await _service.GetSummaryAsync(options);

        // Assert
        summary.TotalAttendance.Should().Be(0);
        summary.UniqueAttendees.Should().Be(0);
        summary.FirstTimeVisitors.Should().Be(0);
        summary.ReturningVisitors.Should().Be(0);
        summary.AverageAttendance.Should().Be(0);
    }

    [Fact]
    public async Task GetSummaryAsync_UsesDefaultDateRange_WhenNotProvided()
    {
        // Arrange
        var options = new AttendanceQueryOptions(); // No dates specified

        // Act
        var summary = await _service.GetSummaryAsync(options);

        // Assert
        // Should default to last 30 days
        summary.StartDate.Should().Be(DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-30));
        summary.EndDate.Should().Be(DateOnly.FromDateTime(DateTime.UtcNow));
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
