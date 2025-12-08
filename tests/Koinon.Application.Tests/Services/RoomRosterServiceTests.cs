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

public class RoomRosterServiceTests : IDisposable
{
    private readonly KoinonDbContext _context;
    private readonly IRoomRosterService _service;
    private readonly Mock<ILogger<RoomRosterService>> _loggerMock;
    private readonly Mock<IUserContext> _userContextMock;
    private readonly Mock<IGradeCalculationService> _gradeServiceMock;
    private readonly Mock<ICheckinAttendanceService> _attendanceServiceMock;

    public RoomRosterServiceTests()
    {
        var options = new DbContextOptionsBuilder<KoinonDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new KoinonDbContext(options);
        _loggerMock = new Mock<ILogger<RoomRosterService>>();
        _userContextMock = new Mock<IUserContext>();
        _gradeServiceMock = new Mock<IGradeCalculationService>();
        _attendanceServiceMock = new Mock<ICheckinAttendanceService>();

        // Setup default user context behavior for tests
        _userContextMock.Setup(x => x.IsAuthenticated).Returns(true);
        _userContextMock.Setup(x => x.CanAccessPerson(It.IsAny<int>())).Returns(true);
        _userContextMock.Setup(x => x.CanAccessLocation(It.IsAny<int>())).Returns(true);

        _service = new RoomRosterService(
            _context,
            _userContextMock.Object,
            _loggerMock.Object,
            _gradeServiceMock.Object,
            _attendanceServiceMock.Object);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task GetRoomRosterAsync_ValidLocation_ReturnsRoster()
    {
        // Arrange
        var location = await SetupLocationAsync();
        var (child1, child2) = await SetupChildrenAsync();
        var attendance1 = await SetupAttendanceAsync(child1, location);
        var attendance2 = await SetupAttendanceAsync(child2, location);

        _gradeServiceMock.Setup(x => x.CalculateGrade(It.IsAny<int?>(), It.IsAny<DateOnly?>()))
            .Returns(5); // 5th grade

        // Act
        var result = await _service.GetRoomRosterAsync(location.IdKey);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(location.IdKey, result.LocationIdKey);
        Assert.Equal(location.Name, result.LocationName);
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Children.Count);

        var rosterChild1 = result.Children.FirstOrDefault(c => c.PersonIdKey == child1.IdKey);
        Assert.NotNull(rosterChild1);
        Assert.Equal(child1.FullName, rosterChild1.FullName);
        Assert.Equal(child1.Allergies, rosterChild1.Allergies);
        Assert.Equal(child1.HasCriticalAllergies, rosterChild1.HasCriticalAllergies);
    }

    [Fact]
    public async Task GetRoomRosterAsync_WithCapacity_CalculatesCapacityMetrics()
    {
        // Arrange
        var location = await SetupLocationAsync(capacity: 10);
        var children = new List<Person>();

        // Add 8 children (80% capacity - near capacity)
        for (int i = 0; i < 8; i++)
        {
            var child = await SetupChildAsync($"Child{i}");
            children.Add(child);
            await SetupAttendanceAsync(child, location);
        }

        // Act
        var result = await _service.GetRoomRosterAsync(location.IdKey);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(8, result.TotalCount);
        Assert.Equal(10, result.Capacity);
        Assert.False(result.IsAtCapacity);
        Assert.True(result.IsNearCapacity); // 80% >= 80% threshold
    }

    [Fact]
    public async Task GetRoomRosterAsync_AtCapacity_SetsCapacityFlag()
    {
        // Arrange
        var location = await SetupLocationAsync(capacity: 5);

        // Add 5 children (100% capacity)
        for (int i = 0; i < 5; i++)
        {
            var child = await SetupChildAsync($"Child{i}");
            await SetupAttendanceAsync(child, location);
        }

        // Act
        var result = await _service.GetRoomRosterAsync(location.IdKey);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(5, result.TotalCount);
        Assert.True(result.IsAtCapacity);
        Assert.True(result.IsNearCapacity);
    }

    [Fact]
    public async Task GetRoomRosterAsync_OnlyCurrentlyCheckedIn_ExcludesCheckedOut()
    {
        // Arrange
        var location = await SetupLocationAsync();
        var child1 = await SetupChildAsync("Present Child");
        var child2 = await SetupChildAsync("Checked Out Child");

        var attendance1 = await SetupAttendanceAsync(child1, location);
        var attendance2 = await SetupAttendanceAsync(child2, location);

        // Check out child2
        attendance2.EndDateTime = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetRoomRosterAsync(location.IdKey);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.TotalCount);
        Assert.Single(result.Children);
        Assert.Equal(child1.IdKey, result.Children[0].PersonIdKey);
    }

    [Fact]
    public async Task GetRoomRosterAsync_WithAllergies_IncludesAllergyInfo()
    {
        // Arrange
        var location = await SetupLocationAsync();
        var child = await SetupChildAsync("Allergy Child");
        child.Allergies = "Peanuts, Tree Nuts";
        child.HasCriticalAllergies = true;
        await _context.SaveChangesAsync();

        await SetupAttendanceAsync(child, location);

        // Act
        var result = await _service.GetRoomRosterAsync(location.IdKey);

        // Assert
        Assert.NotNull(result);
        var rosterChild = result.Children.FirstOrDefault();
        Assert.NotNull(rosterChild);
        Assert.Equal("Peanuts, Tree Nuts", rosterChild.Allergies);
        Assert.True(rosterChild.HasCriticalAllergies);
    }

    [Fact]
    public async Task GetRoomRosterAsync_WithSpecialNeeds_IncludesSpecialNeeds()
    {
        // Arrange
        var location = await SetupLocationAsync();
        var child = await SetupChildAsync("Special Needs Child");
        child.SpecialNeeds = "Requires extra supervision";
        await _context.SaveChangesAsync();

        await SetupAttendanceAsync(child, location);

        // Act
        var result = await _service.GetRoomRosterAsync(location.IdKey);

        // Assert
        Assert.NotNull(result);
        var rosterChild = result.Children.FirstOrDefault();
        Assert.NotNull(rosterChild);
        Assert.Equal("Requires extra supervision", rosterChild.SpecialNeeds);
    }

    [Fact]
    public async Task GetRoomRosterAsync_UnauthorizedUser_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var location = await SetupLocationAsync();
        _userContextMock.Setup(x => x.CanAccessLocation(It.IsAny<int>())).Returns(false);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _service.GetRoomRosterAsync(location.IdKey));
    }

    [Fact]
    public async Task CheckOutFromRosterAsync_DelegatesToAttendanceService()
    {
        // Arrange
        var attendanceIdKey = "test-idkey";
        _attendanceServiceMock.Setup(x => x.CheckOutAsync(attendanceIdKey, default))
            .ReturnsAsync(true);

        // Act
        var result = await _service.CheckOutFromRosterAsync(attendanceIdKey);

        // Assert
        Assert.True(result);
        _attendanceServiceMock.Verify(x => x.CheckOutAsync(attendanceIdKey, default), Times.Once);
    }

    // Helper methods

    private async Task<Group> SetupLocationAsync(int? capacity = null)
    {
        var groupType = new GroupType
        {
            Name = "Check-in Area",
            IsSystem = false
        };
        _context.GroupTypes.Add(groupType);
        await _context.SaveChangesAsync();

        var location = new Group
        {
            Name = "Preschool Room",
            GroupTypeId = groupType.Id,
            IsActive = true,
            IsPublic = true,
            GroupCapacity = capacity
        };
        _context.Groups.Add(location);
        await _context.SaveChangesAsync();

        return location;
    }

    private async Task<Person> SetupChildAsync(string lastName)
    {
        var person = new Person
        {
            FirstName = "Test",
            LastName = lastName,
            BirthYear = 2018,
            BirthMonth = 5,
            BirthDay = 15,
            GraduationYear = 2036
        };
        _context.People.Add(person);
        await _context.SaveChangesAsync();

        var personAlias = new PersonAlias
        {
            PersonId = person.Id,
            AliasPersonId = null
        };
        _context.PersonAliases.Add(personAlias);
        await _context.SaveChangesAsync();

        return person;
    }

    private async Task<(Person child1, Person child2)> SetupChildrenAsync()
    {
        var child1 = await SetupChildAsync("Smith");
        var child2 = await SetupChildAsync("Johnson");
        return (child1, child2);
    }

    private async Task<Attendance> SetupAttendanceAsync(Person person, Group location)
    {
        var personAlias = await _context.PersonAliases
            .FirstOrDefaultAsync(pa => pa.PersonId == person.Id);

        var occurrenceDate = DateOnly.FromDateTime(DateTime.UtcNow);
        var sundayDate = GetSundayDate(occurrenceDate);

        var occurrence = new AttendanceOccurrence
        {
            GroupId = location.Id,
            OccurrenceDate = occurrenceDate,
            SundayDate = sundayDate,
            DidNotOccur = false
        };
        _context.AttendanceOccurrences.Add(occurrence);
        await _context.SaveChangesAsync();

        var attendance = new Attendance
        {
            OccurrenceId = occurrence.Id,
            PersonAliasId = personAlias!.Id,
            StartDateTime = DateTime.UtcNow,
            DidAttend = true,
            IsFirstTime = false
        };
        _context.Attendances.Add(attendance);
        await _context.SaveChangesAsync();

        return attendance;
    }

    private static DateOnly GetSundayDate(DateOnly date)
    {
        var dayOfWeek = (int)date.DayOfWeek;
        var daysToSunday = -dayOfWeek; // Sunday is 0, so we go backwards
        return date.AddDays(daysToSunday);
    }
}
