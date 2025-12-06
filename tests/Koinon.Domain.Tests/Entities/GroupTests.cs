using Koinon.Domain.Entities;
using Xunit;

namespace Koinon.Domain.Tests.Entities;

/// <summary>
/// Unit tests for the Group entity.
/// </summary>
public class GroupTests
{
    [Fact]
    public void Group_Creation_ShouldSetDefaultValues()
    {
        // Arrange & Act
        var group = new Group
        {
            GroupTypeId = 1,
            Name = "Test Group"
        };

        // Assert
        Assert.NotEqual(Guid.Empty, group.Guid);
        Assert.True(group.IsActive);
        Assert.Equal(0, group.Order);
        Assert.False(group.IsSystem);
        Assert.False(group.IsSecurityRole);
        Assert.False(group.IsArchived);
        Assert.False(group.AllowGuests);
        Assert.False(group.IsPublic);
        Assert.NotNull(group.ChildGroups);
        Assert.Empty(group.ChildGroups);
    }

    [Fact]
    public void Group_RequiredFields_MustBeSet()
    {
        // Arrange & Act
        var group = new Group
        {
            GroupTypeId = 1,
            Name = "Smith Family"
        };

        // Assert
        Assert.Equal(1, group.GroupTypeId);
        Assert.Equal("Smith Family", group.Name);
    }

    [Fact]
    public void Group_IdKey_IsComputed()
    {
        // Arrange
        var group = new Group
        {
            GroupTypeId = 1,
            Name = "Test Group",
            Id = 42
        };

        // Act
        var idKey = group.IdKey;

        // Assert
        Assert.NotNull(idKey);
        Assert.NotEmpty(idKey);
        Assert.DoesNotContain("+", idKey);
        Assert.DoesNotContain("/", idKey);
        Assert.DoesNotContain("=", idKey);
    }

    [Fact]
    public void Group_CampusId_CanBeSet()
    {
        // Arrange & Act
        var group = new Group
        {
            GroupTypeId = 1,
            Name = "Test Group",
            CampusId = 5
        };

        // Assert
        Assert.Equal(5, group.CampusId);
    }

    [Fact]
    public void Group_ParentGroupId_CanBeSet()
    {
        // Arrange & Act
        var group = new Group
        {
            GroupTypeId = 1,
            Name = "Sub Group",
            ParentGroupId = 10
        };

        // Assert
        Assert.Equal(10, group.ParentGroupId);
    }

    [Fact]
    public void Group_IsSecurityRole_CanBeSet()
    {
        // Arrange & Act
        var group = new Group
        {
            GroupTypeId = 1,
            Name = "Administrators",
            IsSecurityRole = true
        };

        // Assert
        Assert.True(group.IsSecurityRole);
    }

    [Fact]
    public void Group_GroupCapacity_CanBeSet()
    {
        // Arrange & Act
        var group = new Group
        {
            GroupTypeId = 1,
            Name = "Small Group",
            GroupCapacity = 12
        };

        // Assert
        Assert.Equal(12, group.GroupCapacity);
    }

    [Fact]
    public void Group_AllOptionalFields_CanBeNull()
    {
        // Arrange & Act
        var group = new Group
        {
            GroupTypeId = 1,
            Name = "Minimal Group"
        };

        // Assert
        Assert.Null(group.Description);
        Assert.Null(group.ParentGroupId);
        Assert.Null(group.CampusId);
        Assert.Null(group.GroupCapacity);
        Assert.Null(group.ScheduleId);
        Assert.Null(group.WelcomeSystemCommunicationId);
        Assert.Null(group.ExitSystemCommunicationId);
        Assert.Null(group.RequiredSignatureDocumentTemplateId);
        Assert.Null(group.StatusValueId);
        Assert.Null(group.ArchivedByPersonAliasId);
        Assert.Null(group.ArchivedDateTime);
    }

    [Fact]
    public void Group_ArchivalFields_CanBeSet()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var group = new Group
        {
            GroupTypeId = 1,
            Name = "Old Group",
            IsArchived = true,
            ArchivedDateTime = now,
            ArchivedByPersonAliasId = 1
        };

        // Assert
        Assert.True(group.IsArchived);
        Assert.Equal(now, group.ArchivedDateTime);
        Assert.Equal(1, group.ArchivedByPersonAliasId);
    }

    [Fact]
    public void Group_InheritsFromEntity()
    {
        // Arrange
        var group = new Group
        {
            GroupTypeId = 1,
            Name = "Test Group"
        };

        // Assert
        Assert.IsAssignableFrom<Entity>(group);
        Assert.IsAssignableFrom<IEntity>(group);
        Assert.IsAssignableFrom<IAuditable>(group);
    }

    [Fact]
    public void Group_AuditFields_CanBeSet()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var group = new Group
        {
            GroupTypeId = 1,
            Name = "Test Group",
            CreatedDateTime = now,
            CreatedByPersonAliasId = 1,
            ModifiedDateTime = now.AddMinutes(5),
            ModifiedByPersonAliasId = 2
        };

        // Assert
        Assert.Equal(now, group.CreatedDateTime);
        Assert.Equal(1, group.CreatedByPersonAliasId);
        Assert.Equal(now.AddMinutes(5), group.ModifiedDateTime);
        Assert.Equal(2, group.ModifiedByPersonAliasId);
    }

    [Fact]
    public void Group_GroupType_NavigationCanBeSet()
    {
        // Arrange
        var groupType = new GroupType
        {
            Name = "Family"
        };

        var group = new Group
        {
            GroupTypeId = 1,
            Name = "Smith Family"
        };

        // Act
        group.GroupType = groupType;

        // Assert
        Assert.NotNull(group.GroupType);
        Assert.Equal("Family", group.GroupType.Name);
    }

    [Fact]
    public void Group_Campus_NavigationCanBeSet()
    {
        // Arrange
        var campus = new Campus
        {
            Name = "Main Campus"
        };

        var group = new Group
        {
            GroupTypeId = 1,
            Name = "Test Group",
            CampusId = 1
        };

        // Act
        group.Campus = campus;

        // Assert
        Assert.NotNull(group.Campus);
        Assert.Equal("Main Campus", group.Campus.Name);
    }

    [Fact]
    public void Group_ParentGroup_NavigationCanBeSet()
    {
        // Arrange
        var parentGroup = new Group
        {
            GroupTypeId = 1,
            Name = "Parent Group"
        };

        var childGroup = new Group
        {
            GroupTypeId = 1,
            Name = "Child Group",
            ParentGroupId = 1
        };

        // Act
        childGroup.ParentGroup = parentGroup;

        // Assert
        Assert.NotNull(childGroup.ParentGroup);
        Assert.Equal("Parent Group", childGroup.ParentGroup.Name);
    }

    [Fact]
    public void Group_ChildGroups_NavigationCanBePopulated()
    {
        // Arrange
        var parentGroup = new Group
        {
            GroupTypeId = 1,
            Name = "Parent Group"
        };

        var child1 = new Group
        {
            GroupTypeId = 1,
            Name = "Child 1",
            ParentGroupId = 1
        };

        var child2 = new Group
        {
            GroupTypeId = 1,
            Name = "Child 2",
            ParentGroupId = 1
        };

        // Act
        parentGroup.ChildGroups.Add(child1);
        parentGroup.ChildGroups.Add(child2);

        // Assert
        Assert.Equal(2, parentGroup.ChildGroups.Count);
        Assert.Contains(child1, parentGroup.ChildGroups);
        Assert.Contains(child2, parentGroup.ChildGroups);
    }

    [Fact]
    public void Group_CanSetAllProperties()
    {
        // Arrange & Act
        var group = new Group
        {
            GroupTypeId = 1,
            ParentGroupId = 2,
            CampusId = 3,
            Name = "Complete Group",
            Description = "A group with all properties set",
            IsSecurityRole = false,
            IsActive = true,
            IsArchived = false,
            Order = 5,
            AllowGuests = true,
            IsPublic = true,
            GroupCapacity = 20,
            ScheduleId = 4,
            WelcomeSystemCommunicationId = 5,
            ExitSystemCommunicationId = 6,
            RequiredSignatureDocumentTemplateId = 7,
            StatusValueId = 8
        };

        // Assert
        Assert.Equal(1, group.GroupTypeId);
        Assert.Equal(2, group.ParentGroupId);
        Assert.Equal(3, group.CampusId);
        Assert.Equal("Complete Group", group.Name);
        Assert.Equal("A group with all properties set", group.Description);
        Assert.False(group.IsSecurityRole);
        Assert.True(group.IsActive);
        Assert.False(group.IsArchived);
        Assert.Equal(5, group.Order);
        Assert.True(group.AllowGuests);
        Assert.True(group.IsPublic);
        Assert.Equal(20, group.GroupCapacity);
        Assert.Equal(4, group.ScheduleId);
        Assert.Equal(5, group.WelcomeSystemCommunicationId);
        Assert.Equal(6, group.ExitSystemCommunicationId);
        Assert.Equal(7, group.RequiredSignatureDocumentTemplateId);
        Assert.Equal(8, group.StatusValueId);
    }
}
