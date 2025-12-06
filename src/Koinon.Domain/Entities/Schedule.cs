using System.ComponentModel.DataAnnotations;

namespace Koinon.Domain.Entities;

/// <summary>
/// Defines when something occurs (service times, group meetings, check-in availability).
/// Supports both simple weekly schedules and complex iCalendar recurrence rules.
/// </summary>
public class Schedule : Entity
{
    /// <summary>
    /// The name of this schedule.
    /// </summary>
    [MaxLength(50)]
    public required string Name { get; set; }

    /// <summary>
    /// Optional description of this schedule.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// iCalendar content string (RRULE) for complex recurrence patterns.
    /// </summary>
    public string? ICalendarContent { get; set; }

    /// <summary>
    /// Minutes before the scheduled time when check-in should start.
    /// For example, 60 means check-in opens 1 hour before the scheduled time.
    /// </summary>
    public int? CheckInStartOffsetMinutes { get; set; }

    /// <summary>
    /// Minutes after the scheduled time when check-in should end.
    /// For example, 30 means check-in closes 30 minutes after the scheduled time.
    /// </summary>
    public int? CheckInEndOffsetMinutes { get; set; }

    /// <summary>
    /// The date when this schedule becomes effective.
    /// </summary>
    public DateOnly? EffectiveStartDate { get; set; }

    /// <summary>
    /// The date when this schedule is no longer effective.
    /// </summary>
    public DateOnly? EffectiveEndDate { get; set; }

    /// <summary>
    /// Optional foreign key to the Category for organizing schedules.
    /// </summary>
    public int? CategoryId { get; set; }

    /// <summary>
    /// Day of week for simple weekly schedules.
    /// 0 = Sunday, 1 = Monday, ..., 6 = Saturday.
    /// </summary>
    public DayOfWeek? WeeklyDayOfWeek { get; set; }

    /// <summary>
    /// Time of day for simple weekly schedules.
    /// </summary>
    public TimeSpan? WeeklyTimeOfDay { get; set; }

    /// <summary>
    /// Display order for sorting schedules.
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// Indicates whether this schedule is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Indicates whether this schedule should be automatically deactivated when complete.
    /// </summary>
    public bool AutoInactivateWhenComplete { get; set; }

    /// <summary>
    /// Indicates whether this schedule is visible in public calendars.
    /// </summary>
    public bool IsPublic { get; set; }

    // Navigation Properties

    /// <summary>
    /// Groups that use this schedule.
    /// </summary>
    public virtual ICollection<Group> Groups { get; set; } = new List<Group>();
}
