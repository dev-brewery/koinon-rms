namespace Koinon.Application.Interfaces;

/// <summary>
/// Service for handling retry logic for failed follow-up creation operations.
/// Uses background jobs with exponential backoff to ensure follow-ups are eventually created.
/// </summary>
public interface IFollowUpRetryService
{
    /// <summary>
    /// Queues a follow-up creation attempt in the background with retry logic.
    /// Uses exponential backoff: 1min, 5min, 15min, 1hr, 4hr
    /// </summary>
    /// <param name="personId">The person's ID who needs follow-up.</param>
    /// <param name="attendanceId">The attendance record that triggered the follow-up.</param>
    /// <param name="attemptNumber">Current attempt number (0-based, default 0 for first attempt).</param>
    /// <returns>Job ID for tracking the background job.</returns>
    string QueueFollowUpCreation(int personId, int attendanceId, int attemptNumber = 0);

    /// <summary>
    /// Processes a follow-up creation attempt. This method is executed by the background job.
    /// Retries with exponential backoff if the creation fails.
    /// </summary>
    /// <param name="personId">The person's ID who needs follow-up.</param>
    /// <param name="attendanceId">The attendance record that triggered the follow-up.</param>
    /// <param name="attemptNumber">Current attempt number (0-based).</param>
    Task ProcessFollowUpCreationAsync(int personId, int attendanceId, int attemptNumber);
}
