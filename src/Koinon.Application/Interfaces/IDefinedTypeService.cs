using Koinon.Application.Common;
using Koinon.Application.DTOs;
using Koinon.Application.DTOs.Requests;

namespace Koinon.Application.Interfaces;

/// <summary>
/// Service for managing DefinedTypes and their DefinedValues.
/// DefinedTypes are system-level lookup tables (e.g., "Phone Number Type").
/// Values within a type can be managed by admins; the type itself cannot be deleted
/// if it is system-owned.
/// </summary>
public interface IDefinedTypeService
{
    /// <summary>
    /// Returns all DefinedTypes ordered by Category then Name, with a count of their values.
    /// </summary>
    Task<IReadOnlyList<DefinedTypeSummaryDto>> GetAllTypesAsync(CancellationToken ct = default);

    /// <summary>
    /// Returns a single DefinedType with all its DefinedValues ordered by Order.
    /// </summary>
    Task<Result<DefinedTypeDto>> GetTypeByIdKeyAsync(string idKey, CancellationToken ct = default);

    /// <summary>
    /// Creates a new DefinedValue within the specified DefinedType.
    /// Order defaults to max(existing)+1.
    /// </summary>
    Task<Result<DefinedValueManagementDto>> CreateValueAsync(
        string typeIdKey,
        CreateDefinedValueRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Updates the fields of an existing DefinedValue.
    /// </summary>
    Task<Result<DefinedValueManagementDto>> UpdateValueAsync(
        string valueIdKey,
        UpdateDefinedValueRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Soft-deactivates a DefinedValue (sets IsActive = false).
    /// Hard delete is never performed so foreign-key references remain intact.
    /// </summary>
    Task<Result> DeleteValueAsync(string valueIdKey, CancellationToken ct = default);

    /// <summary>
    /// Bulk-updates the Order field on DefinedValues within a DefinedType.
    /// </summary>
    Task<Result> ReorderValuesAsync(
        string typeIdKey,
        ReorderDefinedValuesRequest request,
        CancellationToken ct = default);
}
