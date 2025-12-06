using Koinon.Domain.Entities;
using Xunit;

namespace Koinon.Domain.Tests.Entities;

/// <summary>
/// Unit tests for the PersonAlias entity.
/// </summary>
public class PersonAliasTests
{
    [Fact]
    public void PersonAlias_Creation_ShouldSetRequiredProperties()
    {
        // Arrange & Act
        var personAlias = new PersonAlias
        {
            PersonId = 123
        };

        // Assert
        Assert.Equal(123, personAlias.PersonId);
        Assert.NotEqual(Guid.Empty, personAlias.Guid); // Should auto-generate
    }

    [Fact]
    public void PersonAlias_WithName_ShouldStoreAlternateName()
    {
        // Arrange & Act
        var personAlias = new PersonAlias
        {
            PersonId = 123,
            Name = "John Smith (formerly John Doe)"
        };

        // Assert
        Assert.Equal("John Smith (formerly John Doe)", personAlias.Name);
    }

    [Fact]
    public void PersonAlias_WithAliasPersonId_ShouldTrackMergedPerson()
    {
        // Arrange
        var mergedFromGuid = Guid.NewGuid();

        // Act
        var personAlias = new PersonAlias
        {
            PersonId = 123, // Current person
            AliasPersonId = 456, // Merged from person
            AliasPersonGuid = mergedFromGuid
        };

        // Assert
        Assert.Equal(456, personAlias.AliasPersonId);
        Assert.Equal(mergedFromGuid, personAlias.AliasPersonGuid);
    }

    [Fact]
    public void PersonAlias_IdKey_ShouldBeGenerated_FromId()
    {
        // Arrange
        var personAlias = new PersonAlias
        {
            PersonId = 123,
            Id = 999
        };

        // Act
        var idKey = personAlias.IdKey;

        // Assert
        Assert.NotNull(idKey);
        Assert.NotEmpty(idKey);
    }
}
