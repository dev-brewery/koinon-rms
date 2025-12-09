using Koinon.Application.Helpers;
using Xunit;

namespace Koinon.Application.Tests.Helpers;

/// <summary>
/// Tests for PhoneNumberHelper.
/// </summary>
public class PhoneNumberHelperTests
{
    #region Normalize Tests

    [Theory]
    [InlineData("(555) 123-4567", "+15551234567")]
    [InlineData("555-123-4567", "+15551234567")]
    [InlineData("555.123.4567", "+15551234567")]
    [InlineData("555 123 4567", "+15551234567")]
    [InlineData("5551234567", "+15551234567")]
    public void Normalize_ShouldConvert_CommonUSFormats_ToE164(string input, string expected)
    {
        // Act
        var result = PhoneNumberHelper.Normalize(input);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Normalize_ShouldAddPlusToUS11DigitNumber_StartingWith1()
    {
        // Arrange
        var input = "15551234567";

        // Act
        var result = PhoneNumberHelper.Normalize(input);

        // Assert
        Assert.Equal("+15551234567", result);
    }

    [Fact]
    public void Normalize_ShouldPreserve_AlreadyE164FormattedNumber()
    {
        // Arrange
        var input = "+15551234567";

        // Act
        var result = PhoneNumberHelper.Normalize(input);

        // Assert
        Assert.Equal("+15551234567", result);
    }

    [Fact]
    public void Normalize_ShouldHandleInternationalNumber_WithPlus()
    {
        // Arrange
        var input = "+442071234567";

        // Act
        var result = PhoneNumberHelper.Normalize(input);

        // Assert
        Assert.Equal("+442071234567", result);
    }

    [Fact]
    public void Normalize_ShouldHandleInternationalNumber_WithoutPlus()
    {
        // Arrange
        var input = "442071234567";

        // Act
        var result = PhoneNumberHelper.Normalize(input);

        // Assert
        Assert.Equal("+442071234567", result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Normalize_ShouldReturnNull_ForEmptyOrWhitespace(string? input)
    {
        // Act
        var result = PhoneNumberHelper.Normalize(input);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Normalize_ShouldReturnNull_ForInputWithNoDigits()
    {
        // Arrange
        var input = "abc-def-ghij";

        // Act
        var result = PhoneNumberHelper.Normalize(input);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Normalize_ShouldStripAllNonDigits_ExceptLeadingPlus()
    {
        // Arrange
        var input = "+1 (555) 123-4567 ext. 890";

        // Act
        var result = PhoneNumberHelper.Normalize(input);

        // Assert
        Assert.Equal("+15551234567890", result);
    }

    #endregion

    #region IsValidE164 Tests

    [Theory]
    [InlineData("+15551234567")]
    [InlineData("+442071234567")]
    [InlineData("+12345678901")]
    [InlineData("+61412345678")]
    public void IsValidE164_ShouldReturnTrue_ForValidE164Numbers(string phone)
    {
        // Act
        var result = PhoneNumberHelper.IsValidE164(phone);

        // Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData("5551234567")]          // Missing +
    [InlineData("0123456789")]          // Starts with 0
    [InlineData("+0123456789")]         // Starts with +0
    [InlineData("+1234567890123456")]   // Too long (16 digits)
    [InlineData("+1")]                  // Too short
    [InlineData("abc")]                 // Not digits
    [InlineData("(555) 123-4567")]      // Not normalized
    public void IsValidE164_ShouldReturnFalse_ForInvalidE164Numbers(string phone)
    {
        // Act
        var result = PhoneNumberHelper.IsValidE164(phone);

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void IsValidE164_ShouldReturnFalse_ForEmptyOrWhitespace(string? phone)
    {
        // Act
        var result = PhoneNumberHelper.IsValidE164(phone);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region NormalizeAndValidate Tests

    [Theory]
    [InlineData("(555) 123-4567", "+15551234567")]
    [InlineData("555-123-4567", "+15551234567")]
    [InlineData("555.123.4567", "+15551234567")]
    [InlineData("+15551234567", "+15551234567")]
    [InlineData("15551234567", "+15551234567")]
    public void NormalizeAndValidate_ShouldReturnNormalizedE164_ForValidInputs(string input, string expected)
    {
        // Act
        var result = PhoneNumberHelper.NormalizeAndValidate(input);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("abc")]
    [InlineData("123")]                         // Too short
    [InlineData("01234567890")]                 // Starts with 0
    public void NormalizeAndValidate_ShouldReturnNull_ForInvalidInputs(string? input)
    {
        // Act
        var result = PhoneNumberHelper.NormalizeAndValidate(input);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void NormalizeAndValidate_ShouldRejectNumber_ThatStartsWith0()
    {
        // Arrange
        var input = "0551234567";

        // Act
        var result = PhoneNumberHelper.NormalizeAndValidate(input);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void NormalizeAndValidate_ShouldRejectNumber_ThatIsTooLong()
    {
        // Arrange - E.164 allows max 15 digits (including country code)
        var input = "12345678901234567"; // 17 digits

        // Act
        var result = PhoneNumberHelper.NormalizeAndValidate(input);

        // Assert
        Assert.Null(result);
    }

    [Theory]
    [InlineData("+44 20 7123 4567", "+442071234567")]
    [InlineData("+61 412 345 678", "+61412345678")]
    [InlineData("+33 1 23 45 67 89", "+33123456789")]
    public void NormalizeAndValidate_ShouldHandleInternationalFormats(string input, string expected)
    {
        // Act
        var result = PhoneNumberHelper.NormalizeAndValidate(input);

        // Assert
        Assert.Equal(expected, result);
    }

    #endregion
}
