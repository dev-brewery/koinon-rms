using Koinon.Application.Common;
using Koinon.Application.DTOs.Giving;
using Koinon.Application.DTOs.Requests;

namespace Koinon.Application.Interfaces;

/// <summary>
/// Service for fund administration: creating, updating, listing, and deactivating funds.
/// </summary>
public interface IFundService
{
    /// <summary>
    /// Returns all funds (active and inactive), ordered by Order then Name.
    /// </summary>
    Task<IReadOnlyList<FundAdminDto>> GetAllFundsAsync(CancellationToken ct = default);

    /// <summary>
    /// Returns a single fund by IdKey with full admin detail.
    /// </summary>
    Task<Result<FundAdminDto>> GetFundAdminAsync(string idKey, CancellationToken ct = default);

    /// <summary>
    /// Creates a new active fund from the supplied request.
    /// </summary>
    Task<Result<FundAdminDto>> CreateFundAsync(CreateFundRequest request, CancellationToken ct = default);

    /// <summary>
    /// Updates editable fields on an existing fund.
    /// </summary>
    Task<Result<FundAdminDto>> UpdateFundAsync(string idKey, UpdateFundRequest request, CancellationToken ct = default);

    /// <summary>
    /// Soft-deactivates a fund by setting IsActive = false.
    /// Funds that have associated contribution details cannot be deleted and are deactivated instead.
    /// </summary>
    Task<Result> DeactivateFundAsync(string idKey, CancellationToken ct = default);
}
