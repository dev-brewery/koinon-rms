using FluentAssertions;
using Koinon.Domain.Attributes;
using Koinon.Domain.Enums;
using Xunit;

namespace Koinon.Domain.Tests.Attributes;

public class SensitiveDataAttributeTests
{
    // Test class with sensitive properties
    private class TestClass
    {
        [SensitiveData]
        public string? DefaultMasking { get; set; }

        [SensitiveData(MaskType = SensitiveMaskType.Partial)]
        public string? PartialMasking { get; set; }

        [SensitiveData(MaskType = SensitiveMaskType.Hash)]
        public string? HashMasking { get; set; }

        [SensitiveData(MaskType = SensitiveMaskType.Full, Reason = "Contains PII")]
        public string? WithReason { get; set; }
    }

    [Fact]
    public void DefaultMaskType_ShouldBeFull()
    {
        // Arrange & Act
        var attribute = new SensitiveDataAttribute();

        // Assert
        attribute.MaskType.Should().Be(SensitiveMaskType.Full);
    }

    [Fact]
    public void MaskType_CanBeSetToPartial()
    {
        // Arrange & Act
        var attribute = new SensitiveDataAttribute { MaskType = SensitiveMaskType.Partial };

        // Assert
        attribute.MaskType.Should().Be(SensitiveMaskType.Partial);
    }

    [Fact]
    public void MaskType_CanBeSetToHash()
    {
        // Arrange & Act
        var attribute = new SensitiveDataAttribute { MaskType = SensitiveMaskType.Hash };

        // Assert
        attribute.MaskType.Should().Be(SensitiveMaskType.Hash);
    }

    [Fact]
    public void Reason_CanBeSet()
    {
        // Arrange & Act
        var attribute = new SensitiveDataAttribute { Reason = "Contains credit card data" };

        // Assert
        attribute.Reason.Should().Be("Contains credit card data");
    }

    [Fact]
    public void Reason_DefaultsToNull()
    {
        // Arrange & Act
        var attribute = new SensitiveDataAttribute();

        // Assert
        attribute.Reason.Should().BeNull();
    }

    [Fact]
    public void Attribute_CanBeAppliedToProperty()
    {
        // Arrange
        var property = typeof(TestClass).GetProperty(nameof(TestClass.DefaultMasking));

        // Act
        var attribute = property?.GetCustomAttributes(typeof(SensitiveDataAttribute), false)
            .FirstOrDefault() as SensitiveDataAttribute;

        // Assert
        attribute.Should().NotBeNull();
        attribute!.MaskType.Should().Be(SensitiveMaskType.Full);
    }

    [Fact]
    public void Attribute_ReflectsPartialMaskType()
    {
        // Arrange
        var property = typeof(TestClass).GetProperty(nameof(TestClass.PartialMasking));

        // Act
        var attribute = property?.GetCustomAttributes(typeof(SensitiveDataAttribute), false)
            .FirstOrDefault() as SensitiveDataAttribute;

        // Assert
        attribute.Should().NotBeNull();
        attribute!.MaskType.Should().Be(SensitiveMaskType.Partial);
    }

    [Fact]
    public void Attribute_ReflectsHashMaskType()
    {
        // Arrange
        var property = typeof(TestClass).GetProperty(nameof(TestClass.HashMasking));

        // Act
        var attribute = property?.GetCustomAttributes(typeof(SensitiveDataAttribute), false)
            .FirstOrDefault() as SensitiveDataAttribute;

        // Assert
        attribute.Should().NotBeNull();
        attribute!.MaskType.Should().Be(SensitiveMaskType.Hash);
    }

    [Fact]
    public void Attribute_ReflectsReason()
    {
        // Arrange
        var property = typeof(TestClass).GetProperty(nameof(TestClass.WithReason));

        // Act
        var attribute = property?.GetCustomAttributes(typeof(SensitiveDataAttribute), false)
            .FirstOrDefault() as SensitiveDataAttribute;

        // Assert
        attribute.Should().NotBeNull();
        attribute!.Reason.Should().Be("Contains PII");
    }

    [Fact]
    public void AttributeUsage_RestrictedToProperty()
    {
        // Arrange
        var attributeUsage = typeof(SensitiveDataAttribute)
            .GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .FirstOrDefault() as AttributeUsageAttribute;

        // Assert
        attributeUsage.Should().NotBeNull();
        attributeUsage!.ValidOn.Should().Be(AttributeTargets.Property);
    }
}
