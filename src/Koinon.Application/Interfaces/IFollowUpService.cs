using Koinon.Application.Common;
using Koinon.Application.DTOs;
using Koinon.Domain.Entities;
using Koinon.Domain.Enums;

namespace Koinon.Application.Interfaces;

/// <summary>
/// Service interface for managing follow-up tasks for visitors and attendees.
/// </summary>
public interface IFollowUpService
{
    /// <summary>
    /// Creates a new follow-up task for a person.
    /// </summary>
    /// <param name="personId">The person's ID who needs follow-up.</param>
    /// <param name="attendanceId">Optional attendance record that triggered the follow-up.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The created follow-up entity.</returns>
    Task<FollowUp> CreateFollowUpAsync(
        int personId,
        int? attendanceId,
        CancellationToken ct = default);

    /// <summary>
    /// Gets all pending follow-ups, optionally filtered by assignee.
    /// </summary>
    /// <param name="assignedToIdKey">Optional IdKey of the person assigned to the follow-ups.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of pending follow-ups.</returns>
    Task<IReadOnlyList<FollowUpDto>> GetPendingFollowUpsAsync(
        string? assignedToIdKey = null,
        CancellationToken ct = default);

    /// <summary>
    /// Gets a follow-up by its IdKey.
    /// </summary>
    /// <param name="idKey">The follow-up's IdKey.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The follow-up DTO if found, null otherwise.</returns>
    Task<FollowUpDto?> GetByIdKeyAsync(
        string idKey,
        CancellationToken ct = default);

    /// <summary>
    /// Updates the status of a follow-up task.
    /// </summary>
    /// <param name="idKey">The follow-up's IdKey.</param>
    /// <param name="status">The new status.</param>
    /// <param name="notes">Optional notes about the status change.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The updated follow-up DTO.</returns>
    Task<Result<FollowUpDto>> UpdateStatusAsync(
        string idKey,
        FollowUpStatus status,
        string? notes = null,
        CancellationToken ct = default);

    /// <summary>
    /// Assigns a follow-up task to a person.
    /// </summary>
    /// <param name="idKey">The follow-up's IdKey.</param>
    /// <param name="assignedToIdKey">The IdKey of the person to assign the task to.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> AssignFollowUpAsync(
        string idKey,
        string assignedToIdKey,
        CancellationToken ct = default);
}
