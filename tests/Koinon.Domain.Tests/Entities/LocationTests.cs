using Koinon.Domain.Entities;

namespace Koinon.Domain.Tests.Entities;

public class LocationTests
{
    [Fact]
    public void Location_InheritsFromEntity()
    {
        // Arrange
        var location = new Location { Name = "Main Building" };

        // Assert
        Assert.IsAssignableFrom<Entity>(location);
    }

    [Fact]
    public void Location_HasRequiredProperties()
    {
        // Arrange & Act
        var location = new Location
        {
            Name = "Main Building"
        };

        // Assert
        Assert.Equal("Main Building", location.Name);
        Assert.NotEqual(Guid.Empty, location.Guid);
    }

    [Fact]
    public void Location_DefaultValues_AreSetCorrectly()
    {
        // Arrange & Act
        var location = new Location { Name = "Test Location" };

        // Assert
        Assert.True(location.IsActive);
        Assert.False(location.IsGeoPointLocked);
        Assert.Equal(0, location.Order);
        Assert.Empty(location.ChildLocations);
    }

    [Fact]
    public void Location_CanSetAddressProperties()
    {
        // Arrange & Act
        var location = new Location
        {
            Name = "Campus Address",
            Street1 = "123 Main St",
            Street2 = "Suite 100",
            City = "Springfield",
            State = "IL",
            PostalCode = "62701",
            Country = "USA"
        };

        // Assert
        Assert.Equal("123 Main St", location.Street1);
        Assert.Equal("Suite 100", location.Street2);
        Assert.Equal("Springfield", location.City);
        Assert.Equal("IL", location.State);
        Assert.Equal("62701", location.PostalCode);
        Assert.Equal("USA", location.Country);
    }

    [Fact]
    public void Location_CanSetGeographicCoordinates()
    {
        // Arrange & Act
        var location = new Location
        {
            Name = "Geocoded Location",
            Latitude = 39.7817,
            Longitude = -89.6501
        };

        // Assert
        Assert.Equal(39.7817, location.Latitude);
        Assert.Equal(-89.6501, location.Longitude);
    }

    [Fact]
    public void Location_CanSetCapacityThresholds()
    {
        // Arrange & Act
        var location = new Location
        {
            Name = "Room A",
            SoftRoomThreshold = 25,
            FirmRoomThreshold = 30
        };

        // Assert
        Assert.Equal(25, location.SoftRoomThreshold);
        Assert.Equal(30, location.FirmRoomThreshold);
    }

    [Fact]
    public void Location_SupportsHierarchicalRelationships()
    {
        // Arrange
        var building = new Location
        {
            Name = "Main Building"
        };

        var room1 = new Location
        {
            Name = "Room 101",
            ParentLocationId = 1,
            ParentLocation = building
        };

        var room2 = new Location
        {
            Name = "Room 102",
            ParentLocationId = 1,
            ParentLocation = building
        };

        // Act
        building.ChildLocations.Add(room1);
        building.ChildLocations.Add(room2);

        // Assert
        Assert.Equal(2, building.ChildLocations.Count);
        Assert.Contains(room1, building.ChildLocations);
        Assert.Contains(room2, building.ChildLocations);
        Assert.Equal(building, room1.ParentLocation);
        Assert.Equal(building, room2.ParentLocation);
    }

    [Fact]
    public void Location_CanBeAssociatedWithLocationType()
    {
        // Arrange
        var locationType = new DefinedValue
        {
            Value = "Room",
            DefinedTypeId = 1
        };

        // Act
        var location = new Location
        {
            Name = "Conference Room",
            LocationTypeValueId = 1,
            LocationTypeValue = locationType
        };

        // Assert
        Assert.Equal(1, location.LocationTypeValueId);
        Assert.Equal(locationType, location.LocationTypeValue);
        Assert.Equal("Room", location.LocationTypeValue.Value);
    }

    [Fact]
    public void Location_InactiveLocation_CanBeMarked()
    {
        // Arrange
        var location = new Location
        {
            Name = "Old Building",
            IsActive = false
        };

        // Assert
        Assert.False(location.IsActive);
    }

    [Fact]
    public void Location_CanLockGeoPoint()
    {
        // Arrange & Act
        var location = new Location
        {
            Name = "Fixed Location",
            IsGeoPointLocked = true,
            Latitude = 40.7128,
            Longitude = -74.0060
        };

        // Assert
        Assert.True(location.IsGeoPointLocked);
    }

    [Fact]
    public void Location_CanHaveDescription()
    {
        // Arrange & Act
        var location = new Location
        {
            Name = "Main Campus",
            Description = "Our primary location for Sunday services and weekday events"
        };

        // Assert
        Assert.Equal("Our primary location for Sunday services and weekday events", location.Description);
    }

    [Fact]
    public void Location_CanHavePrinterDevice()
    {
        // Arrange & Act
        var location = new Location
        {
            Name = "Check-in Desk",
            PrinterDeviceId = 42
        };

        // Assert
        Assert.Equal(42, location.PrinterDeviceId);
    }

    [Fact]
    public void Location_CanHaveImage()
    {
        // Arrange & Act
        var location = new Location
        {
            Name = "Sanctuary",
            ImageId = 100
        };

        // Assert
        Assert.Equal(100, location.ImageId);
    }

    [Fact]
    public void Location_Order_CanBeSet()
    {
        // Arrange & Act
        var location1 = new Location { Name = "First", Order = 1 };
        var location2 = new Location { Name = "Second", Order = 2 };

        // Assert
        Assert.Equal(1, location1.Order);
        Assert.Equal(2, location2.Order);
    }

    [Fact]
    public void Location_IdKey_IsComputed()
    {
        // Arrange
        var location = new Location
        {
            Name = "Test Location",
            Id = 42
        };

        // Act
        var idKey = location.IdKey;

        // Assert
        Assert.NotNull(idKey);
        Assert.NotEmpty(idKey);
    }
}
