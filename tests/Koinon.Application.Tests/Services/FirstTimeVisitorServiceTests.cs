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
/// Tests for FirstTimeVisitorService.
/// </summary>
public class FirstTimeVisitorServiceTests : IDisposable
{
    private readonly KoinonDbContext _context;
    private readonly Mock<ILogger<FirstTimeVisitorService>> _mockLogger;
    private readonly FirstTimeVisitorService _service;

    public FirstTimeVisitorServiceTests()
    {
        // Setup in-memory database
        var options = new DbContextOptionsBuilder<KoinonDbContext>()
            .UseInMemoryDatabase(databaseName: $"KoinonTestDb_{Guid.NewGuid()}")
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new KoinonDbContext(options);
        _context.Database.EnsureCreated();

        // Setup logger mock
        _mockLogger = new Mock<ILogger<FirstTimeVisitorService>>();

        // Create service
        _service = new FirstTimeVisitorService(_context, _mockLogger.Object);

        // Seed test data
        SeedTestData();
    }

    private void SeedTestData()
    {
        // Add campus
        var campus = new Campus
        {
            Id = 1,
            Name = "Main Campus",
            ShortCode = "MAIN",
            IsActive = true,
            CreatedDateTime = DateTime.UtcNow
        };
        _context.Campuses.Add(campus);

        // Add group type
        var groupType = new GroupType
        {
            Id = 1,
            Name = "Children's Ministry",
            TakesAttendance = true,
            CreatedDateTime = DateTime.UtcNow
        };
        _context.GroupTypes.Add(groupType);

        // Add group
        var group = new Group
        {
            Id = 1,
            GroupTypeId = 1,
            CampusId = 1,
            Name = "Kindergarten",
            IsActive = true,
            CreatedDateTime = DateTime.UtcNow
        };
        _context.Groups.Add(group);

        // Add people
        var person1 = new Person
        {
            Id = 1,
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com",
            CreatedDateTime = DateTime.UtcNow
        };

        var person2 = new Person
        {
            Id = 2,
            FirstName = "Jane",
            LastName = "Smith",
            Email = "jane.smith@example.com",
            CreatedDateTime = DateTime.UtcNow
        };

        _context.People.AddRange(person1, person2);

        // Add person aliases
        var alias1 = new PersonAlias
        {
            Id = 1,
            PersonId = 1,
            Name = "Primary Alias",
            CreatedDateTime = DateTime.UtcNow
        };

        var alias2 = new PersonAlias
        {
            Id = 2,
            PersonId = 2,
            Name = "Primary Alias",
            CreatedDateTime = DateTime.UtcNow
        };

        _context.PersonAliases.AddRange(alias1, alias2);

        // Add phone numbers
        var phone1 = new PhoneNumber
        {
            Id = 1,
            PersonId = 1,
            Number = "(555) 123-4567",
            CountryCode = "1",
            CreatedDateTime = DateTime.UtcNow
        };

        _context.PhoneNumbers.Add(phone1);

        // Add attendance occurrence for today
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var occurrence = new AttendanceOccurrence
        {
            Id = 1,
            GroupId = 1,
            OccurrenceDate = today,
            SundayDate = today,
            CreatedDateTime = DateTime.UtcNow
        };

        _context.AttendanceOccurrences.Add(occurrence);

        // Add first-time attendance
        var attendance1 = new Attendance
        {
            Id = 1,
            OccurrenceId = 1,
            PersonAliasId = 1,
            StartDateTime = DateTime.UtcNow,
            IsFirstTime = true,
            DidAttend = true,
            CreatedDateTime = DateTime.UtcNow
        };

        var attendance2 = new Attendance
        {
            Id = 2,
            OccurrenceId = 1,
            PersonAliasId = 2,
            StartDateTime = DateTime.UtcNow,
            IsFirstTime = true,
            DidAttend = true,
            CreatedDateTime = DateTime.UtcNow
        };

        _context.Attendances.AddRange(attendance1, attendance2);

        _context.SaveChanges();
    }

    [Fact]
    public async Task GetTodaysFirstTimersAsync_ShouldReturnTodaysFirstTimeVisitors()
    {
        // Act
        var result = await _service.GetTodaysFirstTimersAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().Contain(v => v.PersonName == "John Doe");
        result.Should().Contain(v => v.PersonName == "Jane Smith");
    }

    [Fact]
    public async Task GetTodaysFirstTimersAsync_ShouldIncludeContactInformation()
    {
        // Act
        var result = await _service.GetTodaysFirstTimersAsync();

        // Assert
        var johnDoe = result.First(v => v.PersonName == "John Doe");
        johnDoe.Email.Should().Be("john.doe@example.com");
        johnDoe.PhoneNumber.Should().Be("(555) 123-4567");
    }

    [Fact]
    public async Task GetTodaysFirstTimersAsync_ShouldIncludeGroupAndCampusInfo()
    {
        // Act
        var result = await _service.GetTodaysFirstTimersAsync();

        // Assert
        var visitor = result.First();
        visitor.GroupName.Should().Be("Kindergarten");
        visitor.GroupTypeName.Should().Be("Children's Ministry");
        visitor.CampusName.Should().Be("Main Campus");
    }

    [Fact]
    public async Task GetFirstTimersByDateRangeAsync_ShouldReturnVisitorsInRange()
    {
        // Arrange
        var startDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1));
        var endDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));

        // Act
        var result = await _service.GetFirstTimersByDateRangeAsync(startDate, endDate);

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetFirstTimersByDateRangeAsync_ShouldReturnEmptyForFutureRange()
    {
        // Arrange
        var startDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10));
        var endDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(20));

        // Act
        var result = await _service.GetFirstTimersByDateRangeAsync(startDate, endDate);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task IsFirstTimeForGroupTypeAsync_ShouldReturnTrue_WhenNoPreviousAttendance()
    {
        // Arrange - Add new person with no attendance
        var newPerson = new Person
        {
            Id = 99,
            FirstName = "New",
            LastName = "Visitor",
            CreatedDateTime = DateTime.UtcNow
        };
        _context.People.Add(newPerson);

        var newAlias = new PersonAlias
        {
            Id = 99,
            PersonId = 99,
            Name = "Primary Alias",
            CreatedDateTime = DateTime.UtcNow
        };
        _context.PersonAliases.Add(newAlias);

        await _context.SaveChangesAsync();

        // Act
        var result = await _service.IsFirstTimeForGroupTypeAsync(99, 1);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsFirstTimeForGroupTypeAsync_ShouldReturnFalse_WhenHasPreviousAttendance()
    {
        // Act - Person 1 already has attendance at group type 1
        var result = await _service.IsFirstTimeForGroupTypeAsync(1, 1);

        // Assert
        result.Should().BeFalse();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
