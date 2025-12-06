using Koinon.Domain.Entities;
using Xunit;

namespace Koinon.Domain.Tests.Entities;

/// <summary>
/// Unit tests for the DefinedType entity.
/// </summary>
public class DefinedTypeTests
{
    [Fact]
    public void DefinedType_Initialization_SetsDefaultValues()
    {
        // Arrange & Act
        var definedType = new DefinedType
        {
            Name = "Test Type"
        };

        // Assert
        Assert.NotEqual(Guid.Empty, definedType.Guid);
        Assert.Equal("Test Type", definedType.Name);
        Assert.False(definedType.IsSystem);
        Assert.Equal(0, definedType.Order);
        Assert.NotNull(definedType.DefinedValues);
        Assert.Empty(definedType.DefinedValues);
    }

    [Fact]
    public void DefinedType_IdKey_EncodesIdCorrectly()
    {
        // Arrange
        var definedType = new DefinedType
        {
            Name = "Test Type",
            Id = 123
        };

        // Act
        var idKey = definedType.IdKey;

        // Assert
        Assert.NotNull(idKey);
        Assert.NotEmpty(idKey);
    }

    [Fact]
    public void DefinedType_CanSetAllProperties()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var createdDate = DateTime.UtcNow;
        var modifiedDate = DateTime.UtcNow.AddHours(1);

        // Act
        var definedType = new DefinedType
        {
            Id = 1,
            Guid = guid,
            Name = "Phone Number Type",
            Description = "Types of phone numbers",
            Category = "Communication",
            HelpText = "Select the type of phone number",
            IsSystem = true,
            Order = 5,
            FieldTypeAssemblyName = "Koinon.FieldTypes.PhoneType",
            CreatedDateTime = createdDate,
            ModifiedDateTime = modifiedDate,
            CreatedByPersonAliasId = 100,
            ModifiedByPersonAliasId = 101
        };

        // Assert
        Assert.Equal(1, definedType.Id);
        Assert.Equal(guid, definedType.Guid);
        Assert.Equal("Phone Number Type", definedType.Name);
        Assert.Equal("Types of phone numbers", definedType.Description);
        Assert.Equal("Communication", definedType.Category);
        Assert.Equal("Select the type of phone number", definedType.HelpText);
        Assert.True(definedType.IsSystem);
        Assert.Equal(5, definedType.Order);
        Assert.Equal("Koinon.FieldTypes.PhoneType", definedType.FieldTypeAssemblyName);
        Assert.Equal(createdDate, definedType.CreatedDateTime);
        Assert.Equal(modifiedDate, definedType.ModifiedDateTime);
        Assert.Equal(100, definedType.CreatedByPersonAliasId);
        Assert.Equal(101, definedType.ModifiedByPersonAliasId);
    }

    [Fact]
    public void DefinedType_DefinedValues_CanAddItems()
    {
        // Arrange
        var definedType = new DefinedType
        {
            Name = "Phone Number Type"
        };

        var definedValue = new DefinedValue
        {
            DefinedTypeId = 1,
            Value = "Mobile"
        };

        // Act
        definedType.DefinedValues.Add(definedValue);

        // Assert
        Assert.Single(definedType.DefinedValues);
        Assert.Contains(definedValue, definedType.DefinedValues);
    }
}
