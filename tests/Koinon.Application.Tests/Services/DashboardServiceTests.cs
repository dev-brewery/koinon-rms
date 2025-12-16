using FluentAssertions;
using Koinon.Application.Services;
using Koinon.Domain.Entities;
using Koinon.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Koinon.Application.Tests.Services;

/// <summary>
/// Tests for DashboardService.
/// </summary>
public class DashboardServiceTests : IDisposable
{
    private readonly KoinonDbContext _context;
    private readonly Mock<ILogger<DashboardService>> _mockLogger;
    private readonly DashboardService _service;

    public DashboardServiceTests()
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
        _mockLogger = new Mock<ILogger<DashboardService>>();

        // Create service
        _service = new DashboardService(_context, _mockLogger.Object);

        // Seed test data
        SeedTestData();
    }

    private void SeedTestData()
    {
        var now = DateTime.UtcNow;
        var today = DateOnly.FromDateTime(now);

        // Add group types (family and non-family)
        var familyGroupType = new GroupType
        {
            Id = 1,
            Name = "Family",
            AllowMultipleLocations = false,
            IsSystem = true,
            CreatedDateTime = now
        };

        var servingGroupType = new GroupType
        {
            Id = 2,
            Name = "Serving Team",
            AllowMultipleLocations = false,
            IsSystem = false,
            CreatedDateTime = now
        };

        _context.GroupTypes.AddRange(familyGroupType, servingGroupType);

        // Add people (some deceased)
        var people = new List<Person>
        {
            new() { Id = 1, FirstName = "John", LastName = "Doe", IsDeceased = false, CreatedDateTime = now },
            new() { Id = 2, FirstName = "Jane", LastName = "Smith", IsDeceased = false, CreatedDateTime = now },
            new() { Id = 3, FirstName = "Bob", LastName = "Johnson", IsDeceased = false, CreatedDateTime = now },
            new() { Id = 4, FirstName = "Deceased", LastName = "Person", IsDeceased = true, CreatedDateTime = now }
        };
        _context.People.AddRange(people);

        // Add families using Family entity (not Group)
        var families = new List<Family>
        {
            new() { Id = 1, Name = "Doe Family", IsActive = true, CreatedDateTime = now },
            new() { Id = 2, Name = "Smith Family", IsActive = true, CreatedDateTime = now },
            new() { Id = 3, Name = "Archived Family", IsActive = false, CreatedDateTime = now }
        };
        _context.Families.AddRange(families);

        // Add non-family groups (should be counted in ActiveGroups)
        var groups = new List<Group>
        {
            new() { Id = 4, GroupTypeId = 2, Name = "Worship Team", IsActive = true, IsArchived = false, CreatedDateTime = now },
            new() { Id = 5, GroupTypeId = 2, Name = "Audio Team", IsActive = true, IsArchived = false, CreatedDateTime = now },
            new() { Id = 6, GroupTypeId = 2, Name = "Inactive Team", IsActive = false, IsArchived = false, CreatedDateTime = now },
            new() { Id = 7, GroupTypeId = 2, Name = "Archived Team", IsActive = true, IsArchived = true, ArchivedDateTime = now, CreatedDateTime = now }
        };
        _context.Groups.AddRange(groups);

        // Add schedules (active and inactive)
        var schedules = new List<Schedule>
        {
            new()
            {
                Id = 1,
                Name = "Sunday Morning",
                IsActive = true,
                WeeklyDayOfWeek = DayOfWeek.Sunday,
                WeeklyTimeOfDay = TimeSpan.FromHours(10),
                CheckInStartOffsetMinutes = 60,
                Order = 0,
                CreatedDateTime = now
            },
            new()
            {
                Id = 2,
                Name = "Wednesday Evening",
                IsActive = true,
                WeeklyDayOfWeek = DayOfWeek.Wednesday,
                WeeklyTimeOfDay = TimeSpan.FromHours(19),
                CheckInStartOffsetMinutes = 30,
                Order = 1,
                CreatedDateTime = now
            },
            new()
            {
                Id = 3,
                Name = "Inactive Schedule",
                IsActive = false,
                WeeklyDayOfWeek = DayOfWeek.Saturday,
                WeeklyTimeOfDay = TimeSpan.FromHours(18),
                Order = 2,
                CreatedDateTime = now
            }
        };
        _context.Schedules.AddRange(schedules);

        // Add attendances (check-ins for today and last week)
        var todayStart = today.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var lastWeek = today.AddDays(-7);
        var lastWeekStart = lastWeek.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

        var attendances = new List<Attendance>
        {
            // Today's check-ins
            new() { Id = 1, PersonAliasId = 1, OccurrenceId = 1, StartDateTime = todayStart.AddHours(9), CreatedDateTime = now },
            new() { Id = 2, PersonAliasId = 2, OccurrenceId = 1, StartDateTime = todayStart.AddHours(10), CreatedDateTime = now },
            new() { Id = 3, PersonAliasId = 3, OccurrenceId = 1, StartDateTime = todayStart.AddHours(11), CreatedDateTime = now },

            // Last week's check-ins (same day)
            new() { Id = 4, PersonAliasId = 1, OccurrenceId = 2, StartDateTime = lastWeekStart.AddHours(9), CreatedDateTime = now.AddDays(-7) },
            new() { Id = 5, PersonAliasId = 2, OccurrenceId = 2, StartDateTime = lastWeekStart.AddHours(10), CreatedDateTime = now.AddDays(-7) },

            // Other days (should not be counted)
            new() { Id = 6, PersonAliasId = 1, OccurrenceId = 3, StartDateTime = todayStart.AddDays(-2).AddHours(9), CreatedDateTime = now.AddDays(-2) }
        };
        _context.Attendances.AddRange(attendances);

        _context.SaveChanges();
    }

    [Fact]
    public async Task GetStatsAsync_ReturnsCorrectPeopleCounts()
    {
        // Act
        var stats = await _service.GetStatsAsync(CancellationToken.None);

        // Assert
        stats.TotalPeople.Should().Be(3); // Excludes deceased person
    }

    [Fact]
    public async Task GetStatsAsync_ReturnsCorrectFamilyCounts()
    {
        // Act
        var stats = await _service.GetStatsAsync(CancellationToken.None);

        // Assert
        stats.TotalFamilies.Should().Be(2); // Excludes archived family
    }

    [Fact]
    public async Task GetStatsAsync_ReturnsCorrectGroupCounts()
    {
        // Act
        var stats = await _service.GetStatsAsync(CancellationToken.None);

        // Assert
        stats.ActiveGroups.Should().Be(2); // Only active, non-archived, non-family groups
    }

    [Fact]
    public async Task GetStatsAsync_ReturnsCorrectScheduleCounts()
    {
        // Act
        var stats = await _service.GetStatsAsync(CancellationToken.None);

        // Assert
        stats.ActiveSchedules.Should().Be(2); // Only active schedules
    }

    [Fact]
    public async Task GetStatsAsync_ReturnsCorrectCheckInCounts()
    {
        // Act
        var stats = await _service.GetStatsAsync(CancellationToken.None);

        // Assert
        stats.TodayCheckIns.Should().Be(3); // Check-ins for today
        stats.LastWeekCheckIns.Should().Be(2); // Check-ins for same day last week
    }

    [Fact]
    public async Task GetStatsAsync_ReturnsUpcomingSchedules_SortedByNextOccurrence()
    {
        // Act
        var stats = await _service.GetStatsAsync(CancellationToken.None);

        // Assert
        stats.UpcomingSchedules.Should().NotBeNull();
        stats.UpcomingSchedules.Should().HaveCountLessOrEqualTo(5); // Maximum 5 schedules
        stats.UpcomingSchedules.Should().HaveCount(2); // We have 2 active schedules with weekly times

        // Verify schedules are sorted by next occurrence
        for (int i = 0; i < stats.UpcomingSchedules.Count - 1; i++)
        {
            stats.UpcomingSchedules[i].NextOccurrence.Should().BeOnOrBefore(stats.UpcomingSchedules[i + 1].NextOccurrence);
        }

        // Verify all schedules have valid data
        foreach (var schedule in stats.UpcomingSchedules)
        {
            schedule.IdKey.Should().NotBeNullOrEmpty();
            schedule.Name.Should().NotBeNullOrEmpty();
            schedule.NextOccurrence.Should().BeAfter(DateTime.UtcNow.AddMinutes(-1)); // Should be in the future or very close to now
        }
    }

    [Fact]
    public async Task GetStatsAsync_WithNoData_ReturnsZeroCounts()
    {
        // Arrange - Clear all data
        _context.People.RemoveRange(_context.People);
        _context.Families.RemoveRange(_context.Families);
        _context.Groups.RemoveRange(_context.Groups);
        _context.Schedules.RemoveRange(_context.Schedules);
        _context.Attendances.RemoveRange(_context.Attendances);
        await _context.SaveChangesAsync();

        // Act
        var stats = await _service.GetStatsAsync(CancellationToken.None);

        // Assert
        stats.TotalPeople.Should().Be(0);
        stats.TotalFamilies.Should().Be(0);
        stats.ActiveGroups.Should().Be(0);
        stats.ActiveSchedules.Should().Be(0);
        stats.TodayCheckIns.Should().Be(0);
        stats.LastWeekCheckIns.Should().Be(0);
        stats.UpcomingSchedules.Should().BeEmpty();
    }

    [Fact]
    public async Task GetStatsAsync_UpcomingSchedules_CalculatesMinutesUntilCheckIn()
    {
        // Act
        var stats = await _service.GetStatsAsync(CancellationToken.None);

        // Assert
        stats.UpcomingSchedules.Should().NotBeEmpty();

        foreach (var schedule in stats.UpcomingSchedules)
        {
            // MinutesUntilCheckIn should be calculated based on NextOccurrence minus CheckInStartOffsetMinutes
            schedule.MinutesUntilCheckIn.Should().NotBe(0); // Should have a calculated value
        }
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
