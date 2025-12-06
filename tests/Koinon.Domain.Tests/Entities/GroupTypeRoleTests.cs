using Koinon.Domain.Entities;
using Xunit;

namespace Koinon.Domain.Tests.Entities;

/// <summary>
/// Unit tests for the GroupTypeRole entity.
/// </summary>
public class GroupTypeRoleTests
{
    [Fact]
    public void GroupTypeRole_Creation_ShouldSetDefaultValues()
    {
        // Arrange & Act
        var role = new GroupTypeRole
        {
            GroupTypeId = 1,
            Name = "Member"
        };

        // Assert
        Assert.NotEqual(Guid.Empty, role.Guid);
        Assert.Equal(0, role.Order);
        Assert.False(role.IsSystem);
        Assert.False(role.IsLeader);
        Assert.False(role.CanView);
        Assert.False(role.CanEdit);
        Assert.False(role.CanManageMembers);
        Assert.False(role.ReceiveRequirementsNotifications);
        Assert.False(role.IsArchived);
    }

    [Fact]
    public void GroupTypeRole_RequiredFields_MustBeSet()
    {
        // Arrange & Act
        var role = new GroupTypeRole
        {
            GroupTypeId = 1,
            Name = "Leader"
        };

        // Assert
        Assert.Equal(1, role.GroupTypeId);
        Assert.Equal("Leader", role.Name);
    }

    [Fact]
    public void GroupTypeRole_IdKey_IsComputed()
    {
        // Arrange
        var role = new GroupTypeRole
        {
            GroupTypeId = 1,
            Name = "Member",
            Id = 42
        };

        // Act
        var idKey = role.IdKey;

        // Assert
        Assert.NotNull(idKey);
        Assert.NotEmpty(idKey);
        Assert.DoesNotContain("+", idKey);
        Assert.DoesNotContain("/", idKey);
        Assert.DoesNotContain("=", idKey);
    }

    [Fact]
    public void GroupTypeRole_IsLeader_CanBeSet()
    {
        // Arrange & Act
        var role = new GroupTypeRole
        {
            GroupTypeId = 1,
            Name = "Leader",
            IsLeader = true
        };

        // Assert
        Assert.True(role.IsLeader);
    }

    [Fact]
    public void GroupTypeRole_Permissions_CanBeConfigured()
    {
        // Arrange & Act
        var role = new GroupTypeRole
        {
            GroupTypeId = 1,
            Name = "Leader",
            CanView = true,
            CanEdit = true,
            CanManageMembers = true,
            ReceiveRequirementsNotifications = true
        };

        // Assert
        Assert.True(role.CanView);
        Assert.True(role.CanEdit);
        Assert.True(role.CanManageMembers);
        Assert.True(role.ReceiveRequirementsNotifications);
    }

    [Fact]
    public void GroupTypeRole_CountConstraints_CanBeSet()
    {
        // Arrange & Act
        var role = new GroupTypeRole
        {
            GroupTypeId = 1,
            Name = "Leader",
            MinCount = 1,
            MaxCount = 2
        };

        // Assert
        Assert.Equal(1, role.MinCount);
        Assert.Equal(2, role.MaxCount);
    }

    [Fact]
    public void GroupTypeRole_AllOptionalFields_CanBeNull()
    {
        // Arrange & Act
        var role = new GroupTypeRole
        {
            GroupTypeId = 1,
            Name = "Minimal Role"
        };

        // Assert
        Assert.Null(role.Description);
        Assert.Null(role.MinCount);
        Assert.Null(role.MaxCount);
    }

    [Fact]
    public void GroupTypeRole_InheritsFromEntity()
    {
        // Arrange
        var role = new GroupTypeRole
        {
            GroupTypeId = 1,
            Name = "Test Role"
        };

        // Assert
        Assert.IsAssignableFrom<Entity>(role);
        Assert.IsAssignableFrom<IEntity>(role);
        Assert.IsAssignableFrom<IAuditable>(role);
    }

    [Fact]
    public void GroupTypeRole_AuditFields_CanBeSet()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var role = new GroupTypeRole
        {
            GroupTypeId = 1,
            Name = "Test Role",
            CreatedDateTime = now,
            CreatedByPersonAliasId = 1,
            ModifiedDateTime = now.AddMinutes(5),
            ModifiedByPersonAliasId = 2
        };

        // Assert
        Assert.Equal(now, role.CreatedDateTime);
        Assert.Equal(1, role.CreatedByPersonAliasId);
        Assert.Equal(now.AddMinutes(5), role.ModifiedDateTime);
        Assert.Equal(2, role.ModifiedByPersonAliasId);
    }

    [Fact]
    public void GroupTypeRole_GroupType_NavigationCanBeSet()
    {
        // Arrange
        var groupType = new GroupType
        {
            Name = "Family"
        };

        var role = new GroupTypeRole
        {
            GroupTypeId = 1,
            Name = "Adult"
        };

        // Act
        role.GroupType = groupType;

        // Assert
        Assert.NotNull(role.GroupType);
        Assert.Equal("Family", role.GroupType.Name);
    }

    [Fact]
    public void GroupTypeRole_CanSetAllProperties()
    {
        // Arrange & Act
        var role = new GroupTypeRole
        {
            GroupTypeId = 1,
            Name = "Leader",
            Description = "Group leader with full permissions",
            IsLeader = true,
            CanView = true,
            CanEdit = true,
            CanManageMembers = true,
            ReceiveRequirementsNotifications = true,
            Order = 1,
            MinCount = 1,
            MaxCount = 3,
            IsArchived = false
        };

        // Assert
        Assert.Equal(1, role.GroupTypeId);
        Assert.Equal("Leader", role.Name);
        Assert.Equal("Group leader with full permissions", role.Description);
        Assert.True(role.IsLeader);
        Assert.True(role.CanView);
        Assert.True(role.CanEdit);
        Assert.True(role.CanManageMembers);
        Assert.True(role.ReceiveRequirementsNotifications);
        Assert.Equal(1, role.Order);
        Assert.Equal(1, role.MinCount);
        Assert.Equal(3, role.MaxCount);
        Assert.False(role.IsArchived);
    }
}
