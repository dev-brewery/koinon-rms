using Koinon.Domain.Entities;
using Xunit;

namespace Koinon.Domain.Tests.Entities;

/// <summary>
/// Unit tests for the GroupType entity.
/// </summary>
public class GroupTypeTests
{
    [Fact]
    public void GroupType_Creation_ShouldSetDefaultValues()
    {
        // Arrange & Act
        var groupType = new GroupType
        {
            Name = "Test Group Type"
        };

        // Assert
        Assert.NotEqual(Guid.Empty, groupType.Guid);
        Assert.Equal("Group", groupType.GroupTerm);
        Assert.Equal("Member", groupType.GroupMemberTerm);
        Assert.Equal(0, groupType.Order);
        Assert.False(groupType.IsSystem);
        Assert.False(groupType.TakesAttendance);
        Assert.NotNull(groupType.Roles);
        Assert.Empty(groupType.Roles);
        Assert.NotNull(groupType.Groups);
        Assert.Empty(groupType.Groups);
    }

    [Fact]
    public void GroupType_Name_IsRequired()
    {
        // Arrange & Act
        var groupType = new GroupType
        {
            Name = "Family"
        };

        // Assert
        Assert.NotNull(groupType.Name);
        Assert.Equal("Family", groupType.Name);
    }

    [Fact]
    public void GroupType_IdKey_IsComputed()
    {
        // Arrange
        var groupType = new GroupType
        {
            Name = "Test Group Type",
            Id = 42
        };

        // Act
        var idKey = groupType.IdKey;

        // Assert
        Assert.NotNull(idKey);
        Assert.NotEmpty(idKey);
        Assert.DoesNotContain("+", idKey);
        Assert.DoesNotContain("/", idKey);
        Assert.DoesNotContain("=", idKey);
    }

    [Fact]
    public void GroupType_CustomTerms_CanBeSet()
    {
        // Arrange & Act
        var groupType = new GroupType
        {
            Name = "Family",
            GroupTerm = "Family",
            GroupMemberTerm = "Member"
        };

        // Assert
        Assert.Equal("Family", groupType.GroupTerm);
        Assert.Equal("Member", groupType.GroupMemberTerm);
    }

    [Fact]
    public void GroupType_PurposeValue_CanBeSet()
    {
        // Arrange & Act
        var groupType = new GroupType
        {
            Name = "Family",
            GroupTypePurposeValueId = 1
        };

        // Assert
        Assert.Equal(1, groupType.GroupTypePurposeValueId);
    }

    [Fact]
    public void GroupType_AttendanceSettings_CanBeConfigured()
    {
        // Arrange & Act
        var groupType = new GroupType
        {
            Name = "Serving Team",
            TakesAttendance = true,
            AttendanceCountsAsWeekendService = true,
            SendAttendanceReminder = true
        };

        // Assert
        Assert.True(groupType.TakesAttendance);
        Assert.True(groupType.AttendanceCountsAsWeekendService);
        Assert.True(groupType.SendAttendanceReminder);
    }

    [Fact]
    public void GroupType_AllOptionalFields_CanBeNull()
    {
        // Arrange & Act
        var groupType = new GroupType
        {
            Name = "Minimal GroupType"
        };

        // Assert
        Assert.Null(groupType.Description);
        Assert.Null(groupType.IconCssClass);
        Assert.Null(groupType.DefaultGroupRoleId);
        Assert.Null(groupType.GroupTypePurposeValueId);
        Assert.Null(groupType.ArchivedByPersonAliasId);
        Assert.Null(groupType.ArchivedDateTime);
    }

    [Fact]
    public void GroupType_ArchivalFields_CanBeSet()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var groupType = new GroupType
        {
            Name = "Old GroupType",
            IsArchived = true,
            ArchivedDateTime = now,
            ArchivedByPersonAliasId = 1
        };

        // Assert
        Assert.True(groupType.IsArchived);
        Assert.Equal(now, groupType.ArchivedDateTime);
        Assert.Equal(1, groupType.ArchivedByPersonAliasId);
    }

    [Fact]
    public void GroupType_InheritsFromEntity()
    {
        // Arrange
        var groupType = new GroupType
        {
            Name = "Test GroupType"
        };

        // Assert
        Assert.IsAssignableFrom<Entity>(groupType);
        Assert.IsAssignableFrom<IEntity>(groupType);
        Assert.IsAssignableFrom<IAuditable>(groupType);
    }

    [Fact]
    public void GroupType_AuditFields_CanBeSet()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var groupType = new GroupType
        {
            Name = "Test GroupType",
            CreatedDateTime = now,
            CreatedByPersonAliasId = 1,
            ModifiedDateTime = now.AddMinutes(5),
            ModifiedByPersonAliasId = 2
        };

        // Assert
        Assert.Equal(now, groupType.CreatedDateTime);
        Assert.Equal(1, groupType.CreatedByPersonAliasId);
        Assert.Equal(now.AddMinutes(5), groupType.ModifiedDateTime);
        Assert.Equal(2, groupType.ModifiedByPersonAliasId);
    }

    [Fact]
    public void GroupType_Roles_NavigationCanBePopulated()
    {
        // Arrange
        var groupType = new GroupType
        {
            Name = "Family"
        };

        var adultRole = new GroupTypeRole
        {
            GroupTypeId = 1,
            Name = "Adult"
        };

        var childRole = new GroupTypeRole
        {
            GroupTypeId = 1,
            Name = "Child"
        };

        // Act
        groupType.Roles.Add(adultRole);
        groupType.Roles.Add(childRole);

        // Assert
        Assert.Equal(2, groupType.Roles.Count);
        Assert.Contains(adultRole, groupType.Roles);
        Assert.Contains(childRole, groupType.Roles);
    }

    [Fact]
    public void GroupType_CanSetAllProperties()
    {
        // Arrange & Act
        var groupType = new GroupType
        {
            Name = "Small Group",
            Description = "Community small groups",
            GroupTerm = "Group",
            GroupMemberTerm = "Participant",
            DefaultGroupRoleId = 1,
            IconCssClass = "fa fa-users",
            AllowMultipleLocations = true,
            ShowInGroupList = true,
            ShowInNavigation = true,
            TakesAttendance = true,
            AttendanceCountsAsWeekendService = false,
            SendAttendanceReminder = true,
            ShowConnectionStatus = true,
            EnableSpecificGroupRequirements = true,
            AllowGroupSync = true,
            AllowSpecificGroupMemberAttributes = true,
            GroupTypePurposeValueId = 2,
            IgnorePersonInactivated = false,
            IsArchived = false,
            Order = 3
        };

        // Assert
        Assert.Equal("Small Group", groupType.Name);
        Assert.Equal("Community small groups", groupType.Description);
        Assert.Equal("Group", groupType.GroupTerm);
        Assert.Equal("Participant", groupType.GroupMemberTerm);
        Assert.Equal(1, groupType.DefaultGroupRoleId);
        Assert.Equal("fa fa-users", groupType.IconCssClass);
        Assert.True(groupType.AllowMultipleLocations);
        Assert.True(groupType.ShowInGroupList);
        Assert.True(groupType.ShowInNavigation);
        Assert.True(groupType.TakesAttendance);
        Assert.False(groupType.AttendanceCountsAsWeekendService);
        Assert.True(groupType.SendAttendanceReminder);
        Assert.True(groupType.ShowConnectionStatus);
        Assert.True(groupType.EnableSpecificGroupRequirements);
        Assert.True(groupType.AllowGroupSync);
        Assert.True(groupType.AllowSpecificGroupMemberAttributes);
        Assert.Equal(2, groupType.GroupTypePurposeValueId);
        Assert.False(groupType.IgnorePersonInactivated);
        Assert.False(groupType.IsArchived);
        Assert.Equal(3, groupType.Order);
    }
}
