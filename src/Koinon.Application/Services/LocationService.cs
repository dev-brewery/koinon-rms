using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

public class LocationService : ILocationService
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IValidator<CreateLocationRequest> _createValidator;
    private readonly IValidator<UpdateLocationRequest> _updateValidator;
    private readonly ILogger<LocationService> _logger;

    public LocationService(
        IApplicationDbContext context,
        IMapper mapper,
        IValidator<CreateLocationRequest> createValidator,
        IValidator<UpdateLocationRequest> updateValidator,
        ILogger<LocationService> logger)
    {
        _context = context;
        _mapper = mapper;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _logger = logger;
    }

    public async Task<IReadOnlyList<LocationSummaryDto>> GetAllAsync(string? campusIdKey, bool includeInactive, CancellationToken ct = default)
    {
        var query = _context.Locations
            .Include(l => l.ParentLocation)
            .Include(l => l.Campus)
            .Include(l => l.LocationTypeValue)
            .AsNoTracking();

        if (!includeInactive)
        {
            query = query.Where(l => l.IsActive);
        }

        if (!string.IsNullOrWhiteSpace(campusIdKey))
        {
            if (IdKeyHelper.TryDecode(campusIdKey, out var campusId))
            {
                query = query.Where(l => l.CampusId == campusId);
            }
            else
            {
                // Invalid campus ID, return empty list or consider error? 
                // Controller logic implied filtering, so returning empty is safer than erroring for list endpoint
                // But controller returned BadRequest. Service should probably follow suit or just filter nothing?
                // Given the signature returns a list, and invalid filter usually implies empty result or error.
                // For simplicity and safety, let's treat invalid filter as "no match".
                // Ideally, we should validate filter before querying.
                // Since this is GetAll, returning empty list for invalid filter is acceptable.
                return new List<LocationSummaryDto>();
            }
        }

        var locations = await query
            .OrderBy(l => l.Order)
            .ThenBy(l => l.Name)
            .ToListAsync(ct);

        return _mapper.Map<List<LocationSummaryDto>>(locations);
    }

    public async Task<Result<IReadOnlyList<LocationDto>>> GetTreeAsync(string? campusIdKey, bool includeInactive, CancellationToken ct = default)
    {
        var query = _context.Locations
            .Include(l => l.ParentLocation)
            .Include(l => l.Campus)
            .Include(l => l.LocationTypeValue)
            .Include(l => l.OverflowLocation)
            .AsNoTracking();

        if (!includeInactive)
        {
            query = query.Where(l => l.IsActive);
        }

        if (!string.IsNullOrWhiteSpace(campusIdKey))
        {
            if (!IdKeyHelper.TryDecode(campusIdKey, out var campusId))
            {
                return Result<IReadOnlyList<LocationDto>>.Failure(Error.FromFluentValidation(new FluentValidation.Results.ValidationResult(new[]
                {
                    new FluentValidation.Results.ValidationFailure("CampusIdKey", $"Invalid campus IdKey format: '{campusIdKey}'")
                })));
            }

            query = query.Where(l => l.CampusId == campusId);
        }

        var allLocations = await query
            .OrderBy(l => l.Order)
            .ThenBy(l => l.Name)
            .ToListAsync(ct);

        var locationDict = allLocations.ToDictionary(l => l.Id);

        var rootLocations = allLocations
            .Where(l => l.ParentLocationId == null)
            .Select(l => BuildTree(l, locationDict, includeInactive))
            .ToList();

        return Result<IReadOnlyList<LocationDto>>.Success(rootLocations);
    }

    public async Task<Result<LocationDto>> GetByIdKeyAsync(string idKey, CancellationToken ct = default)
    {
        if (!IdKeyHelper.TryDecode(idKey, out var id))
        {
            return Result<LocationDto>.Failure(Error.NotFound("Location", idKey));
        }

        var location = await _context.Locations
            .Include(l => l.ParentLocation)
            .Include(l => l.Campus)
            .Include(l => l.LocationTypeValue)
            .Include(l => l.OverflowLocation)
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == id, ct);

        if (location == null)
        {
            return Result<LocationDto>.Failure(Error.NotFound("Location", idKey));
        }

        return Result<LocationDto>.Success(_mapper.Map<LocationDto>(location));
    }

    public async Task<Result<LocationDto>> CreateAsync(CreateLocationRequest request, CancellationToken ct = default)
    {
        var validationResult = await _createValidator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
        {
            return Result<LocationDto>.Failure(Error.FromFluentValidation(validationResult));
        }

        // Parent Location Validation
        int? parentLocationId = null;
        if (!string.IsNullOrWhiteSpace(request.ParentLocationIdKey))
        {
            if (!IdKeyHelper.TryDecode(request.ParentLocationIdKey, out var parentId))
            {
                return Result<LocationDto>.Failure(Error.UnprocessableEntity($"Invalid parent location IdKey: '{request.ParentLocationIdKey}'"));
            }

            if (!await _context.Locations.AnyAsync(l => l.Id == parentId, ct))
            {
                return Result<LocationDto>.Failure(Error.UnprocessableEntity($"Parent location not found with IdKey '{request.ParentLocationIdKey}'"));
            }
            parentLocationId = parentId;
        }

        // Campus Validation
        int? campusId = null;
        if (!string.IsNullOrWhiteSpace(request.CampusIdKey))
        {
            if (!IdKeyHelper.TryDecode(request.CampusIdKey, out var cId))
            {
                return Result<LocationDto>.Failure(Error.UnprocessableEntity($"Invalid campus IdKey: '{request.CampusIdKey}'"));
            }

            if (!await _context.Campuses.AnyAsync(c => c.Id == cId, ct))
            {
                return Result<LocationDto>.Failure(Error.UnprocessableEntity($"Campus not found with IdKey '{request.CampusIdKey}'"));
            }
            campusId = cId;
        }

        // Location Type Validation
        int? locationTypeId = null;
        if (!string.IsNullOrWhiteSpace(request.LocationTypeValueIdKey))
        {
            if (!IdKeyHelper.TryDecode(request.LocationTypeValueIdKey, out var typeId))
            {
                return Result<LocationDto>.Failure(Error.UnprocessableEntity($"Invalid location type IdKey: '{request.LocationTypeValueIdKey}'"));
            }

            if (!await _context.DefinedValues.AnyAsync(dv => dv.Id == typeId, ct))
            {
                return Result<LocationDto>.Failure(Error.UnprocessableEntity($"Location type not found with IdKey '{request.LocationTypeValueIdKey}'"));
            }
            locationTypeId = typeId;
        }

        // Overflow Location Validation
        int? overflowLocationId = null;
        if (!string.IsNullOrWhiteSpace(request.OverflowLocationIdKey))
        {
            if (!IdKeyHelper.TryDecode(request.OverflowLocationIdKey, out var overflowId))
            {
                return Result<LocationDto>.Failure(Error.UnprocessableEntity($"Invalid overflow location IdKey: '{request.OverflowLocationIdKey}'"));
            }

            if (!await _context.Locations.AnyAsync(l => l.Id == overflowId, ct))
            {
                return Result<LocationDto>.Failure(Error.UnprocessableEntity($"Overflow location not found with IdKey '{request.OverflowLocationIdKey}'"));
            }
            overflowLocationId = overflowId;
        }

        var location = new Location
        {
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            ParentLocationId = parentLocationId,
            CampusId = campusId,
            LocationTypeValueId = locationTypeId,
            SoftRoomThreshold = request.SoftRoomThreshold,
            FirmRoomThreshold = request.FirmRoomThreshold,
            StaffToChildRatio = request.StaffToChildRatio,
            OverflowLocationId = overflowLocationId,
            AutoAssignOverflow = request.AutoAssignOverflow,
            Street1 = request.Street1?.Trim(),
            Street2 = request.Street2?.Trim(),
            City = request.City?.Trim(),
            State = request.State?.Trim(),
            PostalCode = request.PostalCode?.Trim(),
            Country = request.Country?.Trim(),
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            IsGeoPointLocked = request.IsGeoPointLocked,
            Order = request.Order,
            IsActive = true,
            CreatedDateTime = DateTime.UtcNow
        };

        _context.Locations.Add(location);
        await _context.SaveChangesAsync(ct);

        // Reload to get includes for DTO
        var createdLocation = await _context.Locations
            .Include(l => l.ParentLocation)
            .Include(l => l.Campus)
            .Include(l => l.LocationTypeValue)
            .Include(l => l.OverflowLocation)
            .AsNoTracking()
            .FirstAsync(l => l.Id == location.Id, ct);

        return Result<LocationDto>.Success(_mapper.Map<LocationDto>(createdLocation));
    }

    public async Task<Result<LocationDto>> UpdateAsync(string idKey, UpdateLocationRequest request, CancellationToken ct = default)
    {
        if (!IdKeyHelper.TryDecode(idKey, out var id))
        {
            return Result<LocationDto>.Failure(Error.NotFound("Location", idKey));
        }

        var location = await _context.Locations.FirstOrDefaultAsync(l => l.Id == id, ct);
        if (location == null)
        {
            return Result<LocationDto>.Failure(Error.NotFound("Location", idKey));
        }

        var validationResult = await _updateValidator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
        {
            return Result<LocationDto>.Failure(Error.FromFluentValidation(validationResult));
        }

        if (request.Name != null)
        {
            location.Name = request.Name.Trim();
        }
        if (request.Description != null)
        {
            location.Description = request.Description.Trim();
        }
        if (request.IsActive.HasValue)
        {
            location.IsActive = request.IsActive.Value;
        }

        // Parent Update
        if (request.ParentLocationIdKey != null)
        {
            if (string.IsNullOrWhiteSpace(request.ParentLocationIdKey))
            {
                location.ParentLocationId = null;
            }
            else
            {
                if (!IdKeyHelper.TryDecode(request.ParentLocationIdKey, out var parentId))
                {
                    return Result<LocationDto>.Failure(Error.UnprocessableEntity($"Invalid parent location IdKey: '{request.ParentLocationIdKey}'"));
                }

                if (!await _context.Locations.AnyAsync(l => l.Id == parentId, ct))
                {
                    return Result<LocationDto>.Failure(Error.UnprocessableEntity($"Parent location not found with IdKey '{request.ParentLocationIdKey}'"));
                }

                if (await WouldCreateCircularReferenceAsync(location.Id, parentId, ct))
                {
                    return Result<LocationDto>.Failure(Error.UnprocessableEntity("Setting this parent would create a circular reference in the location hierarchy"));
                }

                location.ParentLocationId = parentId;
            }
        }

        // Campus Update
        if (request.CampusIdKey != null)
        {
            if (string.IsNullOrWhiteSpace(request.CampusIdKey))
            {
                location.CampusId = null;
            }
            else
            {
                if (!IdKeyHelper.TryDecode(request.CampusIdKey, out var campusId))
                {
                    return Result<LocationDto>.Failure(Error.UnprocessableEntity($"Invalid campus IdKey: '{request.CampusIdKey}'"));
                }

                if (!await _context.Campuses.AnyAsync(c => c.Id == campusId, ct))
                {
                    return Result<LocationDto>.Failure(Error.UnprocessableEntity($"Campus not found with IdKey '{request.CampusIdKey}'"));
                }

                location.CampusId = campusId;
            }
        }

        // Location Type Update
        if (request.LocationTypeValueIdKey != null)
        {
            if (string.IsNullOrWhiteSpace(request.LocationTypeValueIdKey))
            {
                location.LocationTypeValueId = null;
            }
            else
            {
                if (!IdKeyHelper.TryDecode(request.LocationTypeValueIdKey, out var typeId))
                {
                    return Result<LocationDto>.Failure(Error.UnprocessableEntity($"Invalid location type IdKey: '{request.LocationTypeValueIdKey}'"));
                }

                if (!await _context.DefinedValues.AnyAsync(dv => dv.Id == typeId, ct))
                {
                    return Result<LocationDto>.Failure(Error.UnprocessableEntity($"Location type not found with IdKey '{request.LocationTypeValueIdKey}'"));
                }

                location.LocationTypeValueId = typeId;
            }
        }

        if (request.SoftRoomThreshold.HasValue)
        {
            location.SoftRoomThreshold = request.SoftRoomThreshold.Value;
        }
        if (request.FirmRoomThreshold.HasValue)
        {
            location.FirmRoomThreshold = request.FirmRoomThreshold.Value;
        }
        if (request.StaffToChildRatio.HasValue)
        {
            location.StaffToChildRatio = request.StaffToChildRatio.Value;
        }
        if (request.AutoAssignOverflow.HasValue)
        {
            location.AutoAssignOverflow = request.AutoAssignOverflow.Value;
        }

        // Relationships validation for thresholds
        if (location.SoftRoomThreshold.HasValue && location.FirmRoomThreshold.HasValue &&
            location.SoftRoomThreshold.Value > location.FirmRoomThreshold.Value)
        {
            return Result<LocationDto>.Failure(Error.UnprocessableEntity("Soft room threshold must be less than or equal to firm room threshold"));
        }

        // Overflow Update
        if (request.OverflowLocationIdKey != null)
        {
            if (string.IsNullOrWhiteSpace(request.OverflowLocationIdKey))
            {
                location.OverflowLocationId = null;
            }
            else
            {
                if (!IdKeyHelper.TryDecode(request.OverflowLocationIdKey, out var overflowId))
                {
                    return Result<LocationDto>.Failure(Error.UnprocessableEntity($"Invalid overflow location IdKey: '{request.OverflowLocationIdKey}'"));
                }

                if (!await _context.Locations.AnyAsync(l => l.Id == overflowId, ct))
                {
                    return Result<LocationDto>.Failure(Error.UnprocessableEntity($"Overflow location not found with IdKey '{request.OverflowLocationIdKey}'"));
                }

                location.OverflowLocationId = overflowId;
            }
        }

        if (request.Street1 != null)
        {
            location.Street1 = request.Street1.Trim();
        }
        if (request.Street2 != null)
        {
            location.Street2 = request.Street2.Trim();
        }
        if (request.City != null)
        {
            location.City = request.City.Trim();
        }
        if (request.State != null)
        {
            location.State = request.State.Trim();
        }
        if (request.PostalCode != null)
        {
            location.PostalCode = request.PostalCode.Trim();
        }
        if (request.Country != null)
        {
            location.Country = request.Country.Trim();
        }
        if (request.Latitude.HasValue)
        {
            location.Latitude = request.Latitude.Value;
        }
        if (request.Longitude.HasValue)
        {
            location.Longitude = request.Longitude.Value;
        }
        if (request.IsGeoPointLocked.HasValue)
        {
            location.IsGeoPointLocked = request.IsGeoPointLocked.Value;
        }
        if (request.Order.HasValue)
        {
            location.Order = request.Order.Value;
        }

        location.ModifiedDateTime = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);

        var updatedLocation = await _context.Locations
            .Include(l => l.ParentLocation)
            .Include(l => l.Campus)
            .Include(l => l.LocationTypeValue)
            .Include(l => l.OverflowLocation)
            .AsNoTracking()
            .FirstAsync(l => l.Id == location.Id, ct);

        return Result<LocationDto>.Success(_mapper.Map<LocationDto>(updatedLocation));
    }

    public async Task<Result> DeleteAsync(string idKey, CancellationToken ct = default)
    {
        if (!IdKeyHelper.TryDecode(idKey, out var id))
        {
            return Result.Failure(Error.NotFound("Location", idKey));
        }

        var location = await _context.Locations.FirstOrDefaultAsync(l => l.Id == id, ct);
        if (location == null)
        {
            return Result.Failure(Error.NotFound("Location", idKey));
        }

        var hasActiveChildren = await _context.Locations
            .AnyAsync(l => l.ParentLocationId == id && l.IsActive, ct);

        if (hasActiveChildren)
        {
            return Result.Failure(Error.UnprocessableEntity("Cannot deactivate location with active child locations. Deactivate children first."));
        }

        location.IsActive = false;
        location.ModifiedDateTime = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);

        return Result.Success();
    }

    private LocationDto BuildTree(Location location, Dictionary<int, Location> locationDict, bool includeInactive)
    {
        var children = locationDict.Values
            .Where(l => l.ParentLocationId == location.Id)
            .Where(l => includeInactive || l.IsActive)
            .OrderBy(l => l.Order)
            .ThenBy(l => l.Name)
            .Select(child => BuildTree(child, locationDict, includeInactive))
            .ToList();

        var dto = _mapper.Map<LocationDto>(location);
        return dto with { Children = children };
    }

    private async Task<bool> WouldCreateCircularReferenceAsync(int locationId, int newParentId, CancellationToken ct)
    {
        if (locationId == newParentId)
        {
            return true;
        }

        var currentId = newParentId;
        var visited = new HashSet<int> { newParentId };

        while (currentId != 0)
        {
            var parent = await _context.Locations
                .Where(l => l.Id == currentId)
                .Select(l => new { l.ParentLocationId })
                .FirstOrDefaultAsync(ct);

            if (parent?.ParentLocationId == null)
            {
                return false;
            }

            currentId = parent.ParentLocationId.Value;

            if (currentId == locationId)
            {
                return true;
            }

            if (!visited.Add(currentId))
            {
                _logger.LogWarning("Detected existing circular reference in location hierarchy at Id={CurrentId}", currentId);
                return true;
            }
        }

        return false;
    }
}
