namespace Koinon.Application.Interfaces;

/// <summary>
/// Service for calculating a person's current grade based on graduation year.
/// </summary>
public interface IGradeCalculationService
{
    /// <summary>
    /// Calculates the current grade for a person based on their graduation year.
    /// Grade scale: -1 = Pre-K, 0 = Kindergarten, 1 = 1st grade, etc.
    /// Returns null if graduationYear is null.
    /// </summary>
    /// <param name="graduationYear">The person's high school graduation year.</param>
    /// <param name="currentDate">The current date (for testing purposes). Defaults to today.</param>
    /// <returns>The calculated grade, or null if graduationYear is null.</returns>
    /// <remarks>
    /// Values outside the typical -1 to 12 range can be returned:
    /// - Negative values less than -1 indicate children not yet Pre-K age (e.g., -2, -3)
    /// - Values greater than 12 indicate graduated students (e.g., 13, 14, 15)
    /// </remarks>
    int? CalculateGrade(int? graduationYear, DateOnly? currentDate = null);

    /// <summary>
    /// Calculates a person's age in months based on their birth date.
    /// Returns null if birthDate is null.
    /// </summary>
    /// <param name="birthDate">The person's birth date.</param>
    /// <param name="currentDate">The current date (for testing purposes). Defaults to today.</param>
    /// <returns>The age in months, or null if birthDate is null.</returns>
    int? CalculateAgeInMonths(DateOnly? birthDate, DateOnly? currentDate = null);
}
