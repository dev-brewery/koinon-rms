using Koinon.Domain.Entities;
using Xunit;

namespace Koinon.Domain.Tests.Entities;

/// <summary>
/// Unit tests for the DefinedValue entity.
/// </summary>
public class DefinedValueTests
{
    [Fact]
    public void DefinedValue_Initialization_SetsDefaultValues()
    {
        // Arrange & Act
        var definedValue = new DefinedValue
        {
            DefinedTypeId = 1,
            Value = "Test Value"
        };

        // Assert
        Assert.NotEqual(Guid.Empty, definedValue.Guid);
        Assert.Equal(1, definedValue.DefinedTypeId);
        Assert.Equal("Test Value", definedValue.Value);
        Assert.True(definedValue.IsActive);
        Assert.Equal(0, definedValue.Order);
    }

    [Fact]
    public void DefinedValue_IdKey_EncodesIdCorrectly()
    {
        // Arrange
        var definedValue = new DefinedValue
        {
            DefinedTypeId = 1,
            Value = "Mobile",
            Id = 456
        };

        // Act
        var idKey = definedValue.IdKey;

        // Assert
        Assert.NotNull(idKey);
        Assert.NotEmpty(idKey);
    }

    [Fact]
    public void DefinedValue_CanSetAllProperties()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var createdDate = DateTime.UtcNow;
        var modifiedDate = DateTime.UtcNow.AddHours(1);

        var definedType = new DefinedType
        {
            Id = 1,
            Name = "Phone Number Type"
        };

        // Act
        var definedValue = new DefinedValue
        {
            Id = 10,
            Guid = guid,
            DefinedTypeId = 1,
            Value = "Mobile",
            Description = "Mobile phone number",
            Order = 1,
            IsActive = true,
            CreatedDateTime = createdDate,
            ModifiedDateTime = modifiedDate,
            CreatedByPersonAliasId = 200,
            ModifiedByPersonAliasId = 201,
            DefinedType = definedType
        };

        // Assert
        Assert.Equal(10, definedValue.Id);
        Assert.Equal(guid, definedValue.Guid);
        Assert.Equal(1, definedValue.DefinedTypeId);
        Assert.Equal("Mobile", definedValue.Value);
        Assert.Equal("Mobile phone number", definedValue.Description);
        Assert.Equal(1, definedValue.Order);
        Assert.True(definedValue.IsActive);
        Assert.Equal(createdDate, definedValue.CreatedDateTime);
        Assert.Equal(modifiedDate, definedValue.ModifiedDateTime);
        Assert.Equal(200, definedValue.CreatedByPersonAliasId);
        Assert.Equal(201, definedValue.ModifiedByPersonAliasId);
        Assert.Same(definedType, definedValue.DefinedType);
    }

    [Fact]
    public void DefinedValue_IsActive_DefaultsToTrue()
    {
        // Arrange & Act
        var definedValue = new DefinedValue
        {
            DefinedTypeId = 1,
            Value = "Active Value"
        };

        // Assert
        Assert.True(definedValue.IsActive);
    }

    [Fact]
    public void DefinedValue_IsActive_CanBeSetToFalse()
    {
        // Arrange & Act
        var definedValue = new DefinedValue
        {
            DefinedTypeId = 1,
            Value = "Inactive Value",
            IsActive = false
        };

        // Assert
        Assert.False(definedValue.IsActive);
    }

    [Fact]
    public void DefinedValue_NavigationProperty_CanBeNull()
    {
        // Arrange & Act
        var definedValue = new DefinedValue
        {
            DefinedTypeId = 1,
            Value = "Test Value",
            DefinedType = null
        };

        // Assert
        Assert.Null(definedValue.DefinedType);
    }

    [Fact]
    public void DefinedValue_Order_CanBeSet()
    {
        // Arrange & Act
        var definedValue = new DefinedValue
        {
            DefinedTypeId = 1,
            Value = "Third Option",
            Order = 3
        };

        // Assert
        Assert.Equal(3, definedValue.Order);
    }
}
