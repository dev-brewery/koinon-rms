using System.Collections.Generic;
using System.Threading.Tasks;
using Koinon.Application.Common;
using Koinon.Application.DTOs;
using Koinon.Application.DTOs.Requests;

namespace Koinon.Application.Interfaces;

public interface ILocationService
{
    Task<IReadOnlyList<LocationSummaryDto>> GetAllAsync(string? campusIdKey, bool includeInactive, CancellationToken ct = default);
    Task<Result<IReadOnlyList<LocationDto>>> GetTreeAsync(string? campusIdKey, bool includeInactive, CancellationToken ct = default);
    Task<Result<LocationDto>> GetByIdKeyAsync(string idKey, CancellationToken ct = default);
    Task<Result<LocationDto>> CreateAsync(CreateLocationRequest request, CancellationToken ct = default);
    Task<Result<LocationDto>> UpdateAsync(string idKey, UpdateLocationRequest request, CancellationToken ct = default);
    Task<Result> DeleteAsync(string idKey, CancellationToken ct = default);
}
