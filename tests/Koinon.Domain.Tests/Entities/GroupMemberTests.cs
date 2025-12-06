using Koinon.Domain.Entities;
using Koinon.Domain.Enums;
using Xunit;

namespace Koinon.Domain.Tests.Entities;

/// <summary>
/// Unit tests for the GroupMember entity.
/// </summary>
public class GroupMemberTests
{
    [Fact]
    public void GroupMember_Creation_ShouldSetRequiredProperties()
    {
        // Arrange & Act
        var groupMember = new GroupMember
        {
            PersonId = 1,
            GroupId = 2,
            GroupRoleId = 3
        };

        // Assert
        Assert.Equal(1, groupMember.PersonId);
        Assert.Equal(2, groupMember.GroupId);
        Assert.Equal(3, groupMember.GroupRoleId);
        Assert.NotEqual(Guid.Empty, groupMember.Guid); // Should auto-generate
    }

    [Fact]
    public void GroupMemberStatus_DefaultValue_ShouldBeActive()
    {
        // Arrange & Act
        var groupMember = new GroupMember
        {
            PersonId = 1,
            GroupId = 2,
            GroupRoleId = 3
        };

        // Assert
        Assert.Equal(GroupMemberStatus.Active, groupMember.GroupMemberStatus);
    }

    [Fact]
    public void GroupMemberStatus_CanBeSet_ToInactive()
    {
        // Arrange
        var groupMember = new GroupMember
        {
            PersonId = 1,
            GroupId = 2,
            GroupRoleId = 3,
            GroupMemberStatus = GroupMemberStatus.Inactive
        };

        // Act & Assert
        Assert.Equal(GroupMemberStatus.Inactive, groupMember.GroupMemberStatus);
    }

    [Fact]
    public void GroupMemberStatus_CanBeSet_ToPending()
    {
        // Arrange
        var groupMember = new GroupMember
        {
            PersonId = 1,
            GroupId = 2,
            GroupRoleId = 3,
            GroupMemberStatus = GroupMemberStatus.Pending
        };

        // Act & Assert
        Assert.Equal(GroupMemberStatus.Pending, groupMember.GroupMemberStatus);
    }

    [Fact]
    public void IsArchived_DefaultValue_ShouldBeFalse()
    {
        // Arrange & Act
        var groupMember = new GroupMember
        {
            PersonId = 1,
            GroupId = 2,
            GroupRoleId = 3
        };

        // Assert
        Assert.False(groupMember.IsArchived);
    }

    [Fact]
    public void IsNotified_DefaultValue_ShouldBeFalse()
    {
        // Arrange & Act
        var groupMember = new GroupMember
        {
            PersonId = 1,
            GroupId = 2,
            GroupRoleId = 3
        };

        // Assert
        Assert.False(groupMember.IsNotified);
    }

    [Fact]
    public void IsSystem_DefaultValue_ShouldBeFalse()
    {
        // Arrange & Act
        var groupMember = new GroupMember
        {
            PersonId = 1,
            GroupId = 2,
            GroupRoleId = 3
        };

        // Assert
        Assert.False(groupMember.IsSystem);
    }

    [Fact]
    public void DateTimeAdded_CanBeSet()
    {
        // Arrange
        var addedDate = new DateTime(2025, 1, 1, 10, 30, 0);
        var groupMember = new GroupMember
        {
            PersonId = 1,
            GroupId = 2,
            GroupRoleId = 3,
            DateTimeAdded = addedDate
        };

        // Act & Assert
        Assert.Equal(addedDate, groupMember.DateTimeAdded);
    }

    [Fact]
    public void InactiveDateTime_CanBeSet()
    {
        // Arrange
        var inactiveDate = new DateTime(2025, 6, 1, 15, 45, 0);
        var groupMember = new GroupMember
        {
            PersonId = 1,
            GroupId = 2,
            GroupRoleId = 3,
            GroupMemberStatus = GroupMemberStatus.Inactive,
            InactiveDateTime = inactiveDate
        };

        // Act & Assert
        Assert.Equal(inactiveDate, groupMember.InactiveDateTime);
    }

    [Fact]
    public void ArchivedDateTime_CanBeSet()
    {
        // Arrange
        var archivedDate = new DateTime(2025, 12, 1, 8, 0, 0);
        var groupMember = new GroupMember
        {
            PersonId = 1,
            GroupId = 2,
            GroupRoleId = 3,
            IsArchived = true,
            ArchivedDateTime = archivedDate,
            ArchivedByPersonAliasId = 999
        };

        // Act & Assert
        Assert.True(groupMember.IsArchived);
        Assert.Equal(archivedDate, groupMember.ArchivedDateTime);
        Assert.Equal(999, groupMember.ArchivedByPersonAliasId);
    }

    [Fact]
    public void Note_CanBeSet()
    {
        // Arrange
        var note = "This member is a volunteer coordinator";
        var groupMember = new GroupMember
        {
            PersonId = 1,
            GroupId = 2,
            GroupRoleId = 3,
            Note = note
        };

        // Act & Assert
        Assert.Equal(note, groupMember.Note);
    }

    [Fact]
    public void GuestCount_CanBeSet()
    {
        // Arrange
        var groupMember = new GroupMember
        {
            PersonId = 1,
            GroupId = 2,
            GroupRoleId = 3,
            GuestCount = 5
        };

        // Act & Assert
        Assert.Equal(5, groupMember.GuestCount);
    }

    [Fact]
    public void CommunicationPreference_CanBeSet()
    {
        // Arrange
        var groupMember = new GroupMember
        {
            PersonId = 1,
            GroupId = 2,
            GroupRoleId = 3,
            CommunicationPreference = 1 // SMS
        };

        // Act & Assert
        Assert.Equal(1, groupMember.CommunicationPreference);
    }

    [Fact]
    public void IdKey_ShouldBeGenerated_FromId()
    {
        // Arrange
        var groupMember = new GroupMember
        {
            PersonId = 1,
            GroupId = 2,
            GroupRoleId = 3,
            Id = 456
        };

        // Act
        var idKey = groupMember.IdKey;

        // Assert
        Assert.NotNull(idKey);
        Assert.NotEmpty(idKey);
    }

    [Fact]
    public void NavigationProperties_CanBeSet()
    {
        // Arrange
        var person = new Person { FirstName = "John", LastName = "Doe", Id = 1 };
        var group = new Group { Name = "Test Group", GroupTypeId = 1, Id = 2 };
        var groupRole = new GroupTypeRole { Name = "Member", GroupTypeId = 1, Id = 3 };

        var groupMember = new GroupMember
        {
            PersonId = 1,
            GroupId = 2,
            GroupRoleId = 3,
            Person = person,
            Group = group,
            GroupRole = groupRole
        };

        // Act & Assert
        Assert.Equal(person, groupMember.Person);
        Assert.Equal(group, groupMember.Group);
        Assert.Equal(groupRole, groupMember.GroupRole);
    }
}
