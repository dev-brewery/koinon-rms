using AutoMapper;
using FluentValidation;
using Koinon.Application.Common;
using Koinon.Application.DTOs;
using Koinon.Application.DTOs.Requests;
using Koinon.Application.Interfaces;
using Koinon.Domain.Data;
using Koinon.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Koinon.Application.Services;

public class DeviceService(
    IApplicationDbContext context,
    IMapper mapper,
    IValidator<CreateDeviceRequest> createValidator,
    IValidator<UpdateDeviceRequest> updateValidator,
    ILogger<DeviceService> logger) : IDeviceService
{

    public async Task<IReadOnlyList<DeviceSummaryDto>> GetAllAsync(
        string? campusIdKey,
        bool includeInactive,
        CancellationToken ct = default)
    {
        var query = context.Devices
            .Include(d => d.DeviceTypeValue)
            .Include(d => d.Campus)
            .AsNoTracking();

        if (!includeInactive)
        {
            query = query.Where(d => d.IsActive);
        }

        if (!string.IsNullOrWhiteSpace(campusIdKey))
        {
            if (!IdKeyHelper.TryDecode(campusIdKey, out var campusId))
            {
                return new List<DeviceSummaryDto>();
            }

            query = query.Where(d => d.CampusId == campusId);
        }

        var devices = await query
            .OrderBy(d => d.Name)
            .ToListAsync(ct);

        return mapper.Map<List<DeviceSummaryDto>>(devices);
    }

    public async Task<Result<DeviceDetailDto>> GetByIdKeyAsync(string idKey, CancellationToken ct = default)
    {
        if (!IdKeyHelper.TryDecode(idKey, out var id))
        {
            return Result<DeviceDetailDto>.Failure(Error.NotFound("Device", idKey));
        }

        var device = await context.Devices
            .Include(d => d.DeviceTypeValue)
            .Include(d => d.Campus)
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == id, ct);

        if (device == null)
        {
            return Result<DeviceDetailDto>.Failure(Error.NotFound("Device", idKey));
        }

        return Result<DeviceDetailDto>.Success(mapper.Map<DeviceDetailDto>(device));
    }

    public async Task<Result<DeviceDetailDto>> CreateAsync(CreateDeviceRequest request, CancellationToken ct = default)
    {
        var validationResult = await createValidator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
        {
            return Result<DeviceDetailDto>.Failure(Error.FromFluentValidation(validationResult));
        }

        // Campus validation
        int? campusId = null;
        if (!string.IsNullOrWhiteSpace(request.CampusIdKey))
        {
            if (!IdKeyHelper.TryDecode(request.CampusIdKey, out var cId))
            {
                return Result<DeviceDetailDto>.Failure(Error.UnprocessableEntity($"Invalid campus IdKey: '{request.CampusIdKey}'"));
            }

            if (!await context.Campuses.AnyAsync(c => c.Id == cId, ct))
            {
                return Result<DeviceDetailDto>.Failure(Error.UnprocessableEntity($"Campus not found with IdKey '{request.CampusIdKey}'"));
            }

            campusId = cId;
        }

        // Device type validation
        int? deviceTypeValueId = null;
        if (!string.IsNullOrWhiteSpace(request.DeviceTypeValueIdKey))
        {
            if (!IdKeyHelper.TryDecode(request.DeviceTypeValueIdKey, out var typeId))
            {
                return Result<DeviceDetailDto>.Failure(Error.UnprocessableEntity($"Invalid device type IdKey: '{request.DeviceTypeValueIdKey}'"));
            }

            if (!await context.DefinedValues.AnyAsync(dv => dv.Id == typeId, ct))
            {
                return Result<DeviceDetailDto>.Failure(Error.UnprocessableEntity($"Device type not found with IdKey '{request.DeviceTypeValueIdKey}'"));
            }

            deviceTypeValueId = typeId;
        }

        var device = new Device
        {
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            DeviceTypeValueId = deviceTypeValueId,
            CampusId = campusId,
            IpAddress = request.IpAddress?.Trim(),
            IsActive = request.IsActive,
            CreatedDateTime = DateTime.UtcNow
        };

        context.Devices.Add(device);
        await context.SaveChangesAsync(ct);

        var created = await LoadDeviceAsync(device.Id, ct);
        return Result<DeviceDetailDto>.Success(mapper.Map<DeviceDetailDto>(created));
    }

    public async Task<Result<DeviceDetailDto>> UpdateAsync(
        string idKey,
        UpdateDeviceRequest request,
        CancellationToken ct = default)
    {
        if (!IdKeyHelper.TryDecode(idKey, out var id))
        {
            return Result<DeviceDetailDto>.Failure(Error.NotFound("Device", idKey));
        }

        var device = await context.Devices.FirstOrDefaultAsync(d => d.Id == id, ct);
        if (device == null)
        {
            return Result<DeviceDetailDto>.Failure(Error.NotFound("Device", idKey));
        }

        var validationResult = await updateValidator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
        {
            return Result<DeviceDetailDto>.Failure(Error.FromFluentValidation(validationResult));
        }

        if (request.Name != null)
        {
            device.Name = request.Name.Trim();
        }

        if (request.Description != null)
        {
            device.Description = request.Description.Trim();
        }

        if (request.IsActive.HasValue)
        {
            device.IsActive = request.IsActive.Value;
        }

        if (request.IpAddress != null)
        {
            device.IpAddress = string.IsNullOrWhiteSpace(request.IpAddress) ? null : request.IpAddress.Trim();
        }

        if (request.PrinterSettings != null)
        {
            device.PrinterSettings = string.IsNullOrWhiteSpace(request.PrinterSettings) ? null : request.PrinterSettings;
        }

        // Campus update
        if (request.CampusIdKey != null)
        {
            if (string.IsNullOrWhiteSpace(request.CampusIdKey))
            {
                device.CampusId = null;
            }
            else
            {
                if (!IdKeyHelper.TryDecode(request.CampusIdKey, out var campusId))
                {
                    return Result<DeviceDetailDto>.Failure(Error.UnprocessableEntity($"Invalid campus IdKey: '{request.CampusIdKey}'"));
                }

                if (!await context.Campuses.AnyAsync(c => c.Id == campusId, ct))
                {
                    return Result<DeviceDetailDto>.Failure(Error.UnprocessableEntity($"Campus not found with IdKey '{request.CampusIdKey}'"));
                }

                device.CampusId = campusId;
            }
        }

        // Device type update
        if (request.DeviceTypeValueIdKey != null)
        {
            if (string.IsNullOrWhiteSpace(request.DeviceTypeValueIdKey))
            {
                device.DeviceTypeValueId = null;
            }
            else
            {
                if (!IdKeyHelper.TryDecode(request.DeviceTypeValueIdKey, out var typeId))
                {
                    return Result<DeviceDetailDto>.Failure(Error.UnprocessableEntity($"Invalid device type IdKey: '{request.DeviceTypeValueIdKey}'"));
                }

                if (!await context.DefinedValues.AnyAsync(dv => dv.Id == typeId, ct))
                {
                    return Result<DeviceDetailDto>.Failure(Error.UnprocessableEntity($"Device type not found with IdKey '{request.DeviceTypeValueIdKey}'"));
                }

                device.DeviceTypeValueId = typeId;
            }
        }

        device.ModifiedDateTime = DateTime.UtcNow;
        await context.SaveChangesAsync(ct);

        var updated = await LoadDeviceAsync(device.Id, ct);
        return Result<DeviceDetailDto>.Success(mapper.Map<DeviceDetailDto>(updated));
    }

    public async Task<Result> DeleteAsync(string idKey, CancellationToken ct = default)
    {
        if (!IdKeyHelper.TryDecode(idKey, out var id))
        {
            return Result.Failure(Error.NotFound("Device", idKey));
        }

        var device = await context.Devices.FirstOrDefaultAsync(d => d.Id == id, ct);
        if (device == null)
        {
            return Result.Failure(Error.NotFound("Device", idKey));
        }

        device.IsActive = false;
        device.ModifiedDateTime = DateTime.UtcNow;

        await context.SaveChangesAsync(ct);

        return Result.Success();
    }

    public async Task<Result<GenerateKioskTokenResponseDto>> GenerateKioskTokenAsync(string idKey, CancellationToken ct = default)
    {
        if (!IdKeyHelper.TryDecode(idKey, out var id))
        {
            return Result<GenerateKioskTokenResponseDto>.Failure(Error.NotFound("Device", idKey));
        }

        var device = await context.Devices.FirstOrDefaultAsync(d => d.Id == id, ct);
        if (device == null)
        {
            return Result<GenerateKioskTokenResponseDto>.Failure(Error.NotFound("Device", idKey));
        }

        var rawToken = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
        device.KioskToken = rawToken;
        device.KioskTokenExpiresAt = null; // No expiration by default
        device.ModifiedDateTime = DateTime.UtcNow;

        await context.SaveChangesAsync(ct);

        logger.LogInformation("Kiosk token regenerated for device IdKey={IdKey}", idKey);

        var response = new GenerateKioskTokenResponseDto
        {
            Token = rawToken,
            DeviceIdKey = idKey,
            DeviceName = device.Name,
            ExpiresAt = device.KioskTokenExpiresAt
        };

        return Result<GenerateKioskTokenResponseDto>.Success(response);
    }

    private async Task<Device> LoadDeviceAsync(int id, CancellationToken ct)
    {
        return await context.Devices
            .Include(d => d.DeviceTypeValue)
            .Include(d => d.Campus)
            .AsNoTracking()
            .FirstAsync(d => d.Id == id, ct);
    }
}
