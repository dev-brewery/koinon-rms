using FluentAssertions;
using Koinon.Application.Services;
using Koinon.Domain.Entities;
using Koinon.Domain.Enums;
using Koinon.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Koinon.Application.Tests.Services;

/// <summary>
/// Tests for GroupMeetingService.
/// </summary>
public class GroupMeetingServiceTests : IDisposable
{
    private readonly KoinonDbContext _context;
    private readonly Mock<ILogger<GroupMeetingService>> _mockLogger;
    private readonly GroupMeetingService _service;

    public GroupMeetingServiceTests()
    {
        // Setup in-memory database
        var options = new DbContextOptionsBuilder<KoinonDbContext>()
            .UseInMemoryDatabase(databaseName: $"KoinonTestDb_{Guid.NewGuid()}")
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new KoinonDbContext(options);
        _context.Database.EnsureCreated();

        // Setup logger mock
        _mockLogger = new Mock<ILogger<GroupMeetingService>>();

        // Create service
        _service = new GroupMeetingService(_context, _mockLogger.Object);

        // Seed test data
        SeedTestData();
    }

    private void SeedTestData()
    {
        // Add group type
        var groupType = new GroupType
        {
            Id = 1,
            Name = "Small Group",
            IsFamilyGroupType = false,
            AllowMultipleLocations = false,
            IsSystem = false,
            CreatedDateTime = DateTime.UtcNow
        };
        _context.GroupTypes.Add(groupType);

        // Add group type role
        var memberRole = new GroupTypeRole
        {
            Id = 1,
            GroupTypeId = 1,
            Name = "Member",
            IsLeader = false,
            Order = 0,
            CreatedDateTime = DateTime.UtcNow
        };

        var leaderRole = new GroupTypeRole
        {
            Id = 2,
            GroupTypeId = 1,
            Name = "Leader",
            IsLeader = true,
            Order = 1,
            CreatedDateTime = DateTime.UtcNow
        };

        _context.GroupTypeRoles.AddRange(memberRole, leaderRole);

        // Add test people
        var person1 = new Person
        {
            Id = 1,
            FirstName = "John",
            LastName = "Doe",
            Gender = Gender.Male,
            IsEmailActive = true,
            EmailPreference = EmailPreference.EmailAllowed,
            IsDeceased = false,
            CreatedDateTime = DateTime.UtcNow
        };

        var person2 = new Person
        {
            Id = 2,
            FirstName = "Jane",
            LastName = "Smith",
            Gender = Gender.Female,
            IsEmailActive = true,
            EmailPreference = EmailPreference.EmailAllowed,
            IsDeceased = false,
            CreatedDateTime = DateTime.UtcNow
        };

        var person3 = new Person
        {
            Id = 3,
            FirstName = "Bob",
            LastName = "Leader",
            Gender = Gender.Male,
            IsEmailActive = true,
            EmailPreference = EmailPreference.EmailAllowed,
            IsDeceased = false,
            CreatedDateTime = DateTime.UtcNow
        };

        _context.People.AddRange(person1, person2, person3);

        // Add test group
        var group = new Group
        {
            Id = 1,
            GroupTypeId = 1,
            Name = "Wednesday Night Group",
            IsActive = true,
            IsArchived = false,
            IsSecurityRole = false,
            IsPublic = true,
            AllowGuests = false,
            Order = 0,
            CreatedDateTime = DateTime.UtcNow
        };

        _context.Groups.Add(group);

        // Add group members
        var member1 = new GroupMember
        {
            Id = 1,
            GroupId = 1,
            PersonId = 1,
            GroupRoleId = 1,
            GroupMemberStatus = GroupMemberStatus.Active,
            DateTimeAdded = DateTime.UtcNow,
            CreatedDateTime = DateTime.UtcNow
        };

        var member2 = new GroupMember
        {
            Id = 2,
            GroupId = 1,
            PersonId = 2,
            GroupRoleId = 1,
            GroupMemberStatus = GroupMemberStatus.Active,
            DateTimeAdded = DateTime.UtcNow,
            CreatedDateTime = DateTime.UtcNow
        };

        var member3 = new GroupMember
        {
            Id = 3,
            GroupId = 1,
            PersonId = 3,
            GroupRoleId = 2,
            GroupMemberStatus = GroupMemberStatus.Active,
            DateTimeAdded = DateTime.UtcNow,
            CreatedDateTime = DateTime.UtcNow
        };

        // Inactive member (should not receive RSVP)
        var inactiveMember = new GroupMember
        {
            Id = 4,
            GroupId = 1,
            PersonId = 1,
            GroupRoleId = 1,
            GroupMemberStatus = GroupMemberStatus.Inactive,
            DateTimeAdded = DateTime.UtcNow,
            InactiveDateTime = DateTime.UtcNow,
            CreatedDateTime = DateTime.UtcNow
        };

        _context.GroupMembers.AddRange(member1, member2, member3, inactiveMember);

        _context.SaveChanges();
    }

    [Fact]
    public async Task SendRsvpRequestsAsync_CreatesRsvpsForActiveMembers()
    {
        // Arrange
        var group = await _context.Groups.FindAsync(1);
        var meetingDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7));

        // Act
        var count = await _service.SendRsvpRequestsAsync(group!.IdKey, meetingDate);

        // Assert
        count.Should().Be(3); // 3 active members

        var rsvps = await _context.GroupMeetingRsvps
            .Where(r => r.GroupId == 1 && r.MeetingDate == meetingDate)
            .ToListAsync();

        rsvps.Should().HaveCount(3);
        rsvps.Should().AllSatisfy(r => r.Status.Should().Be(RsvpStatus.NoResponse));
        rsvps.Should().Contain(r => r.PersonId == 1);
        rsvps.Should().Contain(r => r.PersonId == 2);
        rsvps.Should().Contain(r => r.PersonId == 3);
    }

    [Fact]
    public async Task SendRsvpRequestsAsync_DoesNotDuplicateExistingRsvps()
    {
        // Arrange
        var group = await _context.Groups.FindAsync(1);
        var meetingDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7));

        // Create an existing RSVP for person 1
        var existingRsvp = new GroupMeetingRsvp
        {
            GroupId = 1,
            MeetingDate = meetingDate,
            PersonId = 1,
            Status = RsvpStatus.Attending,
            CreatedDateTime = DateTime.UtcNow
        };
        _context.GroupMeetingRsvps.Add(existingRsvp);
        await _context.SaveChangesAsync();

        // Act
        var count = await _service.SendRsvpRequestsAsync(group!.IdKey, meetingDate);

        // Assert
        count.Should().Be(2); // Only 2 new RSVPs created (persons 2 and 3)

        var rsvps = await _context.GroupMeetingRsvps
            .Where(r => r.GroupId == 1 && r.MeetingDate == meetingDate)
            .ToListAsync();

        rsvps.Should().HaveCount(3); // Total of 3 RSVPs

        // Existing RSVP should remain unchanged
        var person1Rsvp = rsvps.First(r => r.PersonId == 1);
        person1Rsvp.Status.Should().Be(RsvpStatus.Attending);
    }

    [Fact]
    public async Task SendRsvpRequestsAsync_ReturnsZeroWhenNoActiveMembers()
    {
        // Arrange
        var emptyGroup = new Group
        {
            Id = 2,
            GroupTypeId = 1,
            Name = "Empty Group",
            IsActive = true,
            IsArchived = false,
            CreatedDateTime = DateTime.UtcNow
        };
        _context.Groups.Add(emptyGroup);
        await _context.SaveChangesAsync();

        var meetingDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7));

        // Act
        var count = await _service.SendRsvpRequestsAsync(emptyGroup.IdKey, meetingDate);

        // Assert
        count.Should().Be(0);
    }

    [Fact]
    public async Task UpdateRsvpAsync_SucceedsForGroupMember()
    {
        // Arrange
        var group = await _context.Groups.FindAsync(1);
        var meetingDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7));
        var personId = 1;

        // Act
        var result = await _service.UpdateRsvpAsync(
            personId,
            group!.IdKey,
            meetingDate,
            RsvpStatus.Attending,
            "Looking forward to it!");

        // Assert
        result.Should().BeTrue();

        var rsvp = await _context.GroupMeetingRsvps
            .FirstOrDefaultAsync(r => r.GroupId == 1 && r.MeetingDate == meetingDate && r.PersonId == personId);

        rsvp.Should().NotBeNull();
        rsvp!.Status.Should().Be(RsvpStatus.Attending);
        rsvp.Note.Should().Be("Looking forward to it!");
        rsvp.RespondedDateTime.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateRsvpAsync_FailsForNonMember()
    {
        // Arrange
        var group = await _context.Groups.FindAsync(1);
        var meetingDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7));

        // Create a person who is not a member of the group
        var nonMember = new Person
        {
            Id = 99,
            FirstName = "Non",
            LastName = "Member",
            Gender = Gender.Male,
            IsEmailActive = true,
            EmailPreference = EmailPreference.EmailAllowed,
            IsDeceased = false,
            CreatedDateTime = DateTime.UtcNow
        };
        _context.People.Add(nonMember);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.UpdateRsvpAsync(
            nonMember.Id,
            group!.IdKey,
            meetingDate,
            RsvpStatus.Attending);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateRsvpAsync_HandlesRaceConditionWithRetry()
    {
        // Arrange
        var group = await _context.Groups.FindAsync(1);
        var meetingDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7));
        var personId = 1;

        // Create initial RSVP
        var existingRsvp = new GroupMeetingRsvp
        {
            GroupId = 1,
            MeetingDate = meetingDate,
            PersonId = personId,
            Status = RsvpStatus.NoResponse,
            CreatedDateTime = DateTime.UtcNow
        };
        _context.GroupMeetingRsvps.Add(existingRsvp);
        await _context.SaveChangesAsync();

        // Act - Update the RSVP
        var result = await _service.UpdateRsvpAsync(
            personId,
            group!.IdKey,
            meetingDate,
            RsvpStatus.Attending,
            "Updated");

        // Assert
        result.Should().BeTrue();

        var rsvp = await _context.GroupMeetingRsvps
            .FirstOrDefaultAsync(r => r.GroupId == 1 && r.MeetingDate == meetingDate && r.PersonId == personId);

        rsvp.Should().NotBeNull();
        rsvp!.Status.Should().Be(RsvpStatus.Attending);
        rsvp.Note.Should().Be("Updated");
        rsvp.ModifiedDateTime.Should().NotBeNull();
    }

    [Fact]
    public async Task IsGroupLeaderAsync_ReturnsTrueForLeader()
    {
        // Arrange
        var group = await _context.Groups.FindAsync(1);
        var leaderId = 3; // Person 3 has leader role

        // Act
        var result = await _service.IsGroupLeaderAsync(leaderId, group!.IdKey);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsGroupLeaderAsync_ReturnsFalseForMember()
    {
        // Arrange
        var group = await _context.Groups.FindAsync(1);
        var memberId = 1; // Person 1 has member role

        // Act
        var result = await _service.IsGroupLeaderAsync(memberId, group!.IdKey);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetMyRsvpsAsync_FiltersByDateRange()
    {
        // Arrange
        var personId = 1;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var nextWeek = today.AddDays(7);
        var nextMonth = today.AddDays(30);

        // Create RSVPs at different dates
        var rsvp1 = new GroupMeetingRsvp
        {
            GroupId = 1,
            MeetingDate = nextWeek,
            PersonId = personId,
            Status = RsvpStatus.Attending,
            CreatedDateTime = DateTime.UtcNow
        };

        var rsvp2 = new GroupMeetingRsvp
        {
            GroupId = 1,
            MeetingDate = nextMonth,
            PersonId = personId,
            Status = RsvpStatus.Maybe,
            CreatedDateTime = DateTime.UtcNow
        };

        _context.GroupMeetingRsvps.AddRange(rsvp1, rsvp2);
        await _context.SaveChangesAsync();

        // Act - Get RSVPs only for next 2 weeks
        var result = await _service.GetMyRsvpsAsync(
            personId,
            startDate: today,
            endDate: today.AddDays(14));

        // Assert
        result.Should().HaveCount(1);
        result.First().MeetingDate.Should().Be(nextWeek);
    }

    [Fact]
    public async Task GetRsvpsAsync_ReturnsSummaryWithCorrectCounts()
    {
        // Arrange
        var group = await _context.Groups.FindAsync(1);
        var meetingDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7));

        // Create RSVPs with different statuses
        var rsvps = new[]
        {
            new GroupMeetingRsvp
            {
                GroupId = 1,
                MeetingDate = meetingDate,
                PersonId = 1,
                Status = RsvpStatus.Attending,
                CreatedDateTime = DateTime.UtcNow
            },
            new GroupMeetingRsvp
            {
                GroupId = 1,
                MeetingDate = meetingDate,
                PersonId = 2,
                Status = RsvpStatus.NotAttending,
                CreatedDateTime = DateTime.UtcNow
            },
            new GroupMeetingRsvp
            {
                GroupId = 1,
                MeetingDate = meetingDate,
                PersonId = 3,
                Status = RsvpStatus.Maybe,
                CreatedDateTime = DateTime.UtcNow
            }
        };

        _context.GroupMeetingRsvps.AddRange(rsvps);
        await _context.SaveChangesAsync();

        // Act
        var summary = await _service.GetRsvpsAsync(group!.IdKey, meetingDate);

        // Assert
        summary.Should().NotBeNull();
        summary!.MeetingDate.Should().Be(meetingDate);
        summary.Attending.Should().Be(1);
        summary.NotAttending.Should().Be(1);
        summary.Maybe.Should().Be(1);
        summary.NoResponse.Should().Be(0);
        summary.TotalInvited.Should().Be(3);
        summary.Rsvps.Should().HaveCount(3);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
