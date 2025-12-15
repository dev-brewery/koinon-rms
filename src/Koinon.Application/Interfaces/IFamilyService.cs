using Koinon.Application.Common;
using Koinon.Application.DTOs;
using Koinon.Application.DTOs.Requests;

namespace Koinon.Application.Interfaces;

/// <summary>
/// Service interface for family (household) management operations.
/// </summary>
public interface IFamilyService
{
    /// <summary>
    /// Gets a family by their integer ID with all members.
    /// </summary>
    Task<FamilyDto?> GetByIdAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Gets a family by their IdKey with all members.
    /// </summary>
    Task<FamilyDto?> GetByIdKeyAsync(string idKey, CancellationToken ct = default);

    /// <summary>
    /// Searches for families with optional filters and pagination.
    /// </summary>
    Task<PagedResult<FamilySummaryDto>> SearchAsync(
        string? searchTerm,
        string? campusIdKey,
        bool includeInactive,
        int page,
        int pageSize,
        CancellationToken ct = default);

    /// <summary>
    /// Creates a new family group.
    /// </summary>
    Task<Result<FamilyDto>> CreateFamilyAsync(
        CreateFamilyRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Adds a person as a family member with a specific role.
    /// </summary>
    Task<Result<FamilyMemberDto>> AddFamilyMemberAsync(
        string familyIdKey,
        AddFamilyMemberRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Removes a person from a family.
    /// </summary>
    Task<Result> RemoveFamilyMemberAsync(
        string familyIdKey,
        string personIdKey,
        CancellationToken ct = default);

    /// <summary>
    /// Sets a family as a person's primary family.
    /// </summary>
    Task<Result> SetPrimaryFamilyAsync(
        string personIdKey,
        string familyIdKey,
        CancellationToken ct = default);

    /// <summary>
    /// Updates the address for a family.
    /// </summary>
    Task<Result<FamilyDto>> UpdateAddressAsync(
        string familyIdKey,
        UpdateFamilyAddressRequest request,
        CancellationToken ct = default);
}
