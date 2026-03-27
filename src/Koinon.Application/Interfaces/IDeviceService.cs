using Koinon.Application.Common;
using Koinon.Application.DTOs;
using Koinon.Application.DTOs.Requests;

namespace Koinon.Application.Interfaces;

public interface IDeviceService
{
    Task<IReadOnlyList<DeviceSummaryDto>> GetAllAsync(string? campusIdKey, bool includeInactive, CancellationToken ct = default);
    Task<Result<DeviceDetailDto>> GetByIdKeyAsync(string idKey, CancellationToken ct = default);
    Task<Result<DeviceDetailDto>> CreateAsync(CreateDeviceRequest request, CancellationToken ct = default);
    Task<Result<DeviceDetailDto>> UpdateAsync(string idKey, UpdateDeviceRequest request, CancellationToken ct = default);
    Task<Result> DeleteAsync(string idKey, CancellationToken ct = default);
    Task<Result<GenerateKioskTokenResponseDto>> GenerateKioskTokenAsync(string idKey, CancellationToken ct = default);
}
