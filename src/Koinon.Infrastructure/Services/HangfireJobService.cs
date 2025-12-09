using System.Linq.Expressions;
using Hangfire;
using Koinon.Application.Interfaces;

namespace Koinon.Infrastructure.Services;

/// <summary>
/// Hangfire implementation of IBackgroundJobService.
/// Provides background job scheduling with retry policies and persistence.
/// </summary>
public class HangfireJobService : IBackgroundJobService
{
    /// <inheritdoc />
    public string Enqueue<T>(Expression<Action<T>> methodCall)
    {
        return BackgroundJob.Enqueue(methodCall);
    }

    /// <inheritdoc />
    public string Enqueue(Expression<Action> methodCall)
    {
        return BackgroundJob.Enqueue(methodCall);
    }

    /// <inheritdoc />
    public string Schedule<T>(Expression<Action<T>> methodCall, TimeSpan delay)
    {
        return BackgroundJob.Schedule(methodCall, delay);
    }

    /// <inheritdoc />
    public string Schedule(Expression<Action> methodCall, TimeSpan delay)
    {
        return BackgroundJob.Schedule(methodCall, delay);
    }

    /// <inheritdoc />
    public string Schedule<T>(Expression<Action<T>> methodCall, DateTimeOffset enqueueAt)
    {
        return BackgroundJob.Schedule(methodCall, enqueueAt);
    }

    /// <inheritdoc />
    public string Schedule(Expression<Action> methodCall, DateTimeOffset enqueueAt)
    {
        return BackgroundJob.Schedule(methodCall, enqueueAt);
    }

    /// <inheritdoc />
    public void AddOrUpdateRecurringJob<T>(string recurringJobId, Expression<Action<T>> methodCall, string cronExpression, TimeZoneInfo? timeZone = null)
    {
        RecurringJob.AddOrUpdate(recurringJobId, methodCall, cronExpression, new RecurringJobOptions
        {
            TimeZone = timeZone ?? TimeZoneInfo.Utc
        });
    }

    /// <inheritdoc />
    public void AddOrUpdateRecurringJob(string recurringJobId, Expression<Action> methodCall, string cronExpression, TimeZoneInfo? timeZone = null)
    {
        RecurringJob.AddOrUpdate(recurringJobId, methodCall, cronExpression, new RecurringJobOptions
        {
            TimeZone = timeZone ?? TimeZoneInfo.Utc
        });
    }

    /// <inheritdoc />
    public void RemoveRecurringJob(string recurringJobId)
    {
        RecurringJob.RemoveIfExists(recurringJobId);
    }

    /// <inheritdoc />
    public string ContinueJobWith<T>(string parentJobId, Expression<Action<T>> methodCall)
    {
        return BackgroundJob.ContinueJobWith(parentJobId, methodCall);
    }

    /// <inheritdoc />
    public string ContinueJobWith(string parentJobId, Expression<Action> methodCall)
    {
        return BackgroundJob.ContinueJobWith(parentJobId, methodCall);
    }

    /// <inheritdoc />
    public bool Delete(string jobId)
    {
        return BackgroundJob.Delete(jobId);
    }
}
