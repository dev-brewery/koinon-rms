using System.Linq.Expressions;

namespace Koinon.Application.Interfaces;

/// <summary>
/// Service for scheduling and managing background jobs using Hangfire.
/// Provides methods for fire-and-forget, delayed, recurring, and continuation jobs.
/// </summary>
public interface IBackgroundJobService
{
    /// <summary>
    /// Enqueues a background job to be executed immediately (fire-and-forget).
    /// </summary>
    /// <typeparam name="T">The type containing the method to execute.</typeparam>
    /// <param name="methodCall">Expression representing the method to call.</param>
    /// <returns>Job ID that can be used to track the job.</returns>
    string Enqueue<T>(Expression<Action<T>> methodCall);

    /// <summary>
    /// Enqueues a background job to be executed immediately (fire-and-forget).
    /// </summary>
    /// <param name="methodCall">Expression representing the method to call.</param>
    /// <returns>Job ID that can be used to track the job.</returns>
    string Enqueue(Expression<Action> methodCall);

    /// <summary>
    /// Schedules a background job to be executed after a specified delay.
    /// </summary>
    /// <typeparam name="T">The type containing the method to execute.</typeparam>
    /// <param name="methodCall">Expression representing the method to call.</param>
    /// <param name="delay">Delay before execution.</param>
    /// <returns>Job ID that can be used to track the job.</returns>
    string Schedule<T>(Expression<Action<T>> methodCall, TimeSpan delay);

    /// <summary>
    /// Schedules a background job to be executed after a specified delay.
    /// </summary>
    /// <param name="methodCall">Expression representing the method to call.</param>
    /// <param name="delay">Delay before execution.</param>
    /// <returns>Job ID that can be used to track the job.</returns>
    string Schedule(Expression<Action> methodCall, TimeSpan delay);

    /// <summary>
    /// Schedules a background job to be executed at a specific time.
    /// </summary>
    /// <typeparam name="T">The type containing the method to execute.</typeparam>
    /// <param name="methodCall">Expression representing the method to call.</param>
    /// <param name="enqueueAt">Date and time when the job should be executed.</param>
    /// <returns>Job ID that can be used to track the job.</returns>
    string Schedule<T>(Expression<Action<T>> methodCall, DateTimeOffset enqueueAt);

    /// <summary>
    /// Schedules a background job to be executed at a specific time.
    /// </summary>
    /// <param name="methodCall">Expression representing the method to call.</param>
    /// <param name="enqueueAt">Date and time when the job should be executed.</param>
    /// <returns>Job ID that can be used to track the job.</returns>
    string Schedule(Expression<Action> methodCall, DateTimeOffset enqueueAt);

    /// <summary>
    /// Adds or updates a recurring job with the specified CRON schedule.
    /// </summary>
    /// <typeparam name="T">The type containing the method to execute.</typeparam>
    /// <param name="recurringJobId">Unique identifier for the recurring job.</param>
    /// <param name="methodCall">Expression representing the method to call.</param>
    /// <param name="cronExpression">CRON expression defining the schedule.</param>
    /// <param name="timeZone">Optional time zone for the schedule. Defaults to UTC.</param>
    void AddOrUpdateRecurringJob<T>(string recurringJobId, Expression<Action<T>> methodCall, string cronExpression, TimeZoneInfo? timeZone = null);

    /// <summary>
    /// Adds or updates a recurring job with the specified CRON schedule.
    /// </summary>
    /// <param name="recurringJobId">Unique identifier for the recurring job.</param>
    /// <param name="methodCall">Expression representing the method to call.</param>
    /// <param name="cronExpression">CRON expression defining the schedule.</param>
    /// <param name="timeZone">Optional time zone for the schedule. Defaults to UTC.</param>
    void AddOrUpdateRecurringJob(string recurringJobId, Expression<Action> methodCall, string cronExpression, TimeZoneInfo? timeZone = null);

    /// <summary>
    /// Removes a recurring job.
    /// </summary>
    /// <param name="recurringJobId">Unique identifier of the recurring job to remove.</param>
    void RemoveRecurringJob(string recurringJobId);

    /// <summary>
    /// Schedules a continuation job to be executed after the parent job completes successfully.
    /// </summary>
    /// <typeparam name="T">The type containing the method to execute.</typeparam>
    /// <param name="parentJobId">ID of the parent job.</param>
    /// <param name="methodCall">Expression representing the method to call.</param>
    /// <returns>Job ID of the continuation job.</returns>
    string ContinueJobWith<T>(string parentJobId, Expression<Action<T>> methodCall);

    /// <summary>
    /// Schedules a continuation job to be executed after the parent job completes successfully.
    /// </summary>
    /// <param name="parentJobId">ID of the parent job.</param>
    /// <param name="methodCall">Expression representing the method to call.</param>
    /// <returns>Job ID of the continuation job.</returns>
    string ContinueJobWith(string parentJobId, Expression<Action> methodCall);

    /// <summary>
    /// Deletes a background job.
    /// </summary>
    /// <param name="jobId">ID of the job to delete.</param>
    /// <returns>True if the job was deleted successfully; otherwise, false.</returns>
    bool Delete(string jobId);
}
