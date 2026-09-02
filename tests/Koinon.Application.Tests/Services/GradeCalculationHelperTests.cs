using FluentAssertions;
using Koinon.Application.Services.Common;
using Xunit;

namespace Koinon.Application.Tests.Services;

public class GradeCalculationHelperTests
{
    [Fact]
    public void CalculateGrade_WithNullGraduationYear_ReturnsNull()
    {
        // Act
        var result = GradeCalculationHelper.CalculateGrade(null);

        // Assert
        result.Should().BeNull();
    }

    [Theory]
    [InlineData(1, 2025, "12th Grade")] // Graduates this school year
    [InlineData(2, 2025, "11th Grade")]
    [InlineData(3, 2025, "10th Grade")]
    [InlineData(4, 2025, "9th Grade")]
    [InlineData(5, 2025, "8th Grade")]
    [InlineData(6, 2025, "7th Grade")]
    [InlineData(7, 2025, "6th Grade")]
    [InlineData(8, 2025, "5th Grade")]
    [InlineData(9, 2025, "4th Grade")]
    [InlineData(10, 2025, "3rd Grade")]
    [InlineData(11, 2025, "2nd Grade")]
    [InlineData(12, 2025, "1st Grade")]
    [InlineData(13, 2025, "Kindergarten")]
    [InlineData(14, 2025, "Pre-K")]
    public void CalculateGrade_DuringSchoolYear_ReturnsCorrectGrade(int monthsInFuture, int currentYear, string expectedGrade)
    {
        // Fixed date: September of currentYear — deterministic, never touches the wall clock
        // Arrange
        var today = new DateOnly(currentYear, 9, 15);
        var schoolYear = today.Month >= 8 ? today.Year + 1 : today.Year;
        var graduationYear = schoolYear + (monthsInFuture - 1);

        // Act
        var result = GradeCalculationHelper.CalculateGrade(graduationYear, today);

        // Assert
        result.Should().Be(expectedGrade);
    }

    [Fact]
    public void CalculateGrade_AlreadyGraduated_ReturnsGraduated()
    {
        // Arrange
        var today = new DateOnly(2025, 9, 15);
        var graduationYear = 2024; // Last year

        // Act
        var result = GradeCalculationHelper.CalculateGrade(graduationYear, today);

        // Assert
        result.Should().Be("Graduated");
    }

    [Fact]
    public void CalculateGrade_TooYoung_ReturnsNull()
    {
        // Arrange - someone who would graduate 20 years from now (too young for Pre-K)
        var today = new DateOnly(2025, 9, 15); // school year 2026
        var graduationYear = 2026 + 20;

        // Act
        var result = GradeCalculationHelper.CalculateGrade(graduationYear, today);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void CalculateGrade_BeforeAugust_UsesCurrentYearAsSchoolYear()
    {
        // July 2025: still school year 2024-2025; graduating 2025 = senior
        var result = GradeCalculationHelper.CalculateGrade(2025, new DateOnly(2025, 7, 15));
        result.Should().Be("12th Grade");
    }

    [Fact]
    public void CalculateGrade_InAugust_UsesNextYearAsSchoolYear()
    {
        // August 1, 2025: school year 2025-2026 begins; graduating 2026 = senior.
        // This pins the Month >= 8 boundary — if the cutoff regressed to >= 9,
        // the result would be "11th Grade" and this test fails.
        var result = GradeCalculationHelper.CalculateGrade(2026, new DateOnly(2025, 8, 1));
        result.Should().Be("12th Grade");
    }

    [Fact]
    public void CalculateGrade_InSeptember_UsesNextYearAsSchoolYear()
    {
        // September 2025: school year 2025-2026; graduating 2026 = senior
        var result = GradeCalculationHelper.CalculateGrade(2026, new DateOnly(2025, 9, 15));
        result.Should().Be("12th Grade");
    }

    [Theory]
    [InlineData(2025, 9, 2026, "12th Grade")] // September 2025, graduates 2026
    [InlineData(2025, 9, 2030, "8th Grade")]  // September 2025, graduates 2030 (4 years away)
    [InlineData(2025, 7, 2025, "12th Grade")] // July 2025, graduates 2025 (still in school year 2024-2025)
    [InlineData(2025, 7, 2029, "8th Grade")]  // July 2025, graduates 2029 (4 years from school year 2024-2025)
    public void CalculateGrade_WithSpecificDates_ReturnsCorrectGrade(
        int currentYear,
        int currentMonth,
        int graduationYear,
        string expectedGrade)
    {
        // Fixed-date overload makes this a real production-code assertion
        // (the previous version re-implemented the switch and never called the helper).
        var result = GradeCalculationHelper.CalculateGrade(
            graduationYear, new DateOnly(currentYear, currentMonth, 15));
        result.Should().Be(expectedGrade);
    }
}
