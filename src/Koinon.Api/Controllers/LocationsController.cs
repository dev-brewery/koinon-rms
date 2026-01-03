using Koinon.Api.Filters;
using Koinon.Application.DTOs;
using Koinon.Application.DTOs.Requests;
using Koinon.Application.Interfaces;
using Koinon.Domain.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Koinon.Api.Controllers;

/// <summary>
/// Controller for location operations.
/// Provides endpoints for managing physical locations (buildings, rooms) with hierarchical organization.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
[ValidateIdKey]
public class LocationsController(
    IApplicationDbContext context,
    ILogger<LocationsController> logger) : ControllerBase
{
    /// <summary>
    /// Gets all locations.
    /// </summary>
    /// <param name="campusIdKey">Optional campus filter - only return locations for this campus</param>
    /// <param name="includeInactive">Include inactive locations (default: false)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of all locations</returns>
    /// <response code="200">Returns list of locations</response>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<LocationSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? campusIdKey = null,
        [FromQuery] bool includeInactive = false,
        CancellationToken ct = default)
    {
        var query = context.Locations
            .Include(l => l.ParentLocation)
            .Include(l => l.Campus)
            .Include(l => l.LocationTypeValue)
            .AsNoTracking();

        if (!includeInactive)
        {
            query = query.Where(l => l.IsActive);
        }

        // Filter by campus if provided
        if (!string.IsNullOrWhiteSpace(campusIdKey))
        {
            if (!IdKeyHelper.TryDecode(campusIdKey, out var campusId))
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Validation failed",
                    Detail = $"Invalid campus IdKey format: '{campusIdKey}'",
                    Status = StatusCodes.Status400BadRequest,
                    Instance = HttpContext.Request.Path
                });
            }

            query = query.Where(l => l.CampusId == campusId);
        }

        var locations = await query
            .OrderBy(l => l.Order)
            .ThenBy(l => l.Name)
            .Select(l => new LocationSummaryDto
            {
                IdKey = l.IdKey,
                Name = l.Name,
                Description = l.Description,
                IsActive = l.IsActive,
                ParentLocationName = l.ParentLocation != null ? l.ParentLocation.Name : null,
                CampusName = l.Campus != null ? l.Campus.Name : null,
                LocationTypeName = l.LocationTypeValue != null ? l.LocationTypeValue.Value : null
            })
            .ToListAsync(ct);

        logger.LogInformation(
            "Retrieved {Count} locations (campusIdKey: {CampusIdKey}, includeInactive: {IncludeInactive})",
            locations.Count,
            campusIdKey ?? "all",
            includeInactive);

        return Ok(new { data = locations });
    }

    /// <summary>
    /// Gets locations in hierarchical tree structure.
    /// </summary>
    /// <param name="campusIdKey">Optional campus filter - only return locations for this campus</param>
    /// <param name="includeInactive">Include inactive locations (default: false)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Hierarchical tree of locations</returns>
    /// <response code="200">Returns hierarchical location tree</response>
    [HttpGet("tree")]
    [ProducesResponseType(typeof(IReadOnlyList<LocationDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTree(
        [FromQuery] string? campusIdKey = null,
        [FromQuery] bool includeInactive = false,
        CancellationToken ct = default)
    {
        var query = context.Locations
            .Include(l => l.ParentLocation)
            .Include(l => l.Campus)
            .Include(l => l.LocationTypeValue)
            .Include(l => l.OverflowLocation)
            .AsNoTracking();

        if (!includeInactive)
        {
            query = query.Where(l => l.IsActive);
        }

        // Filter by campus if provided
        if (!string.IsNullOrWhiteSpace(campusIdKey))
        {
            if (!IdKeyHelper.TryDecode(campusIdKey, out var campusId))
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Validation failed",
                    Detail = $"Invalid campus IdKey format: '{campusIdKey}'",
                    Status = StatusCodes.Status400BadRequest,
                    Instance = HttpContext.Request.Path
                });
            }

            query = query.Where(l => l.CampusId == campusId);
        }

        // Load all locations into memory
        var allLocations = await query
            .OrderBy(l => l.Order)
            .ThenBy(l => l.Name)
            .ToListAsync(ct);

        // Build lookup dictionary for efficient tree construction
        var locationDict = allLocations.ToDictionary(l => l.Id);

        // Build tree recursively
        var rootLocations = allLocations
            .Where(l => l.ParentLocationId == null)
            .Select(l => BuildLocationDto(l, locationDict, includeInactive))
            .ToList();

        logger.LogInformation(
            "Retrieved location tree with {Count} root locations (campusIdKey: {CampusIdKey}, includeInactive: {IncludeInactive})",
            rootLocations.Count,
            campusIdKey ?? "all",
            includeInactive);

        return Ok(new { data = rootLocations });
    }

    /// <summary>
    /// Gets a specific location by IdKey.
    /// </summary>
    /// <param name="idKey">The location's IdKey</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Location details</returns>
    /// <response code="200">Returns location details</response>
    /// <response code="404">Location not found</response>
    [HttpGet("{idKey}")]
    [ProducesResponseType(typeof(LocationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByIdKey(string idKey, CancellationToken ct = default)
    {
        if (!IdKeyHelper.TryDecode(idKey, out var id))
        {
            logger.LogDebug("Invalid IdKey format: {IdKey}", idKey);

            return NotFound(new ProblemDetails
            {
                Title = "Location not found",
                Detail = $"Invalid IdKey format: '{idKey}'",
                Status = StatusCodes.Status404NotFound,
                Instance = HttpContext.Request.Path
            });
        }

        var location = await context.Locations
            .Include(l => l.ParentLocation)
            .Include(l => l.Campus)
            .Include(l => l.LocationTypeValue)
            .Include(l => l.OverflowLocation)
            .Include(l => l.ChildLocations.Where(cl => cl.IsActive))
            .AsNoTracking()
            .Where(l => l.Id == id)
            .FirstOrDefaultAsync(ct);

        if (location == null)
        {
            logger.LogDebug("Location not found: IdKey={IdKey}", idKey);

            return NotFound(new ProblemDetails
            {
                Title = "Location not found",
                Detail = $"No location found with IdKey '{idKey}'",
                Status = StatusCodes.Status404NotFound,
                Instance = HttpContext.Request.Path
            });
        }

        var locationDto = MapToLocationDto(location);

        logger.LogDebug("Location retrieved: IdKey={IdKey}, Name={Name}", idKey, location.Name);

        return Ok(new { data = locationDto });
    }

    /// <summary>
    /// Creates a new location.
    /// </summary>
    /// <param name="request">Location creation details</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Created location details</returns>
    /// <response code="201">Location created successfully</response>
    /// <response code="400">Validation failed</response>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(LocationDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateLocationRequest request, CancellationToken ct = default)
    {
        // Validate required fields
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Validation failed",
                Detail = "Location name is required",
                Status = StatusCodes.Status400BadRequest,
                Instance = HttpContext.Request.Path
            });
        }

        // Resolve parent location ID if provided
        int? parentLocationId = null;
        if (!string.IsNullOrWhiteSpace(request.ParentLocationIdKey))
        {
            if (!IdKeyHelper.TryDecode(request.ParentLocationIdKey, out var parentId))
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Validation failed",
                    Detail = $"Invalid parent location IdKey: '{request.ParentLocationIdKey}'",
                    Status = StatusCodes.Status400BadRequest,
                    Instance = HttpContext.Request.Path
                });
            }
            parentLocationId = parentId;
        }

        // Resolve campus ID if provided
        int? campusId = null;
        if (!string.IsNullOrWhiteSpace(request.CampusIdKey))
        {
            if (!IdKeyHelper.TryDecode(request.CampusIdKey, out var cId))
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Validation failed",
                    Detail = $"Invalid campus IdKey: '{request.CampusIdKey}'",
                    Status = StatusCodes.Status400BadRequest,
                    Instance = HttpContext.Request.Path
                });
            }
            campusId = cId;
        }

        // Resolve location type ID if provided
        int? locationTypeValueId = null;
        if (!string.IsNullOrWhiteSpace(request.LocationTypeValueIdKey))
        {
            if (!IdKeyHelper.TryDecode(request.LocationTypeValueIdKey, out var typeId))
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Validation failed",
                    Detail = $"Invalid location type IdKey: '{request.LocationTypeValueIdKey}'",
                    Status = StatusCodes.Status400BadRequest,
                    Instance = HttpContext.Request.Path
                });
            }
            locationTypeValueId = typeId;
        }

        // Resolve overflow location ID if provided
        int? overflowLocationId = null;
        if (!string.IsNullOrWhiteSpace(request.OverflowLocationIdKey))
        {
            if (!IdKeyHelper.TryDecode(request.OverflowLocationIdKey, out var overflowId))
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Validation failed",
                    Detail = $"Invalid overflow location IdKey: '{request.OverflowLocationIdKey}'",
                    Status = StatusCodes.Status400BadRequest,
                    Instance = HttpContext.Request.Path
                });
            }
            overflowLocationId = overflowId;
        }

        var location = new Domain.Entities.Location
        {
            Name = request.Name,
            Description = request.Description,
            ParentLocationId = parentLocationId,
            CampusId = campusId,
            LocationTypeValueId = locationTypeValueId,
            SoftRoomThreshold = request.SoftRoomThreshold,
            FirmRoomThreshold = request.FirmRoomThreshold,
            StaffToChildRatio = request.StaffToChildRatio,
            OverflowLocationId = overflowLocationId,
            AutoAssignOverflow = request.AutoAssignOverflow,
            Street1 = request.Street1,
            Street2 = request.Street2,
            City = request.City,
            State = request.State,
            PostalCode = request.PostalCode,
            Country = request.Country,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            IsGeoPointLocked = request.IsGeoPointLocked,
            Order = request.Order,
            IsActive = true,
            CreatedDateTime = DateTime.UtcNow
        };

        context.Locations.Add(location);
        await context.SaveChangesAsync(ct);

        // Reload with related entities for DTO
        var createdLocation = await context.Locations
            .Include(l => l.ParentLocation)
            .Include(l => l.Campus)
            .Include(l => l.LocationTypeValue)
            .Include(l => l.OverflowLocation)
            .AsNoTracking()
            .FirstAsync(l => l.Id == location.Id, ct);

        var locationDto = MapToLocationDto(createdLocation);

        logger.LogInformation(
            "Location created successfully: IdKey={IdKey}, Name={Name}",
            locationDto.IdKey, locationDto.Name);

        return CreatedAtAction(
            nameof(GetByIdKey),
            new { idKey = locationDto.IdKey },
            new { data = locationDto });
    }

    /// <summary>
    /// Updates an existing location.
    /// </summary>
    /// <param name="idKey">The location's IdKey</param>
    /// <param name="request">Location update details</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Updated location details</returns>
    /// <response code="200">Location updated successfully</response>
    /// <response code="400">Validation failed</response>
    /// <response code="404">Location not found</response>
    [HttpPut("{idKey}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(LocationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        string idKey,
        [FromBody] UpdateLocationRequest request,
        CancellationToken ct = default)
    {
        if (!IdKeyHelper.TryDecode(idKey, out var id))
        {
            logger.LogDebug("Invalid IdKey format: {IdKey}", idKey);

            return NotFound(new ProblemDetails
            {
                Title = "Location not found",
                Detail = $"Invalid IdKey format: '{idKey}'",
                Status = StatusCodes.Status404NotFound,
                Instance = HttpContext.Request.Path
            });
        }

        var location = await context.Locations
            .Where(l => l.Id == id)
            .FirstOrDefaultAsync(ct);

        if (location == null)
        {
            logger.LogDebug("Location not found: IdKey={IdKey}", idKey);

            return NotFound(new ProblemDetails
            {
                Title = "Location not found",
                Detail = $"No location found with IdKey '{idKey}'",
                Status = StatusCodes.Status404NotFound,
                Instance = HttpContext.Request.Path
            });
        }

        // Apply partial updates
        if (request.Name != null)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Validation failed",
                    Detail = "Location name cannot be empty",
                    Status = StatusCodes.Status400BadRequest,
                    Instance = HttpContext.Request.Path
                });
            }
            location.Name = request.Name;
        }

        if (request.Description != null)
        {
            location.Description = request.Description;
        }

        if (request.IsActive.HasValue)
        {
            location.IsActive = request.IsActive.Value;
        }

        if (request.ParentLocationIdKey != null)
        {
            if (string.IsNullOrWhiteSpace(request.ParentLocationIdKey))
            {
                location.ParentLocationId = null;
            }
            else if (!IdKeyHelper.TryDecode(request.ParentLocationIdKey, out var parentId))
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Validation failed",
                    Detail = $"Invalid parent location IdKey: '{request.ParentLocationIdKey}'",
                    Status = StatusCodes.Status400BadRequest,
                    Instance = HttpContext.Request.Path
                });
            }
            else
            {
                location.ParentLocationId = parentId;
            }
        }

        if (request.CampusIdKey != null)
        {
            if (string.IsNullOrWhiteSpace(request.CampusIdKey))
            {
                location.CampusId = null;
            }
            else if (!IdKeyHelper.TryDecode(request.CampusIdKey, out var campusId))
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Validation failed",
                    Detail = $"Invalid campus IdKey: '{request.CampusIdKey}'",
                    Status = StatusCodes.Status400BadRequest,
                    Instance = HttpContext.Request.Path
                });
            }
            else
            {
                location.CampusId = campusId;
            }
        }

        if (request.LocationTypeValueIdKey != null)
        {
            if (string.IsNullOrWhiteSpace(request.LocationTypeValueIdKey))
            {
                location.LocationTypeValueId = null;
            }
            else if (!IdKeyHelper.TryDecode(request.LocationTypeValueIdKey, out var typeId))
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Validation failed",
                    Detail = $"Invalid location type IdKey: '{request.LocationTypeValueIdKey}'",
                    Status = StatusCodes.Status400BadRequest,
                    Instance = HttpContext.Request.Path
                });
            }
            else
            {
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

        if (request.OverflowLocationIdKey != null)
        {
            if (string.IsNullOrWhiteSpace(request.OverflowLocationIdKey))
            {
                location.OverflowLocationId = null;
            }
            else if (!IdKeyHelper.TryDecode(request.OverflowLocationIdKey, out var overflowId))
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Validation failed",
                    Detail = $"Invalid overflow location IdKey: '{request.OverflowLocationIdKey}'",
                    Status = StatusCodes.Status400BadRequest,
                    Instance = HttpContext.Request.Path
                });
            }
            else
            {
                location.OverflowLocationId = overflowId;
            }
        }

        if (request.AutoAssignOverflow.HasValue)
        {
            location.AutoAssignOverflow = request.AutoAssignOverflow.Value;
        }

        if (request.Street1 != null)
        {
            location.Street1 = request.Street1;
        }

        if (request.Street2 != null)
        {
            location.Street2 = request.Street2;
        }

        if (request.City != null)
        {
            location.City = request.City;
        }

        if (request.State != null)
        {
            location.State = request.State;
        }

        if (request.PostalCode != null)
        {
            location.PostalCode = request.PostalCode;
        }

        if (request.Country != null)
        {
            location.Country = request.Country;
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

        await context.SaveChangesAsync(ct);

        // Reload with related entities for DTO
        var updatedLocation = await context.Locations
            .Include(l => l.ParentLocation)
            .Include(l => l.Campus)
            .Include(l => l.LocationTypeValue)
            .Include(l => l.OverflowLocation)
            .Include(l => l.ChildLocations.Where(cl => cl.IsActive))
            .AsNoTracking()
            .FirstAsync(l => l.Id == location.Id, ct);

        var locationDto = MapToLocationDto(updatedLocation);

        logger.LogInformation(
            "Location updated successfully: IdKey={IdKey}, Name={Name}",
            locationDto.IdKey, locationDto.Name);

        return Ok(new { data = locationDto });
    }

    /// <summary>
    /// Soft-deletes a location by setting IsActive to false.
    /// </summary>
    /// <param name="idKey">The location's IdKey</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>No content</returns>
    /// <response code="204">Location deactivated successfully</response>
    /// <response code="404">Location not found</response>
    [HttpDelete("{idKey}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(string idKey, CancellationToken ct = default)
    {
        if (!IdKeyHelper.TryDecode(idKey, out var id))
        {
            logger.LogDebug("Invalid IdKey format: {IdKey}", idKey);

            return NotFound(new ProblemDetails
            {
                Title = "Location not found",
                Detail = $"Invalid IdKey format: '{idKey}'",
                Status = StatusCodes.Status404NotFound,
                Instance = HttpContext.Request.Path
            });
        }

        var location = await context.Locations
            .Where(l => l.Id == id)
            .FirstOrDefaultAsync(ct);

        if (location == null)
        {
            logger.LogDebug("Location not found: IdKey={IdKey}", idKey);

            return NotFound(new ProblemDetails
            {
                Title = "Location not found",
                Detail = $"No location found with IdKey '{idKey}'",
                Status = StatusCodes.Status404NotFound,
                Instance = HttpContext.Request.Path
            });
        }

        // Soft delete by setting IsActive to false
        location.IsActive = false;
        location.ModifiedDateTime = DateTime.UtcNow;

        await context.SaveChangesAsync(ct);

        logger.LogInformation("Location deactivated successfully: IdKey={IdKey}", idKey);

        return NoContent();
    }

    /// <summary>
    /// Maps a Location entity to LocationDto.
    /// </summary>
    private static LocationDto MapToLocationDto(Domain.Entities.Location location)
    {
        return new LocationDto
        {
            IdKey = location.IdKey,
            Guid = location.Guid,
            Name = location.Name,
            Description = location.Description,
            IsActive = location.IsActive,
            Order = location.Order,
            ParentLocationIdKey = location.ParentLocation?.IdKey,
            ParentLocationName = location.ParentLocation?.Name,
            Children = location.ChildLocations
                .OrderBy(cl => cl.Order)
                .ThenBy(cl => cl.Name)
                .Select(cl => MapToLocationDto(cl))
                .ToList(),
            CampusIdKey = location.Campus?.IdKey,
            CampusName = location.Campus?.Name,
            LocationTypeName = location.LocationTypeValue?.Value,
            SoftRoomThreshold = location.SoftRoomThreshold,
            FirmRoomThreshold = location.FirmRoomThreshold,
            StaffToChildRatio = location.StaffToChildRatio,
            OverflowLocationIdKey = location.OverflowLocation?.IdKey,
            OverflowLocationName = location.OverflowLocation?.Name,
            AutoAssignOverflow = location.AutoAssignOverflow,
            Street1 = location.Street1,
            Street2 = location.Street2,
            City = location.City,
            State = location.State,
            PostalCode = location.PostalCode,
            Country = location.Country,
            Latitude = location.Latitude,
            Longitude = location.Longitude,
            IsGeoPointLocked = location.IsGeoPointLocked,
            CreatedDateTime = location.CreatedDateTime,
            ModifiedDateTime = location.ModifiedDateTime
        };
    }

    /// <summary>
    /// Recursively builds a LocationDto with its children.
    /// </summary>
    private static LocationDto BuildLocationDto(
        Domain.Entities.Location location,
        Dictionary<int, Domain.Entities.Location> locationDict,
        bool includeInactive)
    {
        // Get children recursively
        var children = locationDict.Values
            .Where(l => l.ParentLocationId == location.Id)
            .Where(l => includeInactive || l.IsActive)
            .OrderBy(l => l.Order)
            .ThenBy(l => l.Name)
            .Select(child => BuildLocationDto(child, locationDict, includeInactive))
            .ToList();

        return new LocationDto
        {
            IdKey = location.IdKey,
            Guid = location.Guid,
            Name = location.Name,
            Description = location.Description,
            IsActive = location.IsActive,
            Order = location.Order,
            ParentLocationIdKey = location.ParentLocation?.IdKey,
            ParentLocationName = location.ParentLocation?.Name,
            Children = children,
            CampusIdKey = location.Campus?.IdKey,
            CampusName = location.Campus?.Name,
            LocationTypeName = location.LocationTypeValue?.Value,
            SoftRoomThreshold = location.SoftRoomThreshold,
            FirmRoomThreshold = location.FirmRoomThreshold,
            StaffToChildRatio = location.StaffToChildRatio,
            OverflowLocationIdKey = location.OverflowLocation?.IdKey,
            OverflowLocationName = location.OverflowLocation?.Name,
            AutoAssignOverflow = location.AutoAssignOverflow,
            Street1 = location.Street1,
            Street2 = location.Street2,
            City = location.City,
            State = location.State,
            PostalCode = location.PostalCode,
            Country = location.Country,
            Latitude = location.Latitude,
            Longitude = location.Longitude,
            IsGeoPointLocked = location.IsGeoPointLocked,
            CreatedDateTime = location.CreatedDateTime,
            ModifiedDateTime = location.ModifiedDateTime
        };
    }
}
