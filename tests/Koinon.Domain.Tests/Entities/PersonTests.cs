using Koinon.Domain.Entities;
using Koinon.Domain.Enums;
using Xunit;

namespace Koinon.Domain.Tests.Entities;

/// <summary>
/// Unit tests for the Person entity.
/// </summary>
public class PersonTests
{
    [Fact]
    public void Person_Creation_ShouldSetRequiredProperties()
    {
        // Arrange & Act
        var person = new Person
        {
            FirstName = "John",
            LastName = "Doe"
        };

        // Assert
        Assert.Equal("John", person.FirstName);
        Assert.Equal("Doe", person.LastName);
        Assert.NotEqual(Guid.Empty, person.Guid); // Should auto-generate
    }

    [Fact]
    public void FullName_WithoutNickName_ShouldUseFirstName()
    {
        // Arrange
        var person = new Person
        {
            FirstName = "John",
            LastName = "Doe"
        };

        // Act
        var fullName = person.FullName;

        // Assert
        Assert.Equal("John Doe", fullName);
    }

    [Fact]
    public void FullName_WithNickName_ShouldUseNickName()
    {
        // Arrange
        var person = new Person
        {
            FirstName = "Jonathan",
            NickName = "John",
            LastName = "Doe"
        };

        // Act
        var fullName = person.FullName;

        // Assert
        Assert.Equal("John Doe", fullName);
    }

    [Fact]
    public void FullName_WithEmptyNickName_ShouldUseFirstName()
    {
        // Arrange
        var person = new Person
        {
            FirstName = "John",
            NickName = "   ", // Whitespace only
            LastName = "Doe"
        };

        // Act
        var fullName = person.FullName;

        // Assert
        Assert.Equal("John Doe", fullName);
    }

    [Fact]
    public void FullNameReversed_WithoutNickName_ShouldFormatCorrectly()
    {
        // Arrange
        var person = new Person
        {
            FirstName = "John",
            LastName = "Doe"
        };

        // Act
        var fullNameReversed = person.FullNameReversed;

        // Assert
        Assert.Equal("Doe, John", fullNameReversed);
    }

    [Fact]
    public void FullNameReversed_WithNickName_ShouldUseNickName()
    {
        // Arrange
        var person = new Person
        {
            FirstName = "Jonathan",
            NickName = "John",
            LastName = "Doe"
        };

        // Act
        var fullNameReversed = person.FullNameReversed;

        // Assert
        Assert.Equal("Doe, John", fullNameReversed);
    }

    [Fact]
    public void BirthDate_WithAllComponents_ShouldReturnValidDate()
    {
        // Arrange
        var person = new Person
        {
            FirstName = "John",
            LastName = "Doe",
            BirthYear = 1990,
            BirthMonth = 5,
            BirthDay = 15
        };

        // Act
        var birthDate = person.BirthDate;

        // Assert
        Assert.NotNull(birthDate);
        Assert.Equal(new DateOnly(1990, 5, 15), birthDate);
    }

    [Fact]
    public void BirthDate_WithMissingYear_ShouldReturnNull()
    {
        // Arrange
        var person = new Person
        {
            FirstName = "John",
            LastName = "Doe",
            BirthMonth = 5,
            BirthDay = 15
        };

        // Act
        var birthDate = person.BirthDate;

        // Assert
        Assert.Null(birthDate);
    }

    [Fact]
    public void BirthDate_WithMissingMonth_ShouldReturnNull()
    {
        // Arrange
        var person = new Person
        {
            FirstName = "John",
            LastName = "Doe",
            BirthYear = 1990,
            BirthDay = 15
        };

        // Act
        var birthDate = person.BirthDate;

        // Assert
        Assert.Null(birthDate);
    }

    [Fact]
    public void BirthDate_WithMissingDay_ShouldReturnNull()
    {
        // Arrange
        var person = new Person
        {
            FirstName = "John",
            LastName = "Doe",
            BirthYear = 1990,
            BirthMonth = 5
        };

        // Act
        var birthDate = person.BirthDate;

        // Assert
        Assert.Null(birthDate);
    }

    [Fact]
    public void BirthDate_WithInvalidComponents_ShouldReturnNull()
    {
        // Arrange
        var person = new Person
        {
            FirstName = "John",
            LastName = "Doe",
            BirthYear = 1990,
            BirthMonth = 2,
            BirthDay = 30 // February 30th doesn't exist
        };

        // Act
        var birthDate = person.BirthDate;

        // Assert
        Assert.Null(birthDate);
    }

    [Fact]
    public void Gender_DefaultValue_ShouldBeUnknown()
    {
        // Arrange & Act
        var person = new Person
        {
            FirstName = "John",
            LastName = "Doe"
        };

        // Assert
        Assert.Equal(Gender.Unknown, person.Gender);
    }

    [Fact]
    public void Gender_CanBeSet_ToMale()
    {
        // Arrange
        var person = new Person
        {
            FirstName = "John",
            LastName = "Doe",
            Gender = Gender.Male
        };

        // Act & Assert
        Assert.Equal(Gender.Male, person.Gender);
    }

    [Fact]
    public void EmailPreference_DefaultValue_ShouldBeEmailAllowed()
    {
        // Arrange & Act
        var person = new Person
        {
            FirstName = "John",
            LastName = "Doe"
        };

        // Assert
        Assert.Equal(EmailPreference.EmailAllowed, person.EmailPreference);
    }

    [Fact]
    public void IsEmailActive_DefaultValue_ShouldBeTrue()
    {
        // Arrange & Act
        var person = new Person
        {
            FirstName = "John",
            LastName = "Doe"
        };

        // Assert
        Assert.True(person.IsEmailActive);
    }

    [Fact]
    public void IsDeceased_DefaultValue_ShouldBeFalse()
    {
        // Arrange & Act
        var person = new Person
        {
            FirstName = "John",
            LastName = "Doe"
        };

        // Assert
        Assert.False(person.IsDeceased);
    }

    [Fact]
    public void IsSystem_DefaultValue_ShouldBeFalse()
    {
        // Arrange & Act
        var person = new Person
        {
            FirstName = "John",
            LastName = "Doe"
        };

        // Assert
        Assert.False(person.IsSystem);
    }

    [Fact]
    public void IdKey_ShouldBeGenerated_FromId()
    {
        // Arrange
        var person = new Person
        {
            FirstName = "John",
            LastName = "Doe",
            Id = 123
        };

        // Act
        var idKey = person.IdKey;

        // Assert
        Assert.NotNull(idKey);
        Assert.NotEmpty(idKey);
    }
}
