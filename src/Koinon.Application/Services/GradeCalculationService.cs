using Koinon.Application.Interfaces;

namespace Koinon.Application.Services;

/// <summary>
/// Service for calculating a person's current grade based on graduation year.
/// Uses a September 1st cutoff date for grade transitions.
/// </summary>
public class GradeCalculationService : IGradeCalculationService
{
    /// <summary>
    /// The month when the school year starts (September = 9).
    /// </summary>
    private const int SchoolYearStartMonth = 9;

    /// <summary>
    /// The day of the month when the school year starts.
    /// </summary>
    private const int SchoolYearStartDay = 1;

    public int? CalculateGrade(int? graduationYear, DateOnly? currentDate = null)
    {
        if (!graduationYear.HasValue)
        {
            return null;
        }

        currentDate ??= DateOnly.FromDateTime(DateTime.UtcNow);

        // Calculate the graduation year of current seniors
        // If we're before September 1, we're still in the previous school year
        // School year ends in the following June
        // e.g., September 2025 = 2025-2026 school year (ends in 2026)
        var graduationYearOfCurrentSeniors = currentDate.Value.Month >= SchoolYearStartMonth
            ? currentDate.Value.Year + 1
            : currentDate.Value.Year;

        // Calculate years until graduation
        var yearsUntilGraduation = graduationYear.Value - graduationYearOfCurrentSeniors;

        // Grade = 12 - years until graduation
        // Example: If graduating in 2025 and it's the 2024 school year,
        // yearsUntilGraduation = 1, so grade = 12 - 1 = 11 (senior year)
        var grade = 12 - yearsUntilGraduation;

        // Grade can be negative for pre-K (typically -1) or higher than 12 for graduated students
        return grade;
    }

    public int? CalculateAgeInMonths(DateOnly? birthDate, DateOnly? currentDate = null)
    {
        if (!birthDate.HasValue)
        {
            return null;
        }

        currentDate ??= DateOnly.FromDateTime(DateTime.UtcNow);

        // Calculate total months between birth date and current date
        var years = currentDate.Value.Year - birthDate.Value.Year;
        var months = currentDate.Value.Month - birthDate.Value.Month;

        // Adjust for day of month
        if (currentDate.Value.Day < birthDate.Value.Day)
        {
            months--;
        }

        var totalMonths = (years * 12) + months;

        // Age should never be negative
        return totalMonths >= 0 ? totalMonths : 0;
    }
}
