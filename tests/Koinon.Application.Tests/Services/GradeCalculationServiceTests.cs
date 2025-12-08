using FluentAssertions;
using Koinon.Application.Services;
using Xunit;

namespace Koinon.Application.Tests.Services;

public class GradeCalculationServiceTests
{
    private readonly GradeCalculationService _service;

    public GradeCalculationServiceTests()
    {
        _service = new GradeCalculationService();
    }

    #region CalculateGrade Tests

    [Fact]
    public void CalculateGrade_WithNullGraduationYear_ReturnsNull()
    {
        // Act
        var result = _service.CalculateGrade(null);

        // Assert
        result.Should().BeNull();
    }

    [Theory]
    [InlineData(2025, 9, 1, 2026, 12)] // September 1, 2025 -> school year 2025, graduating 2026 = 12th grade
    [InlineData(2025, 9, 1, 2027, 11)] // September 1, 2025 -> school year 2025, graduating 2027 = 11th grade
    [InlineData(2025, 9, 1, 2030, 8)]  // September 1, 2025 -> school year 2025, graduating 2030 = 8th grade
    [InlineData(2025, 9, 1, 2037, 1)]  // September 1, 2025 -> school year 2025, graduating 2037 = 1st grade
    [InlineData(2025, 9, 1, 2038, 0)]  // September 1, 2025 -> school year 2025, graduating 2038 = Kindergarten
    [InlineData(2025, 9, 1, 2039, -1)] // September 1, 2025 -> school year 2025, graduating 2039 = Pre-K
    public void CalculateGrade_AfterSeptember1_ReturnsCorrectGrade(
        int year, int month, int day, int graduationYear, int expectedGrade)
    {
        // Arrange
        var currentDate = new DateOnly(year, month, day);

        // Act
        var result = _service.CalculateGrade(graduationYear, currentDate);

        // Assert
        result.Should().Be(expectedGrade);
    }

    [Theory]
    [InlineData(2025, 8, 31, 2025, 12)] // August 31, 2025 -> school year 2024, graduating 2025 = 12th grade
    [InlineData(2025, 8, 31, 2026, 11)] // August 31, 2025 -> school year 2024, graduating 2026 = 11th grade
    [InlineData(2025, 7, 15, 2025, 12)] // July 15, 2025 -> school year 2024, graduating 2025 = 12th grade
    [InlineData(2025, 1, 1, 2025, 12)]  // January 1, 2025 -> school year 2024, graduating 2025 = 12th grade
    public void CalculateGrade_BeforeSeptember1_ReturnsCorrectGrade(
        int year, int month, int day, int graduationYear, int expectedGrade)
    {
        // Arrange
        var currentDate = new DateOnly(year, month, day);

        // Act
        var result = _service.CalculateGrade(graduationYear, currentDate);

        // Assert
        result.Should().Be(expectedGrade);
    }

    [Theory]
    [InlineData(2025, 9, 1, 2025, 13)]  // Already graduated (grade > 12)
    [InlineData(2025, 9, 1, 2024, 14)]  // Graduated last year
    [InlineData(2025, 9, 1, 2040, -2)]  // Too young (grade < -1)
    public void CalculateGrade_EdgeCases_ReturnsCalculatedValue(
        int year, int month, int day, int graduationYear, int expectedGrade)
    {
        // Arrange - The service calculates grades even for edge cases
        var currentDate = new DateOnly(year, month, day);

        // Act
        var result = _service.CalculateGrade(graduationYear, currentDate);

        // Assert
        result.Should().Be(expectedGrade);
    }

    [Fact]
    public void CalculateGrade_WithoutCurrentDate_UsesToday()
    {
        // Arrange
        var currentYear = DateTime.UtcNow.Year;
        var currentMonth = DateTime.UtcNow.Month;
        var schoolYear = currentMonth >= 9 ? currentYear + 1 : currentYear;
        var graduationYear = schoolYear + 4; // 4 years from school year end = 8th grade

        // Act
        var result = _service.CalculateGrade(graduationYear);

        // Assert
        result.Should().Be(8);
    }

    #endregion

    #region CalculateAgeInMonths Tests

    [Fact]
    public void CalculateAgeInMonths_WithNullBirthDate_ReturnsNull()
    {
        // Act
        var result = _service.CalculateAgeInMonths(null);

        // Assert
        result.Should().BeNull();
    }

    [Theory]
    [InlineData(2020, 1, 1, 2025, 1, 1, 60)]   // Exactly 5 years = 60 months
    [InlineData(2020, 1, 1, 2025, 2, 1, 61)]   // 5 years 1 month = 61 months
    [InlineData(2020, 1, 15, 2025, 1, 15, 60)] // Exactly 5 years = 60 months
    [InlineData(2020, 1, 15, 2025, 1, 14, 59)] // One day short of 5 years = 59 months
    [InlineData(2024, 12, 1, 2025, 1, 1, 1)]   // 1 month old
    [InlineData(2025, 1, 1, 2025, 1, 15, 0)]   // Less than 1 month old
    public void CalculateAgeInMonths_ReturnsCorrectAge(
        int birthYear, int birthMonth, int birthDay,
        int currentYear, int currentMonth, int currentDay,
        int expectedMonths)
    {
        // Arrange
        var birthDate = new DateOnly(birthYear, birthMonth, birthDay);
        var currentDate = new DateOnly(currentYear, currentMonth, currentDay);

        // Act
        var result = _service.CalculateAgeInMonths(birthDate, currentDate);

        // Assert
        result.Should().Be(expectedMonths);
    }

    [Fact]
    public void CalculateAgeInMonths_WithoutCurrentDate_UsesToday()
    {
        // Arrange
        var birthDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-5));

        // Act
        var result = _service.CalculateAgeInMonths(birthDate);

        // Assert
        result.Should().BeGreaterOrEqualTo(59); // At least 59 months (almost 5 years)
        result.Should().BeLessThanOrEqualTo(61); // At most 61 months (just over 5 years)
    }

    [Fact]
    public void CalculateAgeInMonths_WhenBirthDateIsInFuture_ReturnsZero()
    {
        // Arrange
        var birthDate = new DateOnly(2030, 1, 1);
        var currentDate = new DateOnly(2025, 1, 1);

        // Act
        var result = _service.CalculateAgeInMonths(birthDate, currentDate);

        // Assert
        result.Should().Be(0); // Age should never be negative
    }

    [Theory]
    [InlineData(2020, 1, 31, 2025, 1, 30, 59)]  // Born on 31st, current is 30th
    [InlineData(2020, 1, 31, 2025, 2, 28, 60)]  // Born on 31st, current is Feb 28
    [InlineData(2020, 2, 29, 2025, 2, 28, 59)]  // Leap year baby
    public void CalculateAgeInMonths_HandlesEdgeCaseDays(
        int birthYear, int birthMonth, int birthDay,
        int currentYear, int currentMonth, int currentDay,
        int expectedMonths)
    {
        // Arrange
        var birthDate = new DateOnly(birthYear, birthMonth, birthDay);
        var currentDate = new DateOnly(currentYear, currentMonth, currentDay);

        // Act
        var result = _service.CalculateAgeInMonths(birthDate, currentDate);

        // Assert
        result.Should().Be(expectedMonths);
    }

    #endregion
}
