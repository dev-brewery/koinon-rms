using FluentAssertions;
using Koinon.Domain.Data;
using Koinon.Domain.Entities;
using Xunit;

namespace Koinon.Domain.Tests.Entities;

public class EntityTests
{
    // Test entity for testing the abstract Entity base class
    private class TestEntity : Entity
    {
        public string? TestProperty { get; set; }
    }

    [Fact]
    public void Constructor_InitializesGuidAutomatically()
    {
        // Act
        var entity = new TestEntity();

        // Assert
        entity.Guid.Should().NotBeEmpty();
    }

    [Fact]
    public void Constructor_GeneratesUniqueGuidsForDifferentInstances()
    {
        // Act
        var entity1 = new TestEntity();
        var entity2 = new TestEntity();

        // Assert
        entity1.Guid.Should().NotBe(entity2.Guid);
    }

    [Fact]
    public void IdKey_WhenIdIsSet_ReturnsEncodedValue()
    {
        // Arrange
        var entity = new TestEntity { Id = 12345 };

        // Act
        string idKey = entity.IdKey;

        // Assert
        idKey.Should().NotBeNullOrEmpty();
        idKey.Should().Be(IdKeyHelper.Encode(12345));
    }

    [Fact]
    public void IdKey_WhenIdChanges_ReturnsUpdatedEncodedValue()
    {
        // Arrange
        var entity = new TestEntity { Id = 100 };
        string firstIdKey = entity.IdKey;

        // Act
        entity.Id = 200;
        string secondIdKey = entity.IdKey;

        // Assert
        firstIdKey.Should().NotBe(secondIdKey);
        secondIdKey.Should().Be(IdKeyHelper.Encode(200));
    }

    [Fact]
    public void IdKey_CanBeDecodedBackToOriginalId()
    {
        // Arrange
        var entity = new TestEntity { Id = 9999 };

        // Act
        string idKey = entity.IdKey;
        int decodedId = IdKeyHelper.Decode(idKey);

        // Assert
        decodedId.Should().Be(9999);
    }

    [Fact]
    public void Entity_ImplementsIEntity()
    {
        // Arrange
        var entity = new TestEntity();

        // Assert
        entity.Should().BeAssignableTo<IEntity>();
    }

    [Fact]
    public void Entity_ImplementsIAuditable()
    {
        // Arrange
        var entity = new TestEntity();

        // Assert
        entity.Should().BeAssignableTo<IAuditable>();
    }

    [Fact]
    public void AuditProperties_CanBeSet()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var entity = new TestEntity
        {
            CreatedDateTime = now,
            ModifiedDateTime = now.AddMinutes(5),
            CreatedByPersonAliasId = 1,
            ModifiedByPersonAliasId = 2
        };

        // Assert
        entity.CreatedDateTime.Should().Be(now);
        entity.ModifiedDateTime.Should().Be(now.AddMinutes(5));
        entity.CreatedByPersonAliasId.Should().Be(1);
        entity.ModifiedByPersonAliasId.Should().Be(2);
    }

    [Fact]
    public void ModifiedDateTime_CanBeNull()
    {
        // Arrange
        var entity = new TestEntity
        {
            ModifiedDateTime = null
        };

        // Assert
        entity.ModifiedDateTime.Should().BeNull();
    }

    [Fact]
    public void CreatedByPersonAliasId_CanBeNull()
    {
        // Arrange
        var entity = new TestEntity
        {
            CreatedByPersonAliasId = null
        };

        // Assert
        entity.CreatedByPersonAliasId.Should().BeNull();
    }

    [Fact]
    public void ModifiedByPersonAliasId_CanBeNull()
    {
        // Arrange
        var entity = new TestEntity
        {
            ModifiedByPersonAliasId = null
        };

        // Assert
        entity.ModifiedByPersonAliasId.Should().BeNull();
    }
}
