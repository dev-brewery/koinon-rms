namespace Koinon.Application.Services.Common;

/// <summary>
/// Helper class for calculating school grade from graduation year.
/// Used in check-in kiosk to display current grade for school-age children.
/// </summary>
public static class GradeCalculationHelper
{
    /// <summary>
    /// Calculates current school grade from graduation year.
    /// School year runs August to June, with graduation typically in June.
    /// </summary>
    /// <param name="graduationYear">Expected year of high school graduation</param>
    /// <returns>Grade string (e.g., "5th Grade", "Kindergarten") or null if not applicable</returns>
    /// <example>
    /// If today is September 2025 and graduationYear is 2030:
    /// - Current school year is 2025-2026
    /// - Years until graduation: 2030 - 2026 = 4
    /// - Grade: 8th Grade (4 years before 12th)
    /// </example>
    public static string? CalculateGrade(int? graduationYear)
    {
        if (graduationYear == null)
        {
            return null;
        }

        var currentYear = DateTime.Today.Year;
        var currentMonth = DateTime.Today.Month;

        // School year starts in August, so after August we're in the new school year
        // Example: In September 2025, we're in school year 2025-2026
        var schoolYear = currentMonth >= 8 ? currentYear + 1 : currentYear;
        var yearsUntilGraduation = graduationYear.Value - schoolYear;

        return yearsUntilGraduation switch
        {
            < 0 => "Graduated",
            0 => "12th Grade",
            1 => "11th Grade",
            2 => "10th Grade",
            3 => "9th Grade",
            4 => "8th Grade",
            5 => "7th Grade",
            6 => "6th Grade",
            7 => "5th Grade",
            8 => "4th Grade",
            9 => "3rd Grade",
            10 => "2nd Grade",
            11 => "1st Grade",
            12 => "Kindergarten",
            13 => "Pre-K",
            _ => null // Too young or invalid data
        };
    }
}
