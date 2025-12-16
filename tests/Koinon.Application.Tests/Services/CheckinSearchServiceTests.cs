using FluentAssertions;
using Koinon.Application.Services;
using Koinon.Application.Services.Common;
using Koinon.Application.Tests.Fakes;
using Koinon.Domain.Data;
using Koinon.Domain.Entities;
using Koinon.Domain.Enums;
using Koinon.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Koinon.Application.Tests.Services;

public class CheckinSearchServiceTests : IDisposable
{
    private readonly KoinonDbContext _context;
    private readonly CheckinSearchService _sut;
    private readonly Mock<ILogger<CheckinSearchService>> _mockLogger;
    private readonly Mock<ILogger<CheckinDataLoader>> _mockDataLoaderLogger;
    private readonly FakeUserContext _userContext;
    private readonly CheckinDataLoader _dataLoader;

    public CheckinSearchServiceTests()
    {
        // Setup in-memory database
        var options = new DbContextOptionsBuilder<KoinonDbContext>()
            .UseInMemoryDatabase(databaseName: $"KoinonTestDb_{Guid.NewGuid()}")
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new KoinonDbContext(options);
        _context.Database.EnsureCreated();

        _mockLogger = new Mock<ILogger<CheckinSearchService>>();
        _mockDataLoaderLogger = new Mock<ILogger<CheckinDataLoader>>();
        _userContext = new FakeUserContext();
        _dataLoader = new CheckinDataLoader(_context, _mockDataLoaderLogger.Object);
        _sut = new CheckinSearchService(_context, _userContext, _mockLogger.Object, _dataLoader);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Fact]
    public async Task SearchByPhoneAsync_WithLast4Digits_ReturnsMatchingFamilies()
    {
        // Arrange
        var (family, person, _) = await CreateTestFamilyAsync();

        var phone = new PhoneNumber
        {
            PersonId = person.Id,
            Number = "5551234567",
            NumberNormalized = "5551234567",
            CountryCode = "1"
        };
        await _context.PhoneNumbers.AddAsync(phone);
        await _context.SaveChangesAsync();

        // Act
        var results = await _sut.SearchByPhoneAsync("4567");

        // Assert
        results.Should().NotBeEmpty();
        results.Should().Contain(r => r.FamilyIdKey == family.IdKey);
        results.First().Members.Should().NotBeEmpty();
    }

    [Fact]
    public async Task SearchByPhoneAsync_WithFullNumber_ReturnsMatchingFamilies()
    {
        // Arrange
        var (family, _, _) = await CreateTestFamilyAsync();
        var person = family.Members.First().Person!;

        var phone = new PhoneNumber
        {
            PersonId = person.Id,
            Number = "5551234567",
            NumberNormalized = "5551234567",
            CountryCode = "1"
        };
        await _context.PhoneNumbers.AddAsync(phone);
        await _context.SaveChangesAsync();

        // Act
        var results = await _sut.SearchByPhoneAsync("5551234567");

        // Assert
        results.Should().NotBeEmpty();
        results.Should().Contain(r => r.FamilyIdKey == family.IdKey);
    }

    [Fact]
    public async Task SearchByPhoneAsync_WithFormattedNumber_ReturnsMatchingFamilies()
    {
        // Arrange
        var (family, _, _) = await CreateTestFamilyAsync();
        var person = family.Members.First().Person!;

        var phone = new PhoneNumber
        {
            PersonId = person.Id,
            Number = "555-123-4567",
            NumberNormalized = "5551234567",
            CountryCode = "1"
        };
        await _context.PhoneNumbers.AddAsync(phone);
        await _context.SaveChangesAsync();

        // Act
        var results = await _sut.SearchByPhoneAsync("4567");

        // Assert
        results.Should().NotBeEmpty();
        results.Should().Contain(r => r.FamilyIdKey == family.IdKey);
    }

    [Fact]
    public async Task SearchByPhoneAsync_WithNoMatches_ReturnsEmptyList()
    {
        // Arrange
        await CreateTestFamilyAsync();

        // Act
        var results = await _sut.SearchByPhoneAsync("9999");

        // Assert
        results.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchByPhoneAsync_WithTooFewDigits_ReturnsEmptyList()
    {
        // Arrange
        await CreateTestFamilyAsync();

        // Act
        var results = await _sut.SearchByPhoneAsync("123");

        // Assert
        results.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchByNameAsync_WithFirstName_ReturnsMatchingFamilies()
    {
        // Arrange
        var (family, _, _) = await CreateTestFamilyAsync("TestFamily", "John", "Doe");

        // Act
        var results = await _sut.SearchByNameAsync("John");

        // Assert
        results.Should().NotBeEmpty();
        results.Should().Contain(r => r.FamilyIdKey == family.IdKey);
        results.First().Members.Should().Contain(m => m.FirstName == "John");
    }

    [Fact]
    public async Task SearchByNameAsync_WithLastName_ReturnsMatchingFamilies()
    {
        // Arrange
        var (family, _, _) = await CreateTestFamilyAsync("TestFamily", "John", "Doe");

        // Act
        var results = await _sut.SearchByNameAsync("Doe");

        // Assert
        results.Should().NotBeEmpty();
        results.Should().Contain(r => r.FamilyIdKey == family.IdKey);
    }

    [Fact]
    public async Task SearchByNameAsync_WithNickName_ReturnsMatchingFamilies()
    {
        // Arrange
        var (family, person, _) = await CreateTestFamilyAsync();
        person.NickName = "Johnny";
        await _context.SaveChangesAsync();

        // Act
        var results = await _sut.SearchByNameAsync("Johnny");

        // Assert
        results.Should().NotBeEmpty();
        results.Should().Contain(r => r.FamilyIdKey == family.IdKey);
    }

    [Fact]
    public async Task SearchByNameAsync_WithPartialName_ReturnsMatchingFamilies()
    {
        // Arrange
        var (family, _, _) = await CreateTestFamilyAsync("TestFamily", "Jonathan", "Smith");

        // Act
        var results = await _sut.SearchByNameAsync("Jon");

        // Assert
        results.Should().NotBeEmpty();
        results.Should().Contain(r => r.FamilyIdKey == family.IdKey);
    }

    [Fact]
    public async Task SearchByNameAsync_IsCaseInsensitive()
    {
        // Arrange
        var (family, _, _) = await CreateTestFamilyAsync("TestFamily", "John", "Doe");

        // Act
        var results = await _sut.SearchByNameAsync("JOHN");

        // Assert
        results.Should().NotBeEmpty();
        results.Should().Contain(r => r.FamilyIdKey == family.IdKey);
    }

    [Fact]
    public async Task SearchByNameAsync_WithNoMatches_ReturnsEmptyList()
    {
        // Arrange
        await CreateTestFamilyAsync();

        // Act
        var results = await _sut.SearchByNameAsync("NonExistent");

        // Assert
        results.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchByNameAsync_ExcludesDeceasedPersons()
    {
        // Arrange
        var (family, person, _) = await CreateTestFamilyAsync("TestFamily", "John", "Doe");
        person.IsDeceased = true;
        await _context.SaveChangesAsync();

        // Act
        var results = await _sut.SearchByNameAsync("John");

        // Assert
        results.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchByCodeAsync_WithValidCodeToday_ReturnsFamily()
    {
        // Arrange
        var (family, person, _) = await CreateTestFamilyAsync();
        var (attendanceCode, _) = await CreateTestAttendanceAsync(person!);

        // Act
        var result = await _sut.SearchByCodeAsync(attendanceCode.Code);

        // Assert
        result.Should().NotBeNull();
        result!.FamilyIdKey.Should().Be(family.IdKey);
        result.Members.Should().NotBeEmpty();
    }

    [Fact]
    public async Task SearchByCodeAsync_WithOldCode_ReturnsNull()
    {
        // Arrange
        var (_, person, _) = await CreateTestFamilyAsync();
        var attendanceCode = new AttendanceCode
        {
            Code = "ABC",
            IssueDateTime = DateTime.UtcNow.AddDays(-2) // 2 days ago
        };
        await _context.AttendanceCodes.AddAsync(attendanceCode);

        var occurrence = await CreateTestOccurrenceAsync();
        var personAlias = await _context.PersonAliases
            .FirstAsync(pa => pa.PersonId == person.Id);

        var attendance = new Attendance
        {
            OccurrenceId = occurrence.Id,
            PersonAliasId = personAlias.Id,
            AttendanceCodeId = attendanceCode.Id,
            StartDateTime = DateTime.UtcNow.AddDays(-2)
        };
        await _context.Attendances.AddAsync(attendance);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.SearchByCodeAsync("ABC");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task SearchByCodeAsync_WithInvalidCode_ReturnsNull()
    {
        // Arrange
        await CreateTestFamilyAsync();

        // Act
        var result = await _sut.SearchByCodeAsync("XYZ");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task SearchByCodeAsync_IsCaseInsensitive()
    {
        // Arrange
        var (family, person, _) = await CreateTestFamilyAsync();
        var (attendanceCode, _) = await CreateTestAttendanceAsync(person);

        // Act - search with lowercase
        var result = await _sut.SearchByCodeAsync(attendanceCode.Code.ToLowerInvariant());

        // Assert
        result.Should().NotBeNull();
        result!.FamilyIdKey.Should().Be(family.IdKey);
    }

    [Fact]
    public async Task SearchAsync_WithDigits_UsesPhoneSearch()
    {
        // Arrange
        var (family, _, _) = await CreateTestFamilyAsync();
        var person = family.Members.First().Person!;

        var phone = new PhoneNumber
        {
            PersonId = person.Id,
            Number = "5551234567",
            NumberNormalized = "5551234567",
            CountryCode = "1"
        };
        await _context.PhoneNumbers.AddAsync(phone);
        await _context.SaveChangesAsync();

        // Act
        var results = await _sut.SearchAsync("4567");

        // Assert
        results.Should().NotBeEmpty();
        results.Should().Contain(r => r.FamilyIdKey == family.IdKey);
    }

    [Fact]
    public async Task SearchAsync_WithShortAlphanumeric_TriesCodeSearch()
    {
        // Arrange
        var (family, person, _) = await CreateTestFamilyAsync();
        var (attendanceCode, _) = await CreateTestAttendanceAsync(person);

        // Act
        var results = await _sut.SearchAsync(attendanceCode.Code);

        // Assert
        results.Should().NotBeEmpty();
        results.Should().Contain(r => r.FamilyIdKey == family.IdKey);
    }

    [Fact]
    public async Task SearchAsync_WithName_UsesNameSearch()
    {
        // Arrange
        var (family, _, _) = await CreateTestFamilyAsync("TestFamily", "John", "Doe");

        // Act
        var results = await _sut.SearchAsync("John");

        // Assert
        results.Should().NotBeEmpty();
        results.Should().Contain(r => r.FamilyIdKey == family.IdKey);
    }

    [Fact]
    public async Task GetFamiliesWithMembersAsync_SortsMembersCorrectly()
    {
        // Arrange
        var (family, adultRole, childRole) = await CreateTestFamilyWithRolesAsync();

        // Add a child member
        var child = new Person
        {
            FirstName = "Child",
            LastName = "Doe",
            BirthYear = DateTime.UtcNow.Year - 8,
            BirthMonth = 1,
            BirthDay = 1
        };
        await _context.People.AddAsync(child);
        await _context.SaveChangesAsync();

        var childMember = new FamilyMember
        {
            FamilyId = family.Id,
            PersonId = child.Id,
            FamilyRoleId = childRole.Id,
            IsPrimary = false,
            DateAdded = DateTime.UtcNow
        };
        await _context.FamilyMembers.AddAsync(childMember);
        await _context.SaveChangesAsync();

        // Act
        var results = await _sut.SearchByNameAsync("Doe");

        // Assert
        results.Should().NotBeEmpty();
        var result = results.First();
        result.Members.Should().HaveCountGreaterThan(1);

        // Adults should be listed before children
        var firstMember = result.Members.First();
        var lastMember = result.Members.Last();

        firstMember.IsChild.Should().BeFalse();
        lastMember.IsChild.Should().BeTrue();
    }

    [Fact]
    public async Task GetFamiliesWithMembersAsync_IncludesCampusName()
    {
        // Arrange
        var campus = new Campus
        {
            Name = "Main Campus",
            IsActive = true
        };
        await _context.Campuses.AddAsync(campus);
        await _context.SaveChangesAsync();

        var (family, _, _) = await CreateTestFamilyAsync();
        family.CampusId = campus.Id;
        await _context.SaveChangesAsync();

        // Act
        var results = await _sut.SearchByNameAsync("Test");

        // Assert
        results.Should().NotBeEmpty();
        results.First().CampusName.Should().Be("Main Campus");
    }

    [Fact]
    public async Task SearchByPhoneAsync_WithEmptyString_ReturnsEmptyList()
    {
        // Act
        var results = await _sut.SearchByPhoneAsync("");

        // Assert
        results.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchByNameAsync_WithEmptyString_ReturnsEmptyList()
    {
        // Act
        var results = await _sut.SearchByNameAsync("");

        // Assert
        results.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchByCodeAsync_WithEmptyString_ReturnsNull()
    {
        // Act
        var result = await _sut.SearchByCodeAsync("");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task SearchByPhoneAsync_WhenNotAuthenticated_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        _userContext.IsAuthenticated = false;
        var (family, person, _) = await CreateTestFamilyAsync();

        var phone = new PhoneNumber
        {
            PersonId = person.Id,
            Number = "5551234567",
            NumberNormalized = "5551234567",
            CountryCode = "1"
        };
        await _context.PhoneNumbers.AddAsync(phone);
        await _context.SaveChangesAsync();

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            async () => await _sut.SearchByPhoneAsync("4567"));
    }

    [Fact]
    public async Task SearchByNameAsync_WhenNotAuthenticated_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        _userContext.IsAuthenticated = false;
        await CreateTestFamilyAsync("TestFamily", "John", "Doe");

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            async () => await _sut.SearchByNameAsync("John"));
    }

    [Fact]
    public async Task SearchByCodeAsync_WhenNotAuthenticated_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        _userContext.IsAuthenticated = false;
        var (_, person, _) = await CreateTestFamilyAsync();
        var (attendanceCode, _) = await CreateTestAttendanceAsync(person);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            async () => await _sut.SearchByCodeAsync(attendanceCode.Code));
    }

    // Helper methods

    private async Task<(Family family, Person person, FamilyMember member)> CreateTestFamilyAsync(
        string familyName = "Test Family",
        string firstName = "Test",
        string lastName = "Person")
    {
        // Get or create Family group type for roles
        var familyGroupType = await _context.GroupTypes
            .FirstOrDefaultAsync(gt => gt.Guid == SystemGuid.GroupType.Family);
        if (familyGroupType == null)
        {
            familyGroupType = new GroupType
            {
                Name = "Family",
                Guid = SystemGuid.GroupType.Family,
                GroupTerm = "Family",
                GroupMemberTerm = "Family Member",
                IsSystem = true
            };
            await _context.GroupTypes.AddAsync(familyGroupType);
            await _context.SaveChangesAsync();
        }

        // Get or create Adult role (used for family members)
        var adultRole = await _context.GroupTypeRoles
            .FirstOrDefaultAsync(r => r.Name == "Adult" && r.GroupTypeId == familyGroupType.Id);
        if (adultRole == null)
        {
            adultRole = new GroupTypeRole
            {
                GroupTypeId = familyGroupType.Id,
                Name = "Adult",
                IsLeader = true,
                Guid = Guid.NewGuid()
            };
            await _context.GroupTypeRoles.AddAsync(adultRole);
            await _context.SaveChangesAsync();
        }

        // Create person
        var person = new Person
        {
            FirstName = firstName,
            LastName = lastName,
            Gender = Gender.Male,
            BirthYear = 1980,
            BirthMonth = 1,
            BirthDay = 1
        };
        await _context.People.AddAsync(person);
        await _context.SaveChangesAsync();

        // Create person alias
        var personAlias = new PersonAlias
        {
            PersonId = person.Id,
            AliasPersonId = person.Id
        };
        await _context.PersonAliases.AddAsync(personAlias);
        await _context.SaveChangesAsync();

        // Create family using Family entity
        var family = new Family
        {
            Name = familyName,
            IsActive = true
        };
        await _context.Families.AddAsync(family);
        await _context.SaveChangesAsync();

        // Create family member
        var member = new FamilyMember
        {
            FamilyId = family.Id,
            PersonId = person.Id,
            FamilyRoleId = adultRole.Id, // Uses GroupTypeRole ID for now
            IsPrimary = true,
            DateAdded = DateTime.UtcNow
        };
        await _context.FamilyMembers.AddAsync(member);
        await _context.SaveChangesAsync();

        return (family, person, member);
    }

    private async Task<(AttendanceCode code, Attendance attendance)> CreateTestAttendanceAsync(Person person)
    {
        // Create attendance code
        var attendanceCode = new AttendanceCode
        {
            Code = "ABC",
            IssueDateTime = DateTime.UtcNow
        };
        await _context.AttendanceCodes.AddAsync(attendanceCode);

        // Create occurrence
        var occurrence = await CreateTestOccurrenceAsync();

        // Get person alias
        var personAlias = await _context.PersonAliases
            .FirstAsync(pa => pa.PersonId == person.Id);

        // Create attendance
        var attendance = new Attendance
        {
            OccurrenceId = occurrence.Id,
            PersonAliasId = personAlias.Id,
            AttendanceCodeId = attendanceCode.Id,
            StartDateTime = DateTime.UtcNow
        };
        await _context.Attendances.AddAsync(attendance);
        await _context.SaveChangesAsync();

        return (attendanceCode, attendance);
    }

    private async Task<AttendanceOccurrence> CreateTestOccurrenceAsync()
    {
        var occurrence = new AttendanceOccurrence
        {
            OccurrenceDate = DateOnly.FromDateTime(DateTime.UtcNow),
            SundayDate = DateOnly.FromDateTime(DateTime.UtcNow)
        };
        await _context.AttendanceOccurrences.AddAsync(occurrence);
        await _context.SaveChangesAsync();
        return occurrence;
    }

    private async Task<(Family family, GroupTypeRole adultRole, GroupTypeRole childRole)> CreateTestFamilyWithRolesAsync()
    {
        var (family, _, _) = await CreateTestFamilyAsync("Test Family", "Test", "Person");

        var familyGroupType = await _context.GroupTypes
            .FirstAsync(gt => gt.Guid == SystemGuid.GroupType.Family);

        var adultRole = await _context.GroupTypeRoles
            .FirstAsync(r => r.Name == "Adult" && r.GroupTypeId == familyGroupType.Id);

        // Get or create child role
        var childRole = await _context.GroupTypeRoles
            .FirstOrDefaultAsync(r => r.Name == "Child" && r.GroupTypeId == familyGroupType.Id);
        if (childRole == null)
        {
            childRole = new GroupTypeRole
            {
                GroupTypeId = familyGroupType.Id,
                Name = "Child",
                IsLeader = false,
                Guid = Guid.NewGuid()
            };
            await _context.GroupTypeRoles.AddAsync(childRole);
            await _context.SaveChangesAsync();
        }

        return (family, adultRole, childRole);
    }

    [Fact]
    public async Task SearchByNameAsync_PopulatesLastCheckInDate()
    {
        // Arrange
        var (family, person, _) = await CreateTestFamilyAsync("TestFamily", "John", "Doe");
        var (attendanceCode, attendance) = await CreateTestAttendanceAsync(person);

        // Act
        var results = await _sut.SearchByNameAsync("John");

        // Assert
        results.Should().NotBeEmpty();
        var result = results.First();
        var member = result.Members.First(m => m.FirstName == "John");
        member.LastCheckIn.Should().NotBeNull();
        member.LastCheckIn.Should().BeCloseTo(attendance.StartDateTime, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task SearchByNameAsync_WithNoCheckIns_LastCheckInIsNull()
    {
        // Arrange
        await CreateTestFamilyAsync("TestFamily", "Jane", "Doe");

        // Act
        var results = await _sut.SearchByNameAsync("Jane");

        // Assert
        results.Should().NotBeEmpty();
        var member = results.First().Members.First(m => m.FirstName == "Jane");
        member.LastCheckIn.Should().BeNull();
    }

    [Fact]
    public async Task SearchByNameAsync_PopulatesGradeForChildren()
    {
        // Arrange
        var (family, _, childRole) = await CreateTestFamilyWithRolesAsync();

        // Calculate graduation year for a 5th grader
        var currentYear = DateTime.Today.Year;
        var currentMonth = DateTime.Today.Month;
        var schoolYear = currentMonth >= 8 ? currentYear + 1 : currentYear;
        var graduationYear = schoolYear + 7; // 7 years until graduation = 5th grade

        var child = new Person
        {
            FirstName = "Billy",
            LastName = "Doe",
            BirthYear = DateTime.UtcNow.Year - 10,
            BirthMonth = 1,
            BirthDay = 1,
            Gender = Gender.Male,
            GraduationYear = graduationYear
        };
        await _context.People.AddAsync(child);
        await _context.SaveChangesAsync();

        var childMember = new FamilyMember
        {
            FamilyId = family.Id,
            PersonId = child.Id,
            FamilyRoleId = childRole.Id,
            IsPrimary = false,
            DateAdded = DateTime.UtcNow
        };
        await _context.FamilyMembers.AddAsync(childMember);
        await _context.SaveChangesAsync();

        // Act
        var results = await _sut.SearchByNameAsync("Doe");

        // Assert
        results.Should().NotBeEmpty();
        var result = results.First();
        var billyMember = result.Members.FirstOrDefault(m => m.FirstName == "Billy");
        billyMember.Should().NotBeNull();
        billyMember!.Grade.Should().Be("5th Grade");
    }

    [Fact]
    public async Task SearchByNameAsync_WithoutGraduationYear_GradeIsNull()
    {
        // Arrange
        await CreateTestFamilyAsync("TestFamily", "Adult", "Person");

        // Act
        var results = await _sut.SearchByNameAsync("Adult");

        // Assert
        results.Should().NotBeEmpty();
        var member = results.First().Members.First(m => m.FirstName == "Adult");
        member.Grade.Should().BeNull();
    }

    [Fact]
    public async Task SearchByNameAsync_WithGraduatedPerson_GradeShowsGraduated()
    {
        // Arrange
        var (family, person, _) = await CreateTestFamilyAsync("TestFamily", "Graduate", "Person");
        person.GraduationYear = DateTime.Today.Year - 2; // Graduated 2 years ago
        await _context.SaveChangesAsync();

        // Act
        var results = await _sut.SearchByNameAsync("Graduate");

        // Assert
        results.Should().NotBeEmpty();
        var member = results.First().Members.First(m => m.FirstName == "Graduate");
        member.Grade.Should().Be("Graduated");
    }
}
