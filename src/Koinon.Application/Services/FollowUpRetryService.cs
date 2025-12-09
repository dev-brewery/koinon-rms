using Koinon.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Koinon.Application.Services;

/// <summary>
/// Handles retry logic for failed follow-up creation operations using Hangfire.
/// Implements exponential backoff strategy: 1min, 5min, 15min, 1hr, 4hr.
/// </summary>
public class FollowUpRetryService(
    IFollowUpService followUpService,
    IBackgroundJobService backgroundJobService,
    ILogger<FollowUpRetryService> logger) : IFollowUpRetryService
{
    // Exponential backoff schedule in minutes
    private static readonly int[] _retryDelaysMinutes = { 1, 5, 15, 60, 240 };
    private const int MaxAttempts = 5;

    public string QueueFollowUpCreation(int personId, int attendanceId, int attemptNumber = 0)
    {
        // Input validation
        if (personId <= 0)
        {
            throw new ArgumentException("PersonId must be greater than zero.", nameof(personId));
        }

        if (attendanceId <= 0)
        {
            throw new ArgumentException("AttendanceId must be greater than zero.", nameof(attendanceId));
        }

        if (attemptNumber < 0)
        {
            throw new ArgumentException("AttemptNumber cannot be negative.", nameof(attemptNumber));
        }

        if (attemptNumber >= MaxAttempts)
        {
            logger.LogCritical(
                "FOLLOW_UP_RETRY_EXHAUSTED: Maximum retry attempts ({MaxAttempts}) reached. " +
                "PersonId={PersonId} AttendanceId={AttendanceId}. " +
                "Manual intervention required - follow-up was never created.",
                MaxAttempts, personId, attendanceId);

            throw new InvalidOperationException(
                $"Maximum retry attempts reached for follow-up creation: PersonId={personId}, AttendanceId={attendanceId}");
        }

        // Calculate delay for this attempt
        var delayMinutes = attemptNumber > 0 ? _retryDelaysMinutes[attemptNumber - 1] : 0;
        var delay = TimeSpan.FromMinutes(delayMinutes);

        string jobId;
        if (attemptNumber == 0)
        {
            // First attempt - execute immediately
            // Note: Hangfire will inject the CancellationToken at execution time
            jobId = backgroundJobService.Enqueue<IFollowUpRetryService>(
                service => service.ProcessFollowUpCreationAsync(personId, attendanceId, attemptNumber, CancellationToken.None));

            logger.LogInformation(
                "Queued immediate follow-up creation attempt for PersonId={PersonId} AttendanceId={AttendanceId}",
                personId, attendanceId);
        }
        else
        {
            // Subsequent attempts - schedule with exponential backoff
            // Note: Hangfire will inject the CancellationToken at execution time
            jobId = backgroundJobService.Schedule<IFollowUpRetryService>(
                service => service.ProcessFollowUpCreationAsync(personId, attendanceId, attemptNumber, CancellationToken.None),
                delay);

            logger.LogWarning(
                "FOLLOW_UP_RETRY_SCHEDULED: Attempt {AttemptNumber}/{MaxAttempts} scheduled in {DelayMinutes}min. " +
                "PersonId={PersonId} AttendanceId={AttendanceId}",
                attemptNumber + 1, MaxAttempts, delayMinutes, personId, attendanceId);
        }

        return jobId;
    }

    public async Task ProcessFollowUpCreationAsync(int personId, int attendanceId, int attemptNumber, CancellationToken cancellationToken = default)
    {
        // CRITICAL: Input validation - prevent invalid data from being processed
        if (personId <= 0)
        {
            throw new ArgumentException("PersonId must be greater than zero.", nameof(personId));
        }

        if (attendanceId <= 0)
        {
            throw new ArgumentException("AttendanceId must be greater than zero.", nameof(attendanceId));
        }

        if (attemptNumber < 0)
        {
            throw new ArgumentException("AttemptNumber cannot be negative.", nameof(attemptNumber));
        }

        try
        {
            logger.LogInformation(
                "Processing follow-up creation attempt {AttemptNumber}/{MaxAttempts} for PersonId={PersonId} AttendanceId={AttendanceId}",
                attemptNumber + 1, MaxAttempts, personId, attendanceId);

            // Attempt to create the follow-up with cancellation support
            var followUp = await followUpService.CreateFollowUpAsync(personId, attendanceId, cancellationToken);

            logger.LogInformation(
                "FOLLOW_UP_RETRY_SUCCESS: Successfully created follow-up {FollowUpId} on attempt {AttemptNumber}/{MaxAttempts}. " +
                "PersonId={PersonId} AttendanceId={AttendanceId}",
                followUp.Id, attemptNumber + 1, MaxAttempts, personId, attendanceId);
        }
        catch (Exception ex)
        {
            // Log the failure with structured data
            logger.LogError(ex,
                "FOLLOW_UP_RETRY_FAILED: Attempt {AttemptNumber}/{MaxAttempts} failed. " +
                "PersonId={PersonId} AttendanceId={AttendanceId}",
                attemptNumber + 1, MaxAttempts, personId, attendanceId);

            // Schedule next retry attempt if not exhausted
            if (attemptNumber + 1 < MaxAttempts)
            {
                QueueFollowUpCreation(personId, attendanceId, attemptNumber + 1);
            }
            else
            {
                // Final attempt failed - log critical alert
                logger.LogCritical(
                    "FOLLOW_UP_RETRY_EXHAUSTED: All {MaxAttempts} retry attempts failed. " +
                    "PersonId={PersonId} AttendanceId={AttendanceId}. " +
                    "Manual intervention required - follow-up was never created. " +
                    "Last error: {ErrorMessage}",
                    MaxAttempts, personId, attendanceId, ex.Message);

                // Rethrow to mark job as failed in Hangfire
                throw;
            }
        }
    }
}
