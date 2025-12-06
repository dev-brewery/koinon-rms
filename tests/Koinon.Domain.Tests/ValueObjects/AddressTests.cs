using Koinon.Domain.ValueObjects;

namespace Koinon.Domain.Tests.ValueObjects;

public class AddressTests
{
    [Fact]
    public void Constructor_WithAllFields_SetsPropertiesCorrectly()
    {
        // Arrange & Act
        var address = new Address(
            Street1: "123 Main St",
            Street2: "Apt 4B",
            City: "Springfield",
            State: "IL",
            PostalCode: "62701",
            Country: "USA");

        // Assert
        Assert.Equal("123 Main St", address.Street1);
        Assert.Equal("Apt 4B", address.Street2);
        Assert.Equal("Springfield", address.City);
        Assert.Equal("IL", address.State);
        Assert.Equal("62701", address.PostalCode);
        Assert.Equal("USA", address.Country);
    }

    [Fact]
    public void FormattedAddress_WithFullAddress_ReturnsMultilineString()
    {
        // Arrange
        var address = new Address(
            Street1: "123 Main St",
            Street2: "Apt 4B",
            City: "Springfield",
            State: "IL",
            PostalCode: "62701",
            Country: "USA");

        // Act
        var formatted = address.FormattedAddress;

        // Assert
        var expected = string.Join(Environment.NewLine,
            "123 Main St",
            "Apt 4B",
            "Springfield, IL 62701",
            "USA");
        Assert.Equal(expected, formatted);
    }

    [Fact]
    public void FormattedAddress_WithoutStreet2_OmitsEmptyLine()
    {
        // Arrange
        var address = new Address(
            Street1: "123 Main St",
            Street2: null,
            City: "Springfield",
            State: "IL",
            PostalCode: "62701",
            Country: null);

        // Act
        var formatted = address.FormattedAddress;

        // Assert
        var expected = string.Join(Environment.NewLine,
            "123 Main St",
            "Springfield, IL 62701");
        Assert.Equal(expected, formatted);
    }

    [Fact]
    public void FormattedAddress_WithCityOnly_ReturnsCity()
    {
        // Arrange
        var address = new Address(
            Street1: null,
            Street2: null,
            City: "Springfield",
            State: null,
            PostalCode: null,
            Country: null);

        // Act
        var formatted = address.FormattedAddress;

        // Assert
        Assert.Equal("Springfield", formatted);
    }

    [Fact]
    public void FormattedAddress_WithCityAndState_CombinesProperly()
    {
        // Arrange
        var address = new Address(
            Street1: null,
            Street2: null,
            City: "Springfield",
            State: "IL",
            PostalCode: null,
            Country: null);

        // Act
        var formatted = address.FormattedAddress;

        // Assert
        Assert.Equal("Springfield, IL", formatted);
    }

    [Fact]
    public void FormattedAddress_WithEmptyFields_ReturnsEmptyString()
    {
        // Arrange
        var address = new Address(
            Street1: null,
            Street2: null,
            City: null,
            State: null,
            PostalCode: null,
            Country: null);

        // Act
        var formatted = address.FormattedAddress;

        // Assert
        Assert.Equal(string.Empty, formatted);
    }

    [Fact]
    public void FormattedAddress_WithWhitespaceFields_IgnoresWhitespace()
    {
        // Arrange
        var address = new Address(
            Street1: "  ",
            Street2: "\t",
            City: "Springfield",
            State: "  ",
            PostalCode: null,
            Country: null);

        // Act
        var formatted = address.FormattedAddress;

        // Assert
        Assert.Equal("Springfield", formatted);
    }

    [Fact]
    public void FormattedAddress_WithOnlyPostalCode_ReturnsPostalCode()
    {
        // Arrange
        var address = new Address(
            Street1: null,
            Street2: null,
            City: null,
            State: null,
            PostalCode: "62701",
            Country: null);

        // Act
        var formatted = address.FormattedAddress;

        // Assert
        Assert.Equal("62701", formatted);
    }

    [Fact]
    public void Empty_ReturnsAddressWithAllNullFields()
    {
        // Act
        var address = Address.Empty;

        // Assert
        Assert.Null(address.Street1);
        Assert.Null(address.Street2);
        Assert.Null(address.City);
        Assert.Null(address.State);
        Assert.Null(address.PostalCode);
        Assert.Null(address.Country);
    }

    [Fact]
    public void RecordEquality_WithSameValues_AreEqual()
    {
        // Arrange
        var address1 = new Address("123 Main St", null, "Springfield", "IL", "62701", "USA");
        var address2 = new Address("123 Main St", null, "Springfield", "IL", "62701", "USA");

        // Act & Assert
        Assert.Equal(address1, address2);
        Assert.True(address1 == address2);
    }

    [Fact]
    public void RecordEquality_WithDifferentValues_AreNotEqual()
    {
        // Arrange
        var address1 = new Address("123 Main St", null, "Springfield", "IL", "62701", "USA");
        var address2 = new Address("456 Oak Ave", null, "Springfield", "IL", "62701", "USA");

        // Act & Assert
        Assert.NotEqual(address1, address2);
        Assert.True(address1 != address2);
    }
}
