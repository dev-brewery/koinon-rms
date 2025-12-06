using Koinon.Application.DTOs;
using Koinon.Application.Interfaces;
using Koinon.Application.Services;
using Koinon.Application.Services.Common;
using Koinon.Domain.Data;
using Koinon.Domain.Entities;
using Koinon.Domain.Enums;
using Koinon.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Koinon.Application.Tests.Services;

public class CheckinAttendanceServiceTests : IDisposable
{
    private readonly KoinonDbContext _context;
    private readonly ICheckinAttendanceService _service;
    private readonly Mock<ILogger<CheckinAttendanceService>> _loggerMock;
    private readonly Mock<ILogger<ConcurrentOperationHelper>> _concurrencyLoggerMock;
    private readonly Mock<IUserContext> _userContextMock;

    public CheckinAttendanceServiceTests()
    {
        var options = new DbContextOptionsBuilder<KoinonDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new KoinonDbContext(options);
        _loggerMock = new Mock<ILogger<CheckinAttendanceService>>();
        _concurrencyLoggerMock = new Mock<ILogger<ConcurrentOperationHelper>>();
        _userContextMock = new Mock<IUserContext>();

        // Setup default user context behavior for tests
        _userContextMock.Setup(x => x.IsAuthenticated).Returns(true);
        _userContextMock.Setup(x => x.CanAccessPerson(It.IsAny<int>())).Returns(true);
        _userContextMock.Setup(x => x.CanAccessLocation(It.IsAny<int>())).Returns(true);

        var concurrencyHelper = new ConcurrentOperationHelper(_context, _concurrencyLoggerMock.Object);
        _service = new CheckinAttendanceService(_context, _userContextMock.Object, _loggerMock.Object, concurrencyHelper);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task CheckInAsync_ValidRequest_CreatesAttendance()
    {
        // Arrange
        var (person, location, schedule) = await SetupTestDataAsync();

        var request = new CheckinRequestDto
        {
            PersonIdKey = person.IdKey,
            LocationIdKey = location.IdKey,
            ScheduleIdKey = schedule.IdKey,
            GenerateSecurityCode = false
        };

        // Act
        var result = await _service.CheckInAsync(request);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.AttendanceIdKey);
        Assert.NotNull(result.CheckInTime);
        Assert.NotNull(result.Person);
        Assert.Equal(person.FullName, result.Person.FullName);
        Assert.NotNull(result.Location);
        Assert.Equal(location.Name, result.Location.Name);

        // Verify attendance was created in database
        var attendanceId = IdKeyHelper.Decode(result.AttendanceIdKey!);
        var attendance = await _context.Attendances
            .FirstOrDefaultAsync(a => a.Id == attendanceId);
        Assert.NotNull(attendance);
        Assert.Null(attendance.EndDateTime);
        Assert.True(attendance.DidAttend);
    }

    [Fact]
    public async Task CheckInAsync_WithSecurityCode_GeneratesCode()
    {
        // Arrange
        var (person, location, schedule) = await SetupTestDataAsync();

        var request = new CheckinRequestDto
        {
            PersonIdKey = person.IdKey,
            LocationIdKey = location.IdKey,
            ScheduleIdKey = schedule.IdKey,
            GenerateSecurityCode = true
        };

        // Act
        var result = await _service.CheckInAsync(request);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.SecurityCode);
        Assert.NotEmpty(result.SecurityCode);
        Assert.Equal(4, result.SecurityCode.Length);

        // Verify code was saved to database
        var code = await _context.AttendanceCodes
            .FirstOrDefaultAsync(ac => ac.Code == result.SecurityCode);
        Assert.NotNull(code);
        Assert.Equal(DateOnly.FromDateTime(DateTime.UtcNow), code.IssueDate);
    }

    [Fact]
    public async Task CheckInAsync_DuplicateCheckIn_ReturnsFalse()
    {
        // Arrange
        var (person, location, schedule) = await SetupTestDataAsync();

        var request = new CheckinRequestDto
        {
            PersonIdKey = person.IdKey,
            LocationIdKey = location.IdKey,
            ScheduleIdKey = schedule.IdKey,
            GenerateSecurityCode = false
        };

        // First check-in
        await _service.CheckInAsync(request);

        // Act - attempt second check-in
        var result = await _service.CheckInAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("already checked in", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CheckInAsync_InvalidPersonId_ReturnsFalse()
    {
        // Arrange
        var (_, location, schedule) = await SetupTestDataAsync();

        var request = new CheckinRequestDto
        {
            PersonIdKey = "invalid-id",
            LocationIdKey = location.IdKey,
            ScheduleIdKey = schedule.IdKey,
            GenerateSecurityCode = false
        };

        // Act
        var result = await _service.CheckInAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Invalid person ID", result.ErrorMessage);
    }

    [Fact]
    public async Task CheckInAsync_DeceasedPerson_ReturnsFalse()
    {
        // Arrange
        var (person, location, schedule) = await SetupTestDataAsync();
        person.IsDeceased = true;
        await _context.SaveChangesAsync();

        var request = new CheckinRequestDto
        {
            PersonIdKey = person.IdKey,
            LocationIdKey = location.IdKey,
            ScheduleIdKey = schedule.IdKey,
            GenerateSecurityCode = false
        };

        // Act
        var result = await _service.CheckInAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("deceased", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CheckInAsync_InactiveLocation_ReturnsFalse()
    {
        // Arrange
        var (person, location, schedule) = await SetupTestDataAsync();
        location.IsActive = false;
        await _context.SaveChangesAsync();

        var request = new CheckinRequestDto
        {
            PersonIdKey = person.IdKey,
            LocationIdKey = location.IdKey,
            ScheduleIdKey = schedule.IdKey,
            GenerateSecurityCode = false
        };

        // Act
        var result = await _service.CheckInAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("not active", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CheckInAsync_LocationAtCapacity_ReturnsFalse()
    {
        // Arrange
        var (person, location, schedule) = await SetupTestDataAsync();
        location.GroupCapacity = 1;
        await _context.SaveChangesAsync();

        // Create another person and check them in
        var otherPerson = await CreateTestPersonAsync("Other", "Person");
        var otherRequest = new CheckinRequestDto
        {
            PersonIdKey = otherPerson.IdKey,
            LocationIdKey = location.IdKey,
            ScheduleIdKey = schedule.IdKey,
            GenerateSecurityCode = false
        };
        await _service.CheckInAsync(otherRequest);

        // Act - try to check in when at capacity
        var request = new CheckinRequestDto
        {
            PersonIdKey = person.IdKey,
            LocationIdKey = location.IdKey,
            ScheduleIdKey = schedule.IdKey,
            GenerateSecurityCode = false
        };
        var result = await _service.CheckInAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("capacity", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task BatchCheckInAsync_MultipleValid_AllSucceed()
    {
        // Arrange
        var (person1, location, schedule) = await SetupTestDataAsync();
        var person2 = await CreateTestPersonAsync("Jane", "Doe");

        var request = new BatchCheckinRequestDto(
            CheckIns: new List<CheckinRequestDto>
            {
                new()
                {
                    PersonIdKey = person1.IdKey,
                    LocationIdKey = location.IdKey,
                    ScheduleIdKey = schedule.IdKey,
                    GenerateSecurityCode = true
                },
                new()
                {
                    PersonIdKey = person2.IdKey,
                    LocationIdKey = location.IdKey,
                    ScheduleIdKey = schedule.IdKey,
                    GenerateSecurityCode = true
                }
            });

        // Act
        var result = await _service.BatchCheckInAsync(request);

        // Assert
        Assert.Equal(2, result.Results.Count);
        Assert.Equal(2, result.SuccessCount);
        Assert.Equal(0, result.FailureCount);
        Assert.True(result.AllSucceeded);
        Assert.All(result.Results, r => Assert.True(r.Success));
        Assert.All(result.Results, r => Assert.NotNull(r.SecurityCode));
    }

    [Fact]
    public async Task BatchCheckInAsync_SomeInvalid_ReturnsPartialSuccess()
    {
        // Arrange
        var (person1, location, schedule) = await SetupTestDataAsync();

        var request = new BatchCheckinRequestDto(
            new List<CheckinRequestDto>
            {
                new()
                {
                    PersonIdKey = person1.IdKey,
                    LocationIdKey = location.IdKey,
                    ScheduleIdKey = schedule.IdKey,
                    GenerateSecurityCode = false
                },
                new()
                {
                    PersonIdKey = "invalid-id",
                    LocationIdKey = location.IdKey,
                    ScheduleIdKey = schedule.IdKey,
                    GenerateSecurityCode = false
                }
            });

        // Act
        var result = await _service.BatchCheckInAsync(request);

        // Assert
        Assert.Equal(2, result.Results.Count);
        Assert.Equal(1, result.SuccessCount);
        Assert.Equal(1, result.FailureCount);
        Assert.False(result.AllSucceeded);
    }

    [Fact]
    public async Task CheckOutAsync_ValidAttendance_UpdatesEndDateTime()
    {
        // Arrange
        var (person, location, schedule) = await SetupTestDataAsync();
        var checkinRequest = new CheckinRequestDto
        {
            PersonIdKey = person.IdKey,
            LocationIdKey = location.IdKey,
            ScheduleIdKey = schedule.IdKey,
            GenerateSecurityCode = false
        };
        var checkinResult = await _service.CheckInAsync(checkinRequest);

        // Act
        var result = await _service.CheckOutAsync(checkinResult.AttendanceIdKey!);

        // Assert
        Assert.True(result);

        var attendanceId = IdKeyHelper.Decode(checkinResult.AttendanceIdKey!);
        var attendance = await _context.Attendances
            .FirstOrDefaultAsync(a => a.Id == attendanceId);
        Assert.NotNull(attendance);
        Assert.NotNull(attendance.EndDateTime);
    }

    [Fact]
    public async Task CheckOutAsync_InvalidId_ReturnsFalse()
    {
        // Act
        var result = await _service.CheckOutAsync("invalid-id");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task CheckOutAsync_AlreadyCheckedOut_ReturnsFalse()
    {
        // Arrange
        var (person, location, schedule) = await SetupTestDataAsync();
        var checkinRequest = new CheckinRequestDto
        {
            PersonIdKey = person.IdKey,
            LocationIdKey = location.IdKey,
            ScheduleIdKey = schedule.IdKey,
            GenerateSecurityCode = false
        };
        var checkinResult = await _service.CheckInAsync(checkinRequest);
        await _service.CheckOutAsync(checkinResult.AttendanceIdKey!);

        // Act - try to check out again
        var result = await _service.CheckOutAsync(checkinResult.AttendanceIdKey!);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task GetCurrentAttendanceAsync_ReturnsOnlyActiveAttendance()
    {
        // Arrange
        var (person1, location, schedule) = await SetupTestDataAsync();
        var person2 = await CreateTestPersonAsync("Jane", "Doe");

        // Check in two people
        var request1 = new CheckinRequestDto
        {
            PersonIdKey = person1.IdKey,
            LocationIdKey = location.IdKey,
            ScheduleIdKey = schedule.IdKey,
            GenerateSecurityCode = false
        };
        var request2 = new CheckinRequestDto
        {
            PersonIdKey = person2.IdKey,
            LocationIdKey = location.IdKey,
            ScheduleIdKey = schedule.IdKey,
            GenerateSecurityCode = false
        };

        var result1 = await _service.CheckInAsync(request1);
        var result2 = await _service.CheckInAsync(request2);

        // Check out person1
        await _service.CheckOutAsync(result1.AttendanceIdKey!);

        // Act
        var currentAttendance = await _service.GetCurrentAttendanceAsync(location.IdKey);

        // Assert
        Assert.Single(currentAttendance);
        Assert.Equal(person2.FullName, currentAttendance[0].Person.FullName);
    }

    [Fact]
    public async Task GetPersonAttendanceHistoryAsync_ReturnsOrderedHistory()
    {
        // Arrange
        var (person, location, schedule) = await SetupTestDataAsync();

        // Create multiple check-ins
        for (int i = 0; i < 3; i++)
        {
            var request = new CheckinRequestDto
            {
                PersonIdKey = person.IdKey,
                LocationIdKey = location.IdKey,
                ScheduleIdKey = schedule.IdKey,
                GenerateSecurityCode = false
            };
            var result = await _service.CheckInAsync(request);
            await _service.CheckOutAsync(result.AttendanceIdKey!);

            // Small delay to ensure different timestamps
            await Task.Delay(10);
        }

        // Act
        var history = await _service.GetPersonAttendanceHistoryAsync(person.IdKey);

        // Assert
        Assert.Equal(3, history.Count);

        // Verify ordered by most recent first
        for (int i = 0; i < history.Count - 1; i++)
        {
            Assert.True(history[i].StartDateTime >= history[i + 1].StartDateTime);
        }
    }

    [Fact]
    public async Task GetPersonAttendanceHistoryAsync_FiltersByDays()
    {
        // Arrange
        var (person, location, schedule) = await SetupTestDataAsync();

        var request = new CheckinRequestDto
        {
            PersonIdKey = person.IdKey,
            LocationIdKey = location.IdKey,
            ScheduleIdKey = schedule.IdKey,
            GenerateSecurityCode = false
        };
        await _service.CheckInAsync(request);

        // Act - request 0 days (should return empty)
        var history = await _service.GetPersonAttendanceHistoryAsync(person.IdKey, days: 0);

        // Assert
        Assert.Empty(history);
    }

    [Fact]
    public async Task ValidateCheckinAsync_ValidRequest_ReturnsAllowed()
    {
        // Arrange
        var (person, location, _) = await SetupTestDataAsync();

        // Act
        var result = await _service.ValidateCheckinAsync(person.IdKey, location.IdKey);

        // Assert
        Assert.True(result.IsAllowed);
        Assert.Null(result.Reason);
        Assert.False(result.IsAlreadyCheckedIn);
        Assert.False(result.IsAtCapacity);
    }

    [Fact]
    public async Task ValidateCheckinAsync_AlreadyCheckedIn_ReturnsNotAllowed()
    {
        // Arrange
        var (person, location, schedule) = await SetupTestDataAsync();
        var request = new CheckinRequestDto
        {
            PersonIdKey = person.IdKey,
            LocationIdKey = location.IdKey,
            ScheduleIdKey = schedule.IdKey,
            GenerateSecurityCode = false
        };
        await _service.CheckInAsync(request);

        // Act
        var result = await _service.ValidateCheckinAsync(person.IdKey, location.IdKey);

        // Assert
        Assert.False(result.IsAllowed);
        Assert.True(result.IsAlreadyCheckedIn);
    }

    [Fact]
    public async Task ValidateCheckinAsync_AtCapacity_ReturnsNotAllowed()
    {
        // Arrange
        var (person, location, schedule) = await SetupTestDataAsync();
        location.GroupCapacity = 0; // Set to 0 to immediately trigger capacity
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.ValidateCheckinAsync(person.IdKey, location.IdKey);

        // Assert
        Assert.False(result.IsAllowed);
        Assert.True(result.IsAtCapacity);
    }

    // Helper methods

    private async Task<(Person person, Group location, Schedule schedule)> SetupTestDataAsync()
    {
        var person = await CreateTestPersonAsync("John", "Doe");
        var groupType = await CreateTestGroupTypeAsync();
        var location = await CreateTestLocationAsync(groupType);
        var schedule = await CreateTestScheduleAsync();

        return (person, location, schedule);
    }

    private async Task<Person> CreateTestPersonAsync(string firstName, string lastName)
    {
        var person = new Person
        {
            FirstName = firstName,
            LastName = lastName,
            Gender = Gender.Unknown,
            CreatedDateTime = DateTime.UtcNow
        };
        _context.People.Add(person);
        await _context.SaveChangesAsync();

        // Create primary PersonAlias
        var personAlias = new PersonAlias
        {
            PersonId = person.Id,
            Name = person.FullName
        };
        _context.PersonAliases.Add(personAlias);
        await _context.SaveChangesAsync();

        return person;
    }

    private async Task<GroupType> CreateTestGroupTypeAsync()
    {
        var groupType = new GroupType
        {
            Name = "Check-in Area",
            TakesAttendance = true,
            CreatedDateTime = DateTime.UtcNow
        };
        _context.GroupTypes.Add(groupType);
        await _context.SaveChangesAsync();

        return groupType;
    }

    private async Task<Group> CreateTestLocationAsync(GroupType groupType)
    {
        var location = new Group
        {
            Name = "Test Room",
            GroupTypeId = groupType.Id,
            IsActive = true,
            CreatedDateTime = DateTime.UtcNow
        };
        _context.Groups.Add(location);
        await _context.SaveChangesAsync();

        return location;
    }

    private async Task<Schedule> CreateTestScheduleAsync()
    {
        var schedule = new Schedule
        {
            Name = "Sunday Service",
            WeeklyDayOfWeek = DayOfWeek.Sunday,
            WeeklyTimeOfDay = new TimeSpan(10, 0, 0),
            CheckInStartOffsetMinutes = 60,
            CheckInEndOffsetMinutes = 30,
            IsActive = true,
            CreatedDateTime = DateTime.UtcNow
        };
        _context.Schedules.Add(schedule);
        await _context.SaveChangesAsync();

        return schedule;
    }
}
