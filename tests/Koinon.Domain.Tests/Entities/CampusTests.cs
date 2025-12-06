using Koinon.Domain.Entities;
using Xunit;

namespace Koinon.Domain.Tests.Entities;

/// <summary>
/// Unit tests for the Campus entity.
/// </summary>
public class CampusTests
{
    [Fact]
    public void Campus_Creation_ShouldSetDefaultValues()
    {
        // Arrange & Act
        var campus = new Campus
        {
            Name = "Test Campus"
        };

        // Assert
        Assert.NotEqual(Guid.Empty, campus.Guid);
        Assert.True(campus.IsActive);
        Assert.Equal(0, campus.Order);
    }

    [Fact]
    public void Campus_Name_IsRequired()
    {
        // Arrange & Act
        var campus = new Campus
        {
            Name = "Main Campus"
        };

        // Assert
        Assert.NotNull(campus.Name);
        Assert.Equal("Main Campus", campus.Name);
    }

    [Fact]
    public void Campus_ShortCode_CanBeSet()
    {
        // Arrange & Act
        var campus = new Campus
        {
            Name = "Main Campus",
            ShortCode = "MAIN"
        };

        // Assert
        Assert.Equal("MAIN", campus.ShortCode);
    }

    [Fact]
    public void Campus_IdKey_IsComputed()
    {
        // Arrange
        var campus = new Campus
        {
            Name = "Test Campus",
            Id = 42
        };

        // Act
        var idKey = campus.IdKey;

        // Assert
        Assert.NotNull(idKey);
        Assert.NotEmpty(idKey);
        // IdKey should be URL-safe Base64
        Assert.DoesNotContain("+", idKey);
        Assert.DoesNotContain("/", idKey);
        Assert.DoesNotContain("=", idKey);
    }

    [Fact]
    public void Campus_TimeZoneId_CanStoreIANATimeZone()
    {
        // Arrange & Act
        var campus = new Campus
        {
            Name = "West Campus",
            TimeZoneId = "America/Los_Angeles"
        };

        // Assert
        Assert.Equal("America/Los_Angeles", campus.TimeZoneId);
    }

    [Fact]
    public void Campus_AllOptionalFields_CanBeNull()
    {
        // Arrange & Act
        var campus = new Campus
        {
            Name = "Minimal Campus"
        };

        // Assert
        Assert.Null(campus.ShortCode);
        Assert.Null(campus.Description);
        Assert.Null(campus.Url);
        Assert.Null(campus.PhoneNumber);
        Assert.Null(campus.TimeZoneId);
        Assert.Null(campus.CampusStatusValueId);
        Assert.Null(campus.LeaderPersonAliasId);
        Assert.Null(campus.ServiceTimes);
    }

    [Fact]
    public void Campus_CanSetAllProperties()
    {
        // Arrange & Act
        var campus = new Campus
        {
            Name = "East Campus",
            ShortCode = "EAST",
            Description = "Our east side location",
            IsActive = false,
            Url = "https://church.org/east",
            PhoneNumber = "555-1234",
            TimeZoneId = "America/New_York",
            CampusStatusValueId = 1,
            LeaderPersonAliasId = 2,
            ServiceTimes = "Sunday 9:00 AM, 11:00 AM",
            Order = 2
        };

        // Assert
        Assert.Equal("East Campus", campus.Name);
        Assert.Equal("EAST", campus.ShortCode);
        Assert.Equal("Our east side location", campus.Description);
        Assert.False(campus.IsActive);
        Assert.Equal("https://church.org/east", campus.Url);
        Assert.Equal("555-1234", campus.PhoneNumber);
        Assert.Equal("America/New_York", campus.TimeZoneId);
        Assert.Equal(1, campus.CampusStatusValueId);
        Assert.Equal(2, campus.LeaderPersonAliasId);
        Assert.Equal("Sunday 9:00 AM, 11:00 AM", campus.ServiceTimes);
        Assert.Equal(2, campus.Order);
    }

    [Fact]
    public void Campus_InheritsFromEntity()
    {
        // Arrange
        var campus = new Campus
        {
            Name = "Test Campus"
        };

        // Assert
        Assert.IsAssignableFrom<Entity>(campus);
        Assert.IsAssignableFrom<IEntity>(campus);
        Assert.IsAssignableFrom<IAuditable>(campus);
    }

    [Fact]
    public void Campus_AuditFields_CanBeSet()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var campus = new Campus
        {
            Name = "Test Campus",
            CreatedDateTime = now,
            CreatedByPersonAliasId = 1,
            ModifiedDateTime = now.AddMinutes(5),
            ModifiedByPersonAliasId = 2
        };

        // Assert
        Assert.Equal(now, campus.CreatedDateTime);
        Assert.Equal(1, campus.CreatedByPersonAliasId);
        Assert.Equal(now.AddMinutes(5), campus.ModifiedDateTime);
        Assert.Equal(2, campus.ModifiedByPersonAliasId);
    }

    [Fact]
    public void Campus_CampusStatusValue_NavigationPropertyCanBeSet()
    {
        // Arrange
        var campus = new Campus
        {
            Name = "Test Campus",
            CampusStatusValueId = 1
        };

        var statusValue = new DefinedValue
        {
            DefinedTypeId = 1,
            Value = "Active"
        };

        // Act
        campus.CampusStatusValue = statusValue;

        // Assert
        Assert.NotNull(campus.CampusStatusValue);
        Assert.Equal("Active", campus.CampusStatusValue.Value);
    }
}
