using Koinon.Domain.Entities;
using Xunit;

namespace Koinon.Domain.Tests.Entities;

/// <summary>
/// Unit tests for the PhoneNumber entity.
/// </summary>
public class PhoneNumberTests
{
    [Fact]
    public void PhoneNumber_Creation_ShouldSetRequiredProperties()
    {
        // Arrange & Act
        var phoneNumber = new PhoneNumber
        {
            PersonId = 1,
            Number = "555-1234"
        };

        // Assert
        Assert.Equal(1, phoneNumber.PersonId);
        Assert.Equal("555-1234", phoneNumber.Number);
        Assert.NotEqual(Guid.Empty, phoneNumber.Guid); // Should auto-generate
    }

    [Fact]
    public void Number_ShouldStoreFormattedNumber()
    {
        // Arrange & Act
        var phoneNumber = new PhoneNumber
        {
            PersonId = 1,
            Number = "(555) 123-4567"
        };

        // Assert
        Assert.Equal("(555) 123-4567", phoneNumber.Number);
    }

    [Fact]
    public void Number_ShouldStoreUnformattedNumber()
    {
        // Arrange & Act
        var phoneNumber = new PhoneNumber
        {
            PersonId = 1,
            Number = "5551234567"
        };

        // Assert
        Assert.Equal("5551234567", phoneNumber.Number);
    }

    [Fact]
    public void CountryCode_DefaultValue_ShouldBeNull()
    {
        // Arrange & Act
        var phoneNumber = new PhoneNumber
        {
            PersonId = 1,
            Number = "555-1234"
        };

        // Assert
        Assert.Null(phoneNumber.CountryCode);
    }

    [Fact]
    public void CountryCode_CanBeSet_ForInternationalNumbers()
    {
        // Arrange & Act
        var phoneNumber = new PhoneNumber
        {
            PersonId = 1,
            Number = "555-1234",
            CountryCode = "1"
        };

        // Assert
        Assert.Equal("1", phoneNumber.CountryCode);
    }

    [Fact]
    public void Extension_DefaultValue_ShouldBeNull()
    {
        // Arrange & Act
        var phoneNumber = new PhoneNumber
        {
            PersonId = 1,
            Number = "555-1234"
        };

        // Assert
        Assert.Null(phoneNumber.Extension);
    }

    [Fact]
    public void Extension_CanBeSet_ForWorkNumbers()
    {
        // Arrange & Act
        var phoneNumber = new PhoneNumber
        {
            PersonId = 1,
            Number = "555-1234",
            Extension = "1234"
        };

        // Assert
        Assert.Equal("1234", phoneNumber.Extension);
    }

    [Fact]
    public void NumberTypeValueId_DefaultValue_ShouldBeNull()
    {
        // Arrange & Act
        var phoneNumber = new PhoneNumber
        {
            PersonId = 1,
            Number = "555-1234"
        };

        // Assert
        Assert.Null(phoneNumber.NumberTypeValueId);
    }

    [Fact]
    public void NumberTypeValueId_CanBeSet_ToIndicatePhoneType()
    {
        // Arrange & Act
        var phoneNumber = new PhoneNumber
        {
            PersonId = 1,
            Number = "555-1234",
            NumberTypeValueId = 10 // e.g., Mobile type
        };

        // Assert
        Assert.Equal(10, phoneNumber.NumberTypeValueId);
    }

    [Fact]
    public void IsMessagingEnabled_DefaultValue_ShouldBeFalse()
    {
        // Arrange & Act
        var phoneNumber = new PhoneNumber
        {
            PersonId = 1,
            Number = "555-1234"
        };

        // Assert
        Assert.False(phoneNumber.IsMessagingEnabled);
    }

    [Fact]
    public void IsMessagingEnabled_CanBeSet_ForMobileNumbers()
    {
        // Arrange & Act
        var phoneNumber = new PhoneNumber
        {
            PersonId = 1,
            Number = "555-1234",
            IsMessagingEnabled = true
        };

        // Assert
        Assert.True(phoneNumber.IsMessagingEnabled);
    }

    [Fact]
    public void IsUnlisted_DefaultValue_ShouldBeFalse()
    {
        // Arrange & Act
        var phoneNumber = new PhoneNumber
        {
            PersonId = 1,
            Number = "555-1234"
        };

        // Assert
        Assert.False(phoneNumber.IsUnlisted);
    }

    [Fact]
    public void IsUnlisted_CanBeSet_ToHideFromDirectories()
    {
        // Arrange & Act
        var phoneNumber = new PhoneNumber
        {
            PersonId = 1,
            Number = "555-1234",
            IsUnlisted = true
        };

        // Assert
        Assert.True(phoneNumber.IsUnlisted);
    }

    [Fact]
    public void Description_DefaultValue_ShouldBeNull()
    {
        // Arrange & Act
        var phoneNumber = new PhoneNumber
        {
            PersonId = 1,
            Number = "555-1234"
        };

        // Assert
        Assert.Null(phoneNumber.Description);
    }

    [Fact]
    public void Description_CanBeSet_ToProvideContext()
    {
        // Arrange & Act
        var phoneNumber = new PhoneNumber
        {
            PersonId = 1,
            Number = "555-1234",
            Description = "Primary contact number"
        };

        // Assert
        Assert.Equal("Primary contact number", phoneNumber.Description);
    }

    [Fact]
    public void IdKey_ShouldBeGenerated_FromId()
    {
        // Arrange
        var phoneNumber = new PhoneNumber
        {
            PersonId = 1,
            Number = "555-1234",
            Id = 456
        };

        // Act
        var idKey = phoneNumber.IdKey;

        // Assert
        Assert.NotNull(idKey);
        Assert.NotEmpty(idKey);
    }

    [Fact]
    public void PhoneNumber_WithAllProperties_ShouldBeValid()
    {
        // Arrange & Act
        var phoneNumber = new PhoneNumber
        {
            PersonId = 123,
            Number = "(555) 123-4567",
            CountryCode = "1",
            Extension = "9876",
            NumberTypeValueId = 10,
            IsMessagingEnabled = true,
            IsUnlisted = false,
            Description = "Work phone - direct line"
        };

        // Assert
        Assert.Equal(123, phoneNumber.PersonId);
        Assert.Equal("(555) 123-4567", phoneNumber.Number);
        Assert.Equal("1", phoneNumber.CountryCode);
        Assert.Equal("9876", phoneNumber.Extension);
        Assert.Equal(10, phoneNumber.NumberTypeValueId);
        Assert.True(phoneNumber.IsMessagingEnabled);
        Assert.False(phoneNumber.IsUnlisted);
        Assert.Equal("Work phone - direct line", phoneNumber.Description);
    }
}
