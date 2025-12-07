using FluentAssertions;
using Koinon.Domain.Data;
using Xunit;

namespace Koinon.Domain.Tests.Data;

public class IdKeyHelperTests
{
    [Fact]
    public void Encode_WithPositiveId_ReturnsUrlSafeBase64String()
    {
        // Arrange
        int id = 12345;

        // Act
        string idKey = IdKeyHelper.Encode(id);

        // Assert
        idKey.Should().NotBeNullOrEmpty();
        idKey.Should().NotContain("+");
        idKey.Should().NotContain("/");
        idKey.Should().NotContain("=");
    }

    [Fact]
    public void Encode_WithZero_ReturnsValidIdKey()
    {
        // Arrange
        int id = 0;

        // Act
        string idKey = IdKeyHelper.Encode(id);

        // Assert
        idKey.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Encode_WithNegativeId_ReturnsValidIdKey()
    {
        // Arrange
        int id = -42;

        // Act
        string idKey = IdKeyHelper.Encode(id);

        // Assert
        idKey.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Decode_WithValidIdKey_ReturnsOriginalId()
    {
        // Arrange
        string idKey = IdKeyHelper.Encode(12345);

        // Act
        int id = IdKeyHelper.Decode(idKey);

        // Assert
        id.Should().Be(12345);
    }

    [Fact]
    public void Decode_WithNullIdKey_ThrowsArgumentException()
    {
        // Act & Assert
        Action act = () => IdKeyHelper.Decode(null!);
        act.Should().Throw<ArgumentException>()
            .WithMessage("*IdKey cannot be null or empty*");
    }

    [Fact]
    public void Decode_WithEmptyIdKey_ThrowsArgumentException()
    {
        // Act & Assert
        Action act = () => IdKeyHelper.Decode(string.Empty);
        act.Should().Throw<ArgumentException>()
            .WithMessage("*IdKey cannot be null or empty*");
    }

    [Fact]
    public void Decode_WithInvalidIdKey_ThrowsArgumentException()
    {
        // Act & Assert
        Action act = () => IdKeyHelper.Decode("invalid!@#$");
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Invalid IdKey format*");
    }

    [Fact]
    public void TryDecode_WithValidIdKey_ReturnsTrueAndDecodedId()
    {
        // Arrange
        string idKey = IdKeyHelper.Encode(9999);

        // Act
        bool result = IdKeyHelper.TryDecode(idKey, out int id);

        // Assert
        result.Should().BeTrue();
        id.Should().Be(9999);
    }

    [Fact]
    public void TryDecode_WithNullIdKey_ReturnsFalse()
    {
        // Act
        bool result = IdKeyHelper.TryDecode(null, out int id);

        // Assert
        result.Should().BeFalse();
        id.Should().Be(0);
    }

    [Fact]
    public void TryDecode_WithEmptyIdKey_ReturnsFalse()
    {
        // Act
        bool result = IdKeyHelper.TryDecode(string.Empty, out int id);

        // Assert
        result.Should().BeFalse();
        id.Should().Be(0);
    }

    [Fact]
    public void TryDecode_WithInvalidIdKey_ReturnsFalse()
    {
        // Act
        bool result = IdKeyHelper.TryDecode("invalid!@#$", out int id);

        // Assert
        result.Should().BeFalse();
        id.Should().Be(0);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    [InlineData(1000)]
    [InlineData(10000)]
    [InlineData(100000)]
    [InlineData(int.MaxValue)]
    [InlineData(-1)]
    [InlineData(-100)]
    [InlineData(int.MinValue)]
    public void RoundTrip_VariousIds_ReturnsOriginalValue(int originalId)
    {
        // Act
        string idKey = IdKeyHelper.Encode(originalId);
        int decodedId = IdKeyHelper.Decode(idKey);

        // Assert
        decodedId.Should().Be(originalId);
    }

    [Fact]
    public void Encode_DifferentIds_ProducesDifferentIdKeys()
    {
        // Arrange
        int id1 = 100;
        int id2 = 200;

        // Act
        string idKey1 = IdKeyHelper.Encode(id1);
        string idKey2 = IdKeyHelper.Encode(id2);

        // Assert
        idKey1.Should().NotBe(idKey2);
    }

    [Fact]
    public void Encode_SameId_ProducesSameIdKey()
    {
        // Arrange
        int id = 12345;

        // Act
        string idKey1 = IdKeyHelper.Encode(id);
        string idKey2 = IdKeyHelper.Encode(id);

        // Assert
        idKey1.Should().Be(idKey2);
    }

    // Hook test - intentional change
}
