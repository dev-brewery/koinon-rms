using FluentAssertions;
using Koinon.Domain.Data;
using Xunit;

namespace Koinon.Api.ContractTests;

/// <summary>
/// Tests IdKey encoding/decoding contract.
/// Ensures IdKey format is consistent and reversible.
/// </summary>
public class IdKeyContractTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(42)]
    [InlineData(100)]
    [InlineData(1000)]
    [InlineData(999999)]
    [InlineData(int.MaxValue)]
    public void IdKey_Encode_ProducesValidString(int id)
    {
        // Act
        var idKey = IdKeyHelper.Encode(id);

        // Assert
        idKey.Should().NotBeNullOrEmpty();
        idKey.Should().NotContain("+"); // URL-safe
        idKey.Should().NotContain("/"); // URL-safe
        idKey.Should().NotContain("="); // No padding
    }

    [Theory]
    [InlineData(1)]
    [InlineData(42)]
    [InlineData(100)]
    [InlineData(1000)]
    [InlineData(999999)]
    [InlineData(int.MaxValue)]
    public void IdKey_RoundTrip_PreservesOriginalValue(int originalId)
    {
        // Act
        var encoded = IdKeyHelper.Encode(originalId);
        var decoded = IdKeyHelper.Decode(encoded);

        // Assert
        decoded.Should().Be(originalId);
    }

    [Fact]
    public void IdKey_Decode_ThrowsOnNullOrEmpty()
    {
        // Arrange & Act & Assert
        var act1 = () => IdKeyHelper.Decode(null!);
        var act2 = () => IdKeyHelper.Decode("");
        var act3 = () => IdKeyHelper.Decode("   ");

        act1.Should().Throw<ArgumentException>();
        act2.Should().Throw<ArgumentException>();
        act3.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("@@@")]
    [InlineData("not-valid-base64")]
    public void IdKey_Decode_ThrowsOnInvalidFormat(string invalidIdKey)
    {
        // Act & Assert
        var act = () => IdKeyHelper.Decode(invalidIdKey);
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Invalid IdKey format*");
    }

    [Fact]
    public void IdKey_TryDecode_ReturnsFalseOnInvalid()
    {
        // Act
        var result = IdKeyHelper.TryDecode("invalid", out var id);

        // Assert
        result.Should().BeFalse();
        id.Should().Be(0);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(42)]
    [InlineData(100)]
    public void IdKey_TryDecode_ReturnsTrueOnValid(int originalId)
    {
        // Arrange
        var encoded = IdKeyHelper.Encode(originalId);

        // Act
        var result = IdKeyHelper.TryDecode(encoded, out var decodedId);

        // Assert
        result.Should().BeTrue();
        decodedId.Should().Be(originalId);
    }

    [Fact]
    public void IdKey_Format_IsUrlSafe()
    {
        // Arrange
        var ids = new[] { 1, 42, 100, 1000, 999999, int.MaxValue };

        foreach (var id in ids)
        {
            // Act
            var idKey = IdKeyHelper.Encode(id);

            // Assert - should only contain URL-safe base64 characters
            idKey.Should().MatchRegex("^[A-Za-z0-9_-]+$",
                because: "IdKey should only contain URL-safe base64 characters");
        }
    }

    [Fact]
    public void IdKey_Zero_HandledCorrectly()
    {
        // Act
        var encoded = IdKeyHelper.Encode(0);
        var decoded = IdKeyHelper.Decode(encoded);

        // Assert
        encoded.Should().NotBeNullOrEmpty();
        decoded.Should().Be(0);
    }

    [Fact]
    public void IdKey_NegativeNumber_HandledCorrectly()
    {
        // Act
        var encoded = IdKeyHelper.Encode(-1);
        var decoded = IdKeyHelper.Decode(encoded);

        // Assert
        encoded.Should().NotBeNullOrEmpty();
        decoded.Should().Be(-1);
    }
}
