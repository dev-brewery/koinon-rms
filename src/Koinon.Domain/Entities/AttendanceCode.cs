using System.ComponentModel.DataAnnotations;

namespace Koinon.Domain.Entities;

/// <summary>
/// Represents a security code printed on check-in labels.
/// Codes are unique per day and used to securely pick up children from children's ministry.
/// </summary>
public class AttendanceCode : Entity
{
    /// <summary>
    /// The date and time when this code was issued.
    /// </summary>
    public required DateTime IssueDateTime { get; set; }

    /// <summary>
    /// The date portion of IssueDateTime (for enforcing daily uniqueness).
    /// This is automatically set from IssueDateTime.
    /// </summary>
    public DateOnly IssueDate { get; set; }

    /// <summary>
    /// The actual security code (typically 3-4 alphanumeric characters).
    /// </summary>
    [MaxLength(10)]
    public required string Code { get; set; }

    // Navigation Properties

    /// <summary>
    /// Collection of attendance records using this code.
    /// </summary>
    public virtual ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();
}
